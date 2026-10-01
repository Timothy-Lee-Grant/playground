# Lecture 001 (iot#2328): The change, end to end

> **Prompted by:** [dotnet/iot#2328](https://github.com/dotnet/iot/issues/2328) · **Date:** 2026-10-01 ·
> **Built from:** the CLI's plan entries (`../shared/plan.md` Stage 5), the saved runs in `../shared/evidence/`, and
> the diff `../shared/evidence/006-diff.patch`. Everything marked *verified* was run; *unverified* means read from
> the code but not run.
> **Helpful, not required:** `../../iot_concepts/002-delegates-events-callbacks-and-threads.md` (events, callbacks).
> **Reading time:** about 45 minutes. Section 0 takes 5 and is worth doing first.

**The whole thing in five lines**

1. `GpioButton` learns whether it's pressed **only from edges** (the pin changing). A button already held when the
   program starts produces no edge, so the button wrongly reports "not pressed".
2. With debounce on, that wrong state makes the button **throw away the first release** entirely.
3. The fix is 2 lines of logic: right after subscribing to edges, **read the pin once** and set `IsPressed` from
   its level, without raising any events.
4. Seven new tests run the real `GpioButton` against a **fake GPIO driver**: 4 failed before the fix and pass after.
5. Two questions remain for the maintainers: **when** to take that first reading, and **in what order** relative to
   subscribing. The comment you'll post asks exactly those.

---

## 0. Before you read: watch it fail, then pass (5 minutes)

Seeing it happen first is how you learn best, so start here. In a terminal:

```bash
cd ~/Desktop/projects/oss-work/iot-2328/develop/iot
git log --oneline -3
#   395b9fbf Initialize GpioButton.IsPressed from the pin level        ← commit 2: the fix
#   bd01e163 Add GpioButton tests for the initial pressed state        ← commit 1: the tests
#   1eb0b2f6 Make OneWire sysfs paths ... (#2603)                      ← upstream/main, where we started

git checkout bd01e163                      # tests, but NOT the fix ("detached HEAD" warning is fine)
dotnet test src/devices/Button/tests/      # expect: Failed: 4, Passed: 11
git checkout fix/2328-gpiobutton-initial-state
dotnet test src/devices/Button/tests/      # expect: Passed: 15
```

The two commits are split exactly so that this works: commit 1 *proves the bug*, commit 2 *fixes it*. A reviewer
can do the same thing you just did.

---

## 1. The problem, shown happening

### 1.1 The rule

**The button learns its state only from edges. A level that's already there at startup produces no edge, so the
button never hears about it.**

Firmware version of the same bug: you configure an edge-triggered interrupt on a button pin and track "pressed" in
the ISR, but you never sample the pin level during init. If the button is held at power-on, your `pressed` flag is
wrong until the first edge. The fix in firmware is the same as here: sample the level once at init.

Where the analogy stops: in C, your ISR runs when the CPU takes the interrupt. Here, the "ISR" is a C# method
(`PinStateChanged`) that the **driver calls from its own background thread** when it sees an edge. That
difference matters in §5.2.

### 1.2 The cast

| Character | Job | Where |
|---|---|---|
| **`GpioController`** | The pin switchboard: open pins, read them, register edge callbacks. Checks the pin is open, then forwards to the driver | `System.Device.Gpio` |
| **`GpioDriver`** | Talks to the real hardware (libgpiod, sysfs, Raspberry Pi registers). The controller holds one | `System.Device.Gpio` |
| **`GpioButton`** | The wiring translator. Knows whether the button is wired pull-up or pull-down, so it knows whether Low means pressed. Turns edges into "pressed"/"released" | `src/devices/Button/GpioButton.cs` |
| **`ButtonBase`** | The button's brain. Hardware-independent. Holds `IsPressed`, does debounce, runs the holding timer, raises the events (`ButtonDown`, `ButtonUp`, `Press`, `DoublePress`, `Holding`) | `src/devices/Button/ButtonBase.cs` |
| **Your app** | Subscribes to the events, reads `IsPressed` | — |

Pull-up wiring, the default: a resistor holds the pin High; pressing the button connects it to ground, so
**pressed = Low**. Pull-down is the mirror: **pressed = High**.

### 1.3 Path A: the property is wrong

```
new GpioButton(pin 12, isPullUp: true)          // button is physically held: pin is Low
  │
  ├─ ButtonBase:  public bool IsPressed { get; set; } = false;    ← starts false, nobody checks the pin
  ├─ controller.OpenPin(12, InputPullUp)
  └─ controller.RegisterCallbackForPinValueChangedEvent(12, Falling | Rising, PinStateChanged)
                                                    ← from here on, only EDGES reach the button
button.IsPressed  →  false                          ✗ the button is held
```

### 1.4 Path B: the first release is swallowed (the worse one)

When you let go, the pin rises (Low → High). The driver sees a **Rising** edge and calls `PinStateChanged`, which
(for pull-up) calls `HandleButtonReleased()` in `ButtonBase`. Its first lines:

```csharp
protected void HandleButtonReleased()
{
    if (_debounceTime.Ticks > 0 && !IsPressed)   // "debounce is on, and we weren't pressed?"
    {
        return;                                  // "...then this release is noise: ignore it"
    }
    ...
    IsPressed = false;
    ButtonUp?.Invoke(this, new EventArgs());
    ...
    Press?.Invoke(this, new EventArgs());        // "a click happened"
```

That guard is reasonable on its own: with debounce, a release that arrives while we think the button isn't pressed
is treated as contact bounce. But because `IsPressed` is wrongly `false`, a **real** release gets thrown away: no
`ButtonUp`, no `Press`. Your app never hears about the user's first click.

With debounce **off** (the default), the guard doesn't apply: you'd get `ButtonUp` and `Press` with no `ButtonDown`
before them. Odd, but not lost. *(unverified: read from the code, not tested; the fix doesn't change this path.)*

### 1.5 The evidence: what "red" looked like

Saved in `../shared/evidence/003-red.txt` (tests added, fix not yet applied). 15 tests ran, 4 failed:

| Failed test | Assertion that failed | Which path it shows |
|---|---|---|
| `..._IsPressed_Reflects_Pin_Level(isPullUp: True, levelAtStartup: 0, expectedIsPressed: True)` | `Assert.Equal() Failure: Expected: True, Actual: False` | Path A, pull-up |
| `..._IsPressed_Reflects_Pin_Level(isPullUp: False, levelAtStartup: 1, expectedIsPressed: True)` | same | Path A, pull-down |
| `If_Button_Has_External_PullUp_And_Pin_Is_Low_At_Startup_Button_Is_Pressed` | `Assert.True() Failure: Expected: True, Actual: False` | Path A, external resistor |
| `If_Button_Is_Held_At_Startup_With_Debouncing_Release_Raises_ButtonUp_And_Press` | `Assert.True() Failure` on `buttonUp` | **Path B**: the release was swallowed |

Notice they failed on **assertions**, not crashes. That's what "failing for the right reason" means: the test ran
all the way to the point of checking the behavior, and the behavior was wrong. A test that fails with an exception
proves only that something is broken, not that it's *this* bug.

---

## 2. The change, line by line

The whole product change (`src/devices/Button/GpioButton.cs`, +9 lines, 0 removed):

```csharp
                _gpioController.OpenPin(_buttonPin, _gpioPinMode);
                _gpioController.RegisterCallbackForPinValueChangedEvent(
                    _buttonPin,
                    PinEventTypes.Falling | PinEventTypes.Rising,
                    PinStateChanged);

+               // A button already held down at startup produces no edge, so take the initial state from the pin level.
+               // This only sets the state; no events are raised.
+               PinValue initialValue = _gpioController.Read(_buttonPin);
+               IsPressed = _eventPinMode == PinMode.InputPullUp ? initialValue == PinValue.Low : initialValue == PinValue.High;
            }
            catch (Exception)
            {
                if (shouldDispose) { _gpioController.Dispose(); }
                throw;
            }
```

plus a `<remarks>` doc comment on the `GpioButton` class (§2.5).

### 2.1 Where it sits: after subscribing, inside the `try`

**Rule: subscribe first, then read; and keep the read inside the existing error handling.**

- *After* `RegisterCallback…`: this is the ordering question the maintainers will answer (§5.2). We chose
  "subscribe, then read" because it leaves the smallest window for a missed change.
- *Inside the `try`*: if `Read` throws, the `catch` cleans up exactly as it already does when `OpenPin` throws (it
  disposes the controller if the button created it). Outside the `try`, a failed read would leak the controller.

### 2.2 Line 1: `PinValue initialValue = _gpioController.Read(_buttonPin);`

**Rule: this asks the driver for the pin's level right now, once.**

`GpioController.Read` does two things: it throws if the pin isn't open, then calls `_driver.Read(pin)`. What it
does **not** do (worth knowing, F7): it doesn't wait for the voltage to settle, doesn't debounce, and doesn't lock
anything. It's one sample.

`PinValue` is a **struct** with two values, `PinValue.Low` and `PinValue.High`. *Where this differs from C:* in
Arduino-style C, `LOW` is a macro for the integer `0`, and `==` compares integers. In C#, `PinValue` is its own
type; `==` calls an operator the type defines (`operator ==(PinValue a, PinValue b)`), and an `int` converts into
it automatically (`0` → `Low`, anything else → `High`). That's why the tests can write `0` and `1` (§4.3).

### 2.3 Line 2: the ternary

```csharp
IsPressed = _eventPinMode == PinMode.InputPullUp ? initialValue == PinValue.Low : initialValue == PinValue.High;
```

Read it as: "If the button is wired pull-up, pressed means the pin reads Low; otherwise pressed means High." The
`? :` works exactly like C's.

**Why `_eventPinMode` and not `_gpioPinMode`?** `GpioButton` has two pin-mode fields, and the difference is the
whole point of the external-resistor case:

| Field | What it records | `isPullUp: true`, no external resistor | `isPullUp: true`, `hasExternalResistor: true` |
|---|---|---|---|
| `_gpioPinMode` | the mode the pin is **configured** with | `InputPullUp` (chip's internal resistor on) | `Input` (internal resistor off; your board has one) |
| `_eventPinMode` | how the button is **wired** | `InputPullUp` | `InputPullUp` |

Only `_eventPinMode` still knows "pressed = Low" when an external resistor is used. `PinStateChanged` already makes
the same decision with the same field (`if (_eventPinMode == PinMode.InputPullUp)`), so a reader sees one rule
applied twice. The external-resistor test (§4.4) proves this choice; it failed before the fix.

### 2.4 What it deliberately doesn't do: raise events

**Rule: the fix sets state; it does not pretend a press happened.**

The obvious alternative is to call `HandleButtonPressed()` when the pin reads "pressed". That would raise
`ButtonDown` and could start the holding timer. Why not:

1. `ButtonDown` means "a press **happened**". None did: the button was already down before the program started.
2. It keeps the fix free of side effects. If a later change (like #1715's idea of subscribing lazily) moved this
   code to a moment when subscribers *exist*, an event here would suddenly start firing.

A correction to our own earlier briefing, found by the CLI: the brief said raising `ButtonDown` here "would
recreate #1715". Strictly, it wouldn't, because **no one can be subscribed yet**: `button.ButtonDown += …` can only
run after `new GpioButton(...)` has returned. So an event raised in the constructor reaches no one. The two reasons
above are the real ones.

### 2.5 The comment and the doc remark

Two `//` lines say *why* the code exists (no edge at startup) and the one non-obvious rule (no events). The
`<remarks>` on the class tells **users** of `GpioButton` about the behavior change: "`IsPressed` is initialized
from the pin level when the button is created … No events are raised for this initial state." It's on
`GpioButton` and not on `ButtonBase.IsPressed` because `ButtonBase` knows nothing about pins; a note there about a
subclass would point the wrong way. It also means **`ButtonBase.cs` isn't touched at all**, so there's no overlap
with pgrawehr's PR #2608, which rewrites 56 lines of that file.

---

## 3. How it fits together

### 3.1 Construction, after the fix

```
new GpioButton(pin 12, isPullUp: true)              button physically held: pin Low
  ├─ ButtonBase:        IsPressed = false
  ├─ OpenPin(12, InputPullUp)
  ├─ RegisterCallback(12, Falling|Rising, PinStateChanged)      ← driver thread starts watching edges
  ├─ Read(12)  →  Low                                ◄── NEW
  └─ IsPressed = (pull-up && Low)  →  true            ◄── NEW, no events
button.IsPressed  →  true                             ✓
```

### 3.2 The first release, after the fix (debounce on)

```
user lets go → pin Low→High → driver sees Rising → PinStateChanged(Rising)
  → pull-up? yes → HandleButtonReleased()
       guard: debounce > 0 && !IsPressed ?  →  IsPressed is true now → guard passes
       IsPressed = false;  ButtonUp ✓;  Press ✓
```

### 3.3 Before → after

| Situation | Before | After |
|---|---|---|
| Created while held (pull-up Low, pull-down High, external pull-up Low) | `IsPressed == false` | `true` (*verified*) |
| Created while released | `false` | `false` (*verified*) |
| Held at startup, debounce on, then released | release swallowed | `ButtonUp` + `Press`, `IsPressed` false (*verified*) |
| Held at startup, debounce off, then released | `ButtonUp` + `Press`, no `ButtonDown` before them | same (*unverified*, path unchanged) |
| Held at startup with holding enabled | no `Holding` | still no `Holding`; release gives `Press` (*unverified*: the timer only starts on a press **edge**) |

---

## 4. The tests

### 4.1 Why a fake driver

**Rule: tests replace the hardware at the `GpioDriver` seam, so the real `GpioButton` and `ButtonBase` code runs
with no Raspberry Pi.**

```
  test ──► new GpioButton(12, ..., gpio: controller)          ← REAL code under test
                       │
            new GpioController(fakeDriver)                     ← REAL controller
                       │
            Mock<MockableGpioDriver>                           ← FAKE: the test scripts what it answers
              "is InputPullUp supported on 12?"  → true
              "read pin 12"                      → Low (or whatever the test says)
              FireEventHandler(12, Rising)       → plays the hardware: "an edge happened"
```

The design you'd write by hand in firmware (F4): put the hardware behind a table of function pointers (a HAL), and
in your unit tests swap in a table of stub functions that return scripted values. That's exactly this, with two
differences: the "table" is an **abstract class** (`GpioDriver`) that real drivers subclass, and the stub is
generated **at runtime** by a library, Moq, instead of written by you.

### 4.2 The three helpers, and what each one does and doesn't do

| Helper | Does | Doesn't |
|---|---|---|
| **Moq** (`Mock<T>`) | Generates a subclass of `T` at runtime. `Setup(...).Returns(...)` scripts an answer; `Verify(..., Times.Once)` checks a call happened | Know anything about GPIO. Unscripted calls return defaults (`false`, `0`, `Low`), silently |
| **`MockableGpioDriver`** (in `System.Device.Gpio.Tests`) | `GpioDriver`'s methods are `protected`, and Moq can only script **public** ones. So this class overrides each protected method to forward to a public twin: `Read` → `ReadEx`, `OpenPin` → `OpenPinEx`, … It also remembers the registered callback, and `FireEventHandler` calls it | Run on a background thread: `FireEventHandler` calls the callback **synchronously, on the test's own thread** |
| **`CallBase = true`** | Tells Moq: "for methods I didn't script, run the real code of the class". Needed so the protected overrides actually forward to the `…Ex` twins | — |

**Two traps the CLI found, and why every test sets them up:**
- Unscripted `IsPinModeSupportedEx` returns `false`, and `GpioButton`'s constructor then throws "cannot be
  configured as pull-up". So the test class's constructor scripts it to `true`.
- Unscripted `ReadEx` returns `default(PinValue)`, which is **Low**, which for pull-up means "pressed". A test that
  forgot to script the level would pass or fail by accident. So every test states the startup level explicitly.

**Linking instead of copying.** The test project reuses `MockableGpioDriver.cs` with one line in
`Button.Tests.csproj`:

```xml
<Compile Include="..\..\..\System.Device.Gpio.Tests\MockableGpioDriver.cs" Link="MockableGpioDriver.cs" />
```

"Compile this file into my project too, without copying it." Firmware equivalent: adding a shared `.c` file to a
second build target's source list. Three other test projects in the repo (Tca955x, Gpio, Board) already do it.

### 4.3 The test class's setup

```csharp
public GpioButtonTests()                                   // xUnit creates a NEW instance per test,
{                                                          // so each test gets a fresh mock
    _driver = new Mock<MockableGpioDriver>();
    _driver.CallBase = true;
    _driver.Setup(x => x.IsPinModeSupportedEx(ButtonPin, It.IsAny<PinMode>())).Returns(true);
}

private GpioButton CreateButton(PinValue levelAtStartup, bool isPullUp = true,
                                bool hasExternalResistor = false, TimeSpan debounceTime = default)
{
    _driver.Setup(x => x.ReadEx(ButtonPin)).Returns(levelAtStartup);     // "the pin reads X at startup"
    return new GpioButton(ButtonPin, isPullUp, hasExternalResistor,
                          new GpioController(_driver.Object), shouldDispose: true, debounceTime);
}
```

`_driver.Object` is the generated fake itself; `_driver` is the handle you script and verify through.

### 4.4 Each test

**(1) Smoke test: `If_Button_Is_Created_And_Disposed_Pin_Is_Opened_And_Closed`**
Creates a button and checks, through `Verify`, that the fake saw: pin 12 opened, mode set to `InputPullUp`, a
callback registered for both edges; and after `Dispose`, the callback removed and the pin closed.
- *Proves:* the fake really is wired into `GpioButton`, so the other tests' passes aren't empty.
- *Doesn't prove:* anything about `IsPressed`. It passed before and after the fix.

**(2) Theory: `If_Button_Is_Created_IsPressed_Reflects_Pin_Level`**, four rows:

```csharp
[InlineData(true, 0, true)]   // pull-up, Low  → pressed        RED before the fix
[InlineData(true, 1, false)]  // pull-up, High → released       guard (green before and after)
[InlineData(false, 1, true)]  // pull-down, High → pressed      RED before the fix
[InlineData(false, 0, false)] // pull-down, Low → released      guard
```

A `[Theory]` is one test body run once per `[InlineData]` row, like a table-driven test in C. The levels are written
as `0`/`1` because attribute arguments must be compile-time constants, and a struct like `PinValue` can't be one;
the `int` converts to `PinValue` automatically (§2.2).
- *Proves:* the bug and the fix for both wirings (rows 1 and 3). The guard rows catch a **wrong** fix, e.g. one
  that always says `true`, or that has the wiring backwards.
- *Doesn't prove:* that the real pin has settled when it's read (§5.1). The fake answers instantly.

**(3) `If_Button_Has_External_PullUp_And_Pin_Is_Low_At_Startup_Button_Is_Pressed`**
Checks the pin was configured as plain `Input` (external resistor), and that `IsPressed` is `true`.
- *Proves:* the fix uses the **wiring** (`_eventPinMode`), not the pin mode (§2.3). Red before the fix.
- *Doesn't prove:* the external pull-*down* case (same code path, not tested separately).

**(4) `If_Button_Is_Held_At_Startup_With_Debouncing_Release_Raises_ButtonUp_And_Press`**

```csharp
using GpioButton button = CreateButton(PinValue.Low, debounceTime: TimeSpan.FromMilliseconds(100)); // held, debounce on
button.ButtonUp += (sender, e) => buttonUp = true;
button.Press    += (sender, e) => pressed  = true;
_driver.Object.FireEventHandler(ButtonPin, PinEventTypes.Rising);   // the user lets go
Assert.True(buttonUp);  Assert.True(pressed);  Assert.False(button.IsPressed);
```

- *Proves:* **the issue's real consequence is gone**. Before the fix it failed at `Assert.True(buttonUp)`: the
  release was swallowed (§1.4). After, `ButtonUp` and `Press` both fire.
- *Doesn't prove:* anything about real time. Debounce timing never comes into play on this path; the guard only
  checks "is debounce on" and `IsPressed`.

### 4.5 Green, and the hygiene checks

- `004-green.txt`: **15/15 pass** (the 8 old tests + 7 new).
- `005-hygiene.txt`: a clean rebuild with **0 warnings**. That matters because this repo sets
  `TreatWarningsAsErrors=true` (`eng/Compilers.props`), so any warning would break their CI. **5 runs, 15/15 each**,
  to check nothing is flaky (pgrawehr's #2608 exists because the old button tests were). **Diff: 3 files, 98
  lines added, 0 removed.**

### 4.6 What none of the tests prove

| Not proven | Why | How it could be |
|---|---|---|
| Real hardware behaves this way | All fake | Run a tiny console app on a Pi with a real button held at startup (optional, a nice "verified on hardware" line) |
| The level is settled when read (§5.1) | The fake answers instantly | Only hardware, and it varies by board |
| The order question and its race (§5.2) | The fake fires callbacks synchronously, on the test thread; real drivers use background threads | Very hard to test reliably; argued in words instead |
| Other drivers (sysfs, libgpiod v1/v2, RaspberryPi3Driver) read correctly right after an edge subscription | Not exercised | Hardware run |

Saying this plainly, in the PR, is part of good engineering: reviewers trust a contributor who states the limits of
their evidence.

---

## 5. The open questions, and the comment you'll post

### 5.1 When to read (U1): the settle-time question

**Rule: right after the pull-up is turned on, the pin voltage may not have reached High yet.**

The internal pull-up is a resistor (tens of kΩ) charging whatever capacitance is on the line (the pin, the wire, the
button). That's an RC circuit: the voltage rises over some microseconds, more with long wires. Issue #1715 reports
exactly this: a false `Press` at startup because the line hadn't settled when edges were first watched. If our
`Read` lands during that rise on a pull-up line, it may see Low and report **pressed when nobody is touching it**.

Three options, and what each would mean for the code:

| Option | Code change | Trade-off |
|---|---|---|
| **(a) read immediately** (what we built) | none | Simplest. May misread on a slow line; a doc note could cover it |
| **(b) settle delay, then read** | add a short wait before `Read` | Blocks the constructor; "how long is enough" depends on the hardware |
| **(c) read lazily**, on first event subscription | move registration + read out of the constructor into the events' `add` accessors (#1715's agreed fix) | Fixes #1715 too, but it's a bigger change, in `ButtonBase` territory where #2608 is working |

### 5.2 In what order (U2): the "sample the level vs. enable the interrupt" race

**Rule: whichever order you pick, there's a moment where an edge can slip between "I read the pin" and "I know about
edges". We picked the order with the smaller moment.**

Because the driver calls `PinStateChanged` on **its own thread**, the constructor and a real release can overlap:

```
READ, then REGISTER  (alternative)                  REGISTER, then READ  (what we built)

ctor:   Read() → Low (pressed)                       ctor:   RegisterCallback(...)
user:   lets go  (edge!)  ← no callback yet:         ctor:   Read() → Low (pressed)
                            NOBODY hears it          user:   lets go → driver thread runs
ctor:   RegisterCallback(...)                                 PinStateChanged → IsPressed = false
ctor:   IsPressed = true        ✗ stale              ctor:   IsPressed = true     ✗ stale (overwrites)

window = the whole RegisterCallback call             window = between Read() returning and the
(on libgpiod that can start a thread)                assignment: a few instructions
```

Both can leave `IsPressed` wrong until the next edge. "Register, then read" shrinks the window from a whole method
call (which may start a thread) to a few instructions. Closing it **completely** needs a lock shared by the
constructor and the handlers, i.e. changes inside `ButtonBase` (out of scope, and in #2608's area). The existing
code has no locking at all today. This is the "lost-update window" the CLI found while checking the plan; it belongs
in the comment or the PR, stated honestly.

### 5.3 The comment, sentence by sentence

Revised now that the change exists (the last version was written before any code). **Don't post it until you can
say each row's "what it means" in your own words.**

> I'd like to pick this up if it's still wanted. On `main`, `GpioButton` never reads the pin's current level, so
> `IsPressed` stays `false` until the first edge. It's more than cosmetic: with debounce enabled,
> `HandleButtonReleased` returns early when `!IsPressed`, so the first release of a button held at startup is
> swallowed (no `ButtonUp`/`Press`).
>
> I have a small change ready: after registering the edge callback, read the pin once and set `IsPressed` from the
> active level (Low for pull-up, High for pull-down), without raising events. Tests use `MockableGpioDriver`; four
> fail on `main` and pass with the change, including the swallowed release. Two questions before I open a PR:
>
> 1. **Timing:** #1715 suggests the pull-up may not have settled right after `OpenPin`. Is an immediate read OK, or
>    would you prefer a short settle delay, or reading lazily on first subscription (#1715's proposed fix)?
> 2. **Ordering:** I read *after* registering the callback, so an edge between the two can't be missed entirely;
>    there's still a tiny window where a callback could be overwritten by the initial read, which only a lock in
>    `ButtonBase` would close. Is that acceptable here?
>
> I see @pgrawehr's #2608 touches the same files; my change doesn't touch `ButtonBase.cs`, so I'll rebase once it's
> in. @raffaeler, OK for me to take this?

| Sentence | What it means | Backed by |
|---|---|---|
| "if it's still wanted" | You're asking permission, not announcing. The issue is assigned to raffaeler as area owner | Etiquette: comment before code |
| "never reads the pin's current level" | §1.1 | Code: §1.3 |
| "first release … is swallowed" | §1.4, path B | **Verified**: test (4) red before the fix |
| "I have a small change ready" | You've done the work; you're not asking them to wait for you | Your branch, 2 commits |
| "four fail on `main` and pass with the change" | §1.5 and §4.5 | `003-red.txt`, `004-green.txt` |
| Question 1 | §5.1 | Reasoning + #1715 (not verified on hardware) |
| Question 2 | §5.2 | Reasoning (not testable with the fake) |
| "doesn't touch `ButtonBase.cs`" | §2.5: no overlap with #2608 | `006-diff.patch` |

**What posting commits you to:** doing the work if they say yes (it's already done), answering their questions
within a reasonable time (your own rule: within 48 hours), and accepting their direction on the two questions. It
does **not** commit you to a deadline, to defending our defaults, or to a bigger change than you're comfortable with:
if they want option (c), you can say it's larger than you planned and ask how they'd like to proceed.

### 5.4 Every likely answer, and what it would mean

| They say… | What changes | Size |
|---|---|---|
| "Looks good, go ahead" | Open the PR as is (plan Step 9) | none |
| "(b) add a settle delay" | A wait before `Read`; maybe a test that the delay is used | small |
| "(c) do it lazily, with #1715" | Registration and read move into the events' `add` accessors; touches `ButtonBase`; must coordinate with #2608 | medium-large: a plan revision; worth asking whether they'd like it as a follow-up PR instead |
| "Read before registering" | Swap two statements; tests unchanged | trivial |
| "Window is fine" / "add a lock" | Nothing / a lock in `ButtonBase` (coordinate with #2608) | none / medium |
| "@pgrawehr will fold it into #2608" | Nothing to submit; compare his version with ours (📖 learn mode) | — |
| "Not wanted" | Close it; keep the lecture and tests as learning | — |
| Silence by Sunday | Open the PR anyway; its description repeats the two questions | — |

---

## 6. Three small calls left for you (the CLI's open questions)

1. **Commit 2's message ends with `Fixes #2328`.** When you push a commit that mentions an issue, GitHub shows "…
   referenced this issue" on #2328, even from your fork. So push only **after** the comment is posted (Step 9 does),
   or remove the line and keep `Fixes #2328` only in the PR description (the repo's PR template wants it there as the
   first line anyway). Either is fine.
2. **The holding case** (§3.3, last row): mention it in the PR or not. A one-line "unchanged" note is enough.
3. **Test names** copy the style of the existing `ButtonTests.cs` (`If_…`), for local consistency.

---

## 7. Teach-back checklist

When you explain this back (a walk is perfect), try to cover:

1. Why a button held at startup is invisible to `GpioButton` (edges vs. levels).
2. Why that becomes a **lost click** when debounce is on (the guard in `HandleButtonReleased`).
3. What the two added lines do, and why `_eventPinMode` instead of `_gpioPinMode`.
4. Why the fix sets `IsPressed` instead of calling `HandleButtonPressed()` (two reasons, and why "#1715" isn't one).
5. How the tests run real `GpioButton` code with no Pi: `GpioDriver` as the seam, `MockableGpioDriver`, Moq,
   `CallBase`, and the two traps (unscripted `false` and unscripted `Low`).
6. What "red for the right reason" means, and which test proves the swallowed release.
7. What the tests can't prove, and why (instant answers, same-thread callbacks).
8. The two maintainer questions in your own words: settle time (RC) and the read/register race.
9. What posting the comment commits you to, and what it doesn't.
