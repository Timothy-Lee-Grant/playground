# The Button That Was Already Pressed: Timothy's First Code Change to dotnet/iot

Audio lecture 001 for dotnet/iot issue 2328, written on 2026-10-01 to be listened to as a NotebookLM Audio Overview. It is the listening companion to reading lecture 001, "The change, end to end", and covers the same ground: the bug, the fix, the tests, and the two questions for the maintainers.

## What this episode is about

This episode tells the story of one small bug in a Microsoft open-source library, and the change that fixes it. The library is dotnet/iot, D-O-T-N-E-T slash I-O-T, which is how .NET programs talk to hardware like the pins on a Raspberry Pi. The bug is in the part of the library that represents a physical push button.

The listener is an embedded C engineer. He has written plenty of firmware that reads buttons, so the core of this bug will feel familiar. What's new for him is how the same problem looks inside a C# library, how you prove a fix without any hardware attached, and how you talk to the people who own the code before you send them a change.

The key idea for the whole episode fits in one sentence. **The button only learns its state from changes on the pin, so a button that is already held down when the program starts is invisible until it changes.** Everything else follows from that.

Here's the route. First, the cast of characters. Second, the bug, followed step by step. Third, the fix, which is just two lines of logic. Fourth, how the fix was proven with a fake piece of hardware. Fifth, the two open questions for the maintainers. Sixth, the comment that will be posted on the issue, and what posting it means. And finally, common misconceptions and a recap.

One honesty note up front. The fix and its tests have been built and run on Timothy's Mac. Seven new tests were added. Four of them failed before the fix and all of them pass after it, along with the eight tests that already existed. None of this has been run on a real Raspberry Pi with a real button yet, and nothing has been posted publicly yet.

## Part one: the cast of characters

There are five characters, and it helps to keep their names straight.

The first is **the Switchboard**. In the code it's called GpioController, the G-P-I-O controller. Its job is to open pins, read them, and let you register a callback that runs when a pin changes. The Switchboard doesn't touch hardware itself. It checks that the pin is open and then passes the request down.

The second is **the Hardware Whisperer**. In the code it's GpioDriver, the G-P-I-O driver. This is the layer that actually talks to the operating system or the chip. There are several real ones, for different ways Linux exposes pins. The key idea about the Whisperer: **it's the seam where tests swap in a fake**. We'll come back to that in part four.

The third is **the Wiring Translator**. In the code it's GpioButton. It knows how the button is wired. Most buttons are wired with a pull-up resistor, which holds the pin high, so pressing the button pulls it to ground. For a pull-up button, **pressed means the pin reads low**. The mirror image is pull-down wiring, where pressed means high. The Translator turns pin changes into "pressed" and "released".

The fourth is **the Brain**. In the code it's ButtonBase. The Brain doesn't know pins exist. It keeps the state, a true-or-false property called IsPressed. It handles debounce. It runs the holding timer. And it raises the events your app listens to: ButtonDown, ButtonUp, Press, which means a full click, DoublePress, and Holding.

The fifth character is the most important one for this bug. He's **the Watcher**. When the Translator registers its callback, the driver starts watching the pin, and **on most real drivers the watching happens on its own background thread**. When the pin changes, the Watcher calls the Translator's callback, a method named PinStateChanged, from that background thread.

The firmware comparison is a good one, up to a point. The Watcher plus PinStateChanged is the library's version of an edge-triggered interrupt and its interrupt service routine. Where the analogy stops: in firmware, the CPU jumps into your ISR. Here, a separate thread calls an ordinary C# method, and that thread can run at the same moment as other code in your program. Hold on to that; it matters in part five.

Quick recap of part one. The Switchboard routes requests. The Hardware Whisperer talks to the hardware and is where tests plug in a fake. The Wiring Translator knows whether low means pressed. The Brain keeps the state and raises the events. The Watcher calls back from its own thread when the pin changes.

## Part two: the bug, followed step by step

Let's follow one real situation. The program starts while someone is already holding the button down. The button is wired pull-up, so the pin is reading low.

Step one. The program creates the button. The Brain is constructed first, and its IsPressed property starts out as false. That's just the default value of a C# property. Nobody has looked at the pin.

Step two. The Translator asks the Switchboard to open the pin as an input with the pull-up turned on.

Step three. The Translator registers its callback for both kinds of change: falling, which is high to low, and rising, which is low to high. From this moment on, the button only hears about **changes**.

And that's the whole bug. **No one ever reads the pin's current level.** The button is held, the pin is low, and nothing is changing, so no callback ever fires. If your program asks whether the button is pressed, it gets false. That was issue 2328 as it was originally reported.

In firmware terms, this is the classic mistake of setting up an edge interrupt for a button and never sampling the pin during initialization. Your pressed flag is wrong until the first edge.

But reading the code turned up something worse, and this is the part Timothy's change brings to the maintainers that the original report didn't mention.

Keep following the same story. Now the person lets go of the button. The pin rises from low to high. The Watcher sees a rising change and calls the Translator. The Translator knows this is a pull-up button, so rising means released, and it calls the Brain's release handler, a method named HandleButtonReleased.

The very first thing that release handler does is a guard check. In words, it says: if debounce is turned on, and we're not currently pressed, then ignore this release and return immediately.

That guard makes sense on its own. With debounce enabled, a release that arrives when the button isn't even pressed looks like electrical noise, contact bounce. But in our story, the Brain wrongly believes the button isn't pressed, because nobody ever told it. So the guard throws away a **real** release. No ButtonUp event. No Press event. **The user's first click simply vanishes.**

The key idea of part two: **the wrong starting value isn't cosmetic. With debounce enabled, it silently eats a real user action.**

For completeness: if debounce is turned off, which is the default, the guard doesn't apply. Then the release does produce ButtonUp and Press, just without a ButtonDown before them. That's odd, but nothing is lost. That path was only checked by reading the code, not by a test, and the fix doesn't change it.

Recap of part two. The button learns only from changes. A level that's already there at startup produces no change. So IsPressed starts false and stays false. And with debounce on, the first real release is thrown away by a guard that assumed the button wasn't pressed.

## Part three: the fix

The fix lives in the Translator's constructor, the code that runs when the button is created. It adds two lines of logic, plus a two-line comment.

The first new line reads the pin once, through the Switchboard, and stores the result in a local variable called initialValue. It's a one-time sample of the level, right now.

The second new line sets IsPressed. In words, it says: if this button is wired pull-up, then it's pressed when the pin reads low; otherwise, it's pressed when the pin reads high. It's written with the question-mark-colon conditional operator, which works exactly as it does in C.

There are four decisions hidden in those two lines, and each one is worth understanding.

Decision one: **where** the lines go. They go after the callback is registered, not before. Part five explains why, because it's one of the two questions for the maintainers. They also go inside the existing try block. That means if reading the pin throws an error, the same cleanup runs that already runs when opening the pin fails. Put them outside, and a failed read could leak the controller.

Decision two: **which field** to use for the wiring. The Translator has two fields that sound alike. One records the mode the pin is configured with. The other records how the button is wired. They're usually the same, but not always. If your board has its own external resistor, the pin is configured as a plain input with the chip's internal resistor turned off, but the button is still wired pull-up. Only the wiring field still knows that pressed means low. So the fix uses the wiring field, named eventPinMode. It's also the same field the existing change-handling code already uses, so the same rule appears twice in the file, consistently. One of the tests proves this choice matters: it failed before the fix.

Decision three: **what the fix doesn't do**. It does not pretend a press happened. The obvious alternative was to call the Brain's press handler, which would raise the ButtonDown event. That was rejected for two reasons. First, ButtonDown means "a press happened just now", and nothing happened: the button was already down before the program started. Second, keeping the fix free of side effects means a future change can't accidentally turn it into a burst of events.

A common misconception, and one the planning made itself at first: that raising ButtonDown in the constructor would cause a phantom click for the user. **Actually, an event raised inside the constructor reaches no one, because nobody can have subscribed yet.** Your app can only add its handler after the button has been created, and the constructor is still running. The CLI agent that wrote the code pointed this out while checking the plan, and it was right. The two reasons above are the real ones.

Decision four: **what file it doesn't touch**. The Brain's file, ButtonBase, is untouched. That's on purpose. One of the maintainers, Patrick Grawehr, known on GitHub as pgrawehr, has an open pull request, number 2608, that rewrites a lot of that file to make the button tests less flaky. Staying out of it means the two changes won't collide.

There's also a short documentation note on the Translator's class, telling users that IsPressed now starts from the pin level and that no events are raised for that starting state.

Recap of part three. Read the pin once, after registering the callback, inside the existing error handling. Decide pressed using the wiring, not the pin mode. Set the state, don't raise events. And leave the Brain's file alone.

## Part four: proving it with a fake piece of hardware

How do you prove a hardware bug is fixed when the tests run on a laptop, and on a build server, with no Raspberry Pi anywhere?

The answer is the seam we mentioned earlier: the Hardware Whisperer. The tests build a real Translator and a real Brain on top of a real Switchboard, but underneath the Switchboard they put **a stand-in Whisperer** that the test controls completely.

Here's the design you'd write by hand in firmware. You'd put the hardware behind a table of function pointers, a hardware abstraction layer, and in your unit tests you'd swap in a table of stub functions that return whatever values the test needs. That's exactly what's happening here, with two differences. In C#, the "table" is an abstract class that every real driver inherits from. And the stub isn't written by hand. A library called **Moq**, spelled M-O-Q, generates it while the test runs. Think of Moq as **the Prop Department**: you tell it what object you need, and what it should answer when asked, and it builds the prop.

There's one wrinkle, and it explains a helper class with an odd name: MockableGpioDriver. The real driver's methods are protected, which means outside code can't call them, and Moq can only script public methods. So the repo already contains a small adapter class that overrides each protected method and forwards it to a public twin. Read forwards to a public method called ReadEx. Open forwards to OpenPinEx. And so on. The adapter also remembers the callback the Translator registered, and it has a method called FireEventHandler that calls that callback. **FireEventHandler is how a test plays the role of the Watcher**: it says "the pin just rose", and the Translator's callback runs.

The test project uses that adapter by linking to the existing file rather than copying it. It's one line in the project file. In firmware terms, it's like adding a shared C file to a second build target's list of sources. Three other test projects in the repo already do the same.

Two traps came up, and every test is written to avoid them. First: when you ask a Moq stand-in something you didn't script, it answers with a default. If nobody scripts the question "is pull-up mode supported on this pin?", the answer is false, and the Translator's constructor throws an error. So the test setup scripts that answer to yes. Second, and sneakier: if nobody scripts the pin's level, the default value is low, and for a pull-up button **low means pressed**. A test that forgot to set the level could pass by accident. So every test states the starting level explicitly.

Now the tests themselves.

The first is a smoke test. It creates a button and checks that the stand-in saw the right calls: the pin opened, set to pull-up, the callback registered for both kinds of change, and on disposal, the callback removed and the pin closed. It proves the fake is really wired in, so the other tests mean something. It says nothing about the bug, and it passed both before and after the fix.

The second is a table of four cases run by one test. Pull-up and low should be pressed. Pull-up and high should be released. Pull-down and high should be pressed. Pull-down and low should be released. Before the fix, the two "pressed" cases failed: the test expected true and got false. The two "released" cases passed before and after. They're guards. They're there to catch a wrong fix, like one that always says pressed, or one that has the wiring backwards.

The third test is the external resistor case. It checks the pin was configured as a plain input, and that the button still reports pressed. It failed before the fix, and it's the test that proves decision two from part three.

The fourth test is the one that matters most. It creates a held button with debounce turned on, subscribes to ButtonUp and Press, and then uses FireEventHandler to say "the pin just rose", meaning the user let go. Before the fix, it failed at the very first check: ButtonUp never fired. **That failure is the swallowed click, caught in a test.** After the fix, ButtonUp and Press both fire, and IsPressed ends up false.

An important idea about all of these: they failed **for the right reason**. Each failure was an assertion, a check that ran and found the wrong value. None of them crashed. A crash would prove that something is broken. An assertion failure proves that **this** behavior is wrong.

After the fix, all fifteen tests pass. The build has zero warnings, which matters because this repository treats every warning as an error. The tests were run five times in a row to make sure nothing was flaky. And the whole change is three files, ninety-eight lines added, and nothing removed. The work is in two commits: the first adds the tests and proves the bug, the second adds the fix. A reviewer can check out the first commit and watch the tests fail, then check out the second and watch them pass.

Now the honest part: what these tests do **not** prove. They don't prove a real Raspberry Pi behaves this way, because everything underneath is a stand-in. They don't prove the pin voltage has settled when it's read, because the stand-in answers instantly. And they can't test the race we're about to discuss, because FireEventHandler calls the callback on the test's own thread, not on a background thread like the real Watcher. Saying this plainly in the pull request is part of doing it well.

Recap of part four. Tests replace the Hardware Whisperer with a stand-in made by Moq. FireEventHandler plays the Watcher. Always script the starting level, because the default means pressed. Four tests went from red to green for the right reason, including the one that catches the swallowed click. And the tests can't speak to real timing.

## Part five: the two questions for the maintainers

Two decisions don't belong to Timothy. They belong to the people who maintain the library, so the change uses sensible defaults and asks.

The first question is **when** to read the pin.

When the pull-up resistor is switched on, the pin's voltage doesn't jump to high instantly. The resistor is charging the small capacitance of the pin, the wire and the button. That's an RC circuit, and on a long wire it can take a noticeable moment. Another open issue, number 1715, reports a false click at startup for exactly this reason. So if the new read happens during that rise, on a pull-up line, it might see low and report pressed when nobody is touching the button.

There are three choices. Read immediately, which is what was built: simplest, with a small risk on slow lines. Wait a short settle delay, then read: safer, but it blocks the constructor, and how long is long enough depends on the hardware. Or read lazily, the first time someone subscribes to a button event: this is the fix the maintainers already agreed on for issue 1715, but it's a bigger change that would reach into the Brain's file, where the other pull request is working.

The second question is **in what order**: read the pin before turning on the callback, or after.

This is where the Watcher's background thread matters. Follow both orders step by step.

Order one, read first, then register. The constructor reads the pin: low, pressed. In the gap before the callback is registered, the user lets go. Nobody is watching yet, so nobody hears it. Then the callback is registered, and the constructor sets IsPressed to true. Now it's stale, and it stays wrong until the next change. The gap here is the whole registration call, which on some drivers even starts a new thread.

Order two, register first, then read, which is what was built. The callback is registered, and the Watcher is already watching. The constructor reads the pin: low, pressed. Now, in the tiny gap before the constructor stores that answer, the user lets go. The Watcher's thread runs the release handler and sets IsPressed to false. Then the constructor finishes its line and sets IsPressed to true, overwriting the newer answer with the older one. Also stale.

So the key idea is this. **Both orders have a gap. Registering first shrinks the gap from a whole method call down to a few instructions.** Closing it completely would need a lock shared by the constructor and the Brain's handlers, and today the Brain has no locking at all. That would mean changing the Brain's file, which is out of scope and in the other pull request's territory. The honest thing is to say this to the maintainers, and the comment does.

A common misconception here: that "register, then read" is simply correct. Actually, it's the **better** of two imperfect orders, not a perfect one. The CLI agent caught this while checking the plan against the code, and it corrected the original plan, which had claimed it was safe.

Recap of part five. When to read: immediately, after a settle delay, or lazily; we built immediately and we ask. In what order: both orders leave a gap; register-then-read leaves the smaller one; only a lock would close it.

## Part six: the comment, and what posting it means

Before any pull request, the comment goes on the issue. In open source, you ask before you send code. The issue is assigned to a maintainer known on GitHub as raffaeler, R-A-F-F-A-E-L-E-R, as the owner of that area.

Here's what the comment says, in plain words. It asks whether the work is still wanted. It explains that the button never reads the pin's current level. It says this is more than cosmetic, because with debounce on, the first release of a held button is swallowed. It says a small change is ready, with tests that fail before the change and pass after it, including the swallowed release. Then it asks the two questions from part five: whether reading immediately is acceptable or they'd prefer a settle delay or a lazy read, and whether the small remaining gap in the ordering is acceptable. It mentions that the change doesn't touch the file that pull request 2608 is rewriting, and offers to rebase once that's merged. And it asks raffaeler whether it's all right to take the issue.

Every factual claim in that comment is backed by something. The bug claims are backed by the code. The swallowed release is backed by the fourth test failing before the fix. The two questions are backed by reasoning, and the comment presents them as questions, not answers.

What does posting it commit Timothy to? Doing the work if they say yes, and it's already done. Answering their questions within a reasonable time; his own rule is within two days. And accepting their direction on the two questions. What it does **not** commit him to: a deadline, defending the defaults, or taking on a bigger change than he's comfortable with. If they choose the lazy option, which is a larger change, it's perfectly fine to say so and ask whether they'd like it as a separate follow-up.

And the possible answers are all manageable. If they say go ahead, the pull request opens as it is. If they want a settle delay, that's a small addition. If they want the other order, that's swapping two statements. If they say Patrick will fold it into his pull request, Timothy compares his version with ours and learns from the difference. If nobody answers by Sunday, the pull request opens anyway, with the questions repeated in its description.

## Part seven: common misconceptions

Let's gather the misconceptions in one place, each as the wrong version and then the correction.

Misconception one: the bug is only a wrong property value. Actually, with debounce enabled, it causes a real click to be thrown away.

Misconception two: the fix should simulate a press when the button is held. Actually, it only sets the state, because nothing was pressed while the program was running, and no events should fire.

Misconception three: raising an event in the constructor would surprise subscribers. Actually, nobody can be subscribed while the constructor is still running.

Misconception four: the pin-mode field and the wiring field are the same thing. Actually, with an external resistor they differ, and only the wiring field knows that low means pressed.

Misconception five: if the tests pass, the hardware works. Actually, the tests prove the logic with a stand-in. Real timing, settling and threads are not tested.

Misconception six: registering first, then reading, is race-free. Actually, it has a much smaller gap, not no gap.

## Recap: the key ideas

Here are the nine ideas to be able to explain in your own words.

One. The button learns its state only from changes on the pin, so a button held at startup is invisible.

Two. With debounce on, that makes the first real release disappear, because a guard in the release handler assumes the button wasn't pressed.

Three. The fix reads the pin once, right after registering the callback, and sets IsPressed using the wiring, not the pin mode.

Four. The fix sets state and raises no events, because nothing happened, and because nobody could be listening yet anyway.

Five. The tests replace the Hardware Whisperer with a stand-in built by Moq, and FireEventHandler plays the Watcher.

Six. Four tests failed before the fix for the right reason, by assertion, including the one that catches the swallowed click, and all fifteen pass after it.

Seven. The tests can't prove real hardware timing, settling, or the threading race.

Eight. The maintainers decide two things: when to read, given settle time, and in what order, given the race; register-then-read leaves the smaller gap.

Nine. Posting the comment commits Timothy to doing the work and listening, not to a deadline or to defending every default.
