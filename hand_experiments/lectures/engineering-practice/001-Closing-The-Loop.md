2026_08_08_16_11-Closing-The-Loop

# Closing the Loop
### Why you're being called slow, why the usual advice ("stop going deep") is wrong, and what to actually change

---

## 0. What this track is

`lectures/engineering-practice/` is a second lecture track, parallel to the topic folders (`kafka/`, `redis/`,
`reactive/`). Those teach *what a system is*. This one teaches *how to move through unfamiliar systems* — speed,
judgment, when to stop digging, how to debug, how to scope. Topic lectures use engineering practice as the
teaching material; this track uses your actual sessions as *its* material.

This first one exists because of a specific piece of feedback: senior engineers at work say you are **too slow**
and **too caught up in the details**. Today's Kafka session is the first well-instrumented example of that
happening, so we can move past self-report and look at evidence.

The companion document — the specific findings, with fixes — is
`lectures/kafka/Problems/001-Kafka-Problem-Log.md`. Read that one first if you haven't. This one is the argument
underneath it.

---

## 1. The headline, before anything else

> **You are not slow because you go deep. You are slow because your feedback loop is open.**

Those are different diseases and the standard prescription ("stop being such a perfectionist, just ship") treats
the wrong one. If you follow that advice you'll lose the thing that actually makes you good and you *still* won't
get faster, because the real cost isn't in the depth — it's in the gap between doing something and finding out
whether it worked.

Here is the evidence, from today, from your own git log:

| Commit | Time | Message | Compiled? |
|---|---|---|---|
| `bc4e6fb` | 15:36 | "my incorrect attempt" | No |
| `6bc52c0` | 15:50 | "attempting fixes" | No |
| `90aaadb` | 15:52 | "attempting fixes" | No |

Sixteen minutes, three commits, zero builds. At 15:52 the file contained five separate compiler errors, of which
you had located one. `dotnet build` — four seconds — would have named all five, with file and line numbers.

And the Kafka broker had never started once, because `docker-compose.yml` referenced a network called `taks-net`
while defining one called `task-net`. So during those sixteen minutes you were reasoning carefully about C# code
that could not compile, intended to talk to a broker that did not exist.

**Notice what's missing from that story: any deep dive.** You didn't lose the hour reading librdkafka source. You
didn't lose it understanding partition assignment. You lost it *guessing* — reasoning in your head about
questions the machine would have answered instantly and definitively. Depth wasn't the problem. Working without
a signal was.

---

## 2. Decoding the criticism

"Too slow" and "too caught up in details" are what a senior engineer says when they observe a *symptom*. They
almost never have a mechanism, because they're not in your head. It's worth separating what they probably
actually observed from what they said:

| What they said | What they most likely observed |
|---|---|
| "You're slow" | Long gaps where they can't tell if you're progressing or stuck |
| "Too caught up in details" | You answered a scoping question with implementation detail, or blocked on a question that didn't need answering yet |
| "You over-engineer" | You built four things when the task needed one |
| "Just get it working" | They have a working-system-first mental model and you have a understand-first mental model |

None of these are "you understand things too well." Nobody has ever been penalized for understanding a system.
People are constantly penalized for **being unpredictable to plan around**. When your manager can't tell whether
you're 20% or 80% done, and the answer keeps being "still investigating," you become a scheduling risk. That is
what "slow" means in a professional context far more often than raw throughput.

This matters for the Microsoft goal specifically: at that level, the bar isn't "writes code fast." It's
"reduces uncertainty fast, and communicates the reduction." Those are learnable and mechanical.

---

## 3. Why you got here (this part is not a character flaw)

You wrote this about yourself in `persona.md`:

> *"I have a blockage in my head about being comfortable using frameworks, abstractions, and systems which I do
> not fully understand... I feel I am UNABLE to move past this."*

Here is the thing worth hearing: **that instinct is a correctly-learned response to your actual professional
environment.** You do embedded C/C++, Raspberry Pi, microcontrollers, I2C/SPI, hardware/software integration. In
that world:

| | Embedded / firmware | Cloud backend |
|---|---|---|
| **Cost of one experiment** | Flash the device, attach a scope, maybe 2–10 minutes | `dotnet run`, 3 seconds |
| **Cost of being wrong** | Can brick hardware, damage components, corrupt a bus | Container restarts |
| **Depth of abstraction** | Thin — you can and must read down to the register | Deep — librdkafka is 200k lines of C you will never read |
| **Is the source readable?** | Yes, and reading it is often the *fastest* path | Sometimes, and it's usually the *slowest* path |
| **Correct strategy** | **Think hard, act once** | **Act cheaply, think about the result** |

In embedded, "understand it fully before you touch it" is not perfectionism — it's the rational strategy when
experiments are expensive and mistakes are physical. You didn't develop a bad habit. You developed a *good* habit
and then changed domains, and the habit is now mis-calibrated because the cost structure inverted underneath it.

That reframe matters practically, because it tells you what to change: not your standards, not your curiosity.
**Your estimate of what an experiment costs.** In backend work, running the thing is nearly free, and you are
still pricing it like flashing a board.

There's a second, sharper point hiding in your own self-assessment:

> *"if I attempt to utilize a component which I don't fully understand, I completely break functionality"*

Look at today against that claim. Your five blockers were: a duplicate constructor, a casing typo, a spelling typo,
a leftover line referencing deleted symbols, and a misspelled Docker network. **Not one of them came from
insufficient understanding of Kafka.** The Kafka parts you wrote — `Acks.All`, flushing before dispose, catching
`ProduceException` specifically, wanting the `DeliveryResult` — were *correct and well-informed*. You broke
functionality on things you already understood completely, because you weren't checking your work.

So the belief "I break things when I don't fully understand them" is, on today's evidence, false — and it's
expensive, because it's the belief that justifies the hyperfixation. The real correlation isn't depth-of-
understanding to breakage. It's **length-of-feedback-loop** to breakage.

---

## 4. Two characters

You learn best from personified components, so here are the two engineers living in your head.

### The Cartographer

The Cartographer will not take a step into unmapped territory. Before she moves she wants the full survey: every
river, every ridge, every switchback. Her maps are genuinely excellent — better than anyone else's — and when she
finally does move, she moves correctly and she never gets lost.

Her failure mode is that **the territory is bigger than she thinks, and most of it isn't on her route.** She
surveys the whole mountain range to walk one valley. Worse: she has no way to know which parts of her map are
wrong, because she's never walked any of it. Today she carefully mapped a producer that couldn't compile, a
contracts assembly nobody references, and two consumer projects that don't exist, on the far side of a broker
that never started.

### The Scout

The Scout walks the route first, badly. He trips, backtracks, gets a boot wet. Twenty minutes later he's at the
other end and he can tell you the four things that actually matter: where the crossing is, which ridge is
impassable, where he lost the trail.

*Then* he maps it — and his map is of a route that exists, annotated with the failures he personally hit.

The Scout is not less thorough than the Cartographer. He is thorough **second**. That's the entire difference, and
it's the whole lesson: the Scout ends up with a better map, because reality told him where to look.

**You are a strong Cartographer.** The persona file is full of evidence for it — the SPICE feasibility document
that verified every numerical claim with a from-scratch MNA solver, the Tool_Box capstone audit where you asked to
be attacked rather than praised. Those are senior habits and you should keep every one of them.

The correction isn't *become a Scout instead*. It's **scout first, map second.** Same depth, reordered.

---

## 5. The protocol

Five rules. They are mechanical on purpose — this is a problem you can't fix with resolve, because the moment you
hit a genuinely interesting question, resolve loses. It needs to be a process you follow when you don't feel like
it.

### Rule 1 — The Walking Skeleton

> **Before you build the second component, one message must travel the whole path.**

A walking skeleton is the thinnest possible end-to-end slice: ugly, hardcoded, no error handling, no abstraction —
but it *runs*, all the way through, and produces observable output.

Today's Kafka walking skeleton is:

```
one .NET process → real broker → one .NET process
```

That's it. No `Contracts` project. No `ConsumerTwo`. No JSON. No solution folders. Hardcoded topic, hardcoded
`localhost:9092`, `Console.WriteLine`. One message. Once that runs, you have earned the right to build the second
thing — and everything you build afterward is anchored to something known to work.

What you built instead: four projects, a solution file, a Compose file, a contracts record — and the skeleton
never walked. Every one of those decisions was made **in the dark**, and every one of them is a decision you now
have to re-examine.

*Why this is faster and not just cruder:* the skeleton doesn't skip the hard parts, it **front-loads** them. All
the genuinely unknown risk in this project — does the broker start, are the advertised listeners right, can a Mac
process reach the container, does the client library connect — lives in that first thin path. The parts you
deferred (a second consumer, a shared record type) carry near-zero unknown risk; you already know how to write a
record. You spent your first hour on the certain parts and left the uncertain ones untouched. That's backwards,
and it's the single highest-leverage thing to invert.

**The tell that you're violating this rule:** you're creating a second anything — project, service, class,
abstraction, config file — and the first one has never run.

### Rule 2 — The Signal Clock

> **Never go more than 10 minutes without a signal from the machine.**

A *signal* is the machine telling you something you didn't already know: a build result, a test result, a log
line, a returned value, a screenshot. Reading code is not a signal. Reasoning is not a signal. An LLM's opinion is
not a signal.

Set an actual timer for a week. When it goes off, whatever state you're in, get a signal — build it, run it, print
something. If the code doesn't compile, that *is* the signal: read the errors.

Today's loop was 16 minutes at minimum and arguably the whole session. The target is more like 90 seconds, and
`dotnet build` costs you four of them.

Concretely, for this project:

```bash
dotnet build                                    # after EVERY edit that touches >1 line
dotnet watch --project src/MyKafkaSystem.Producer   # or: never think about it again
docker compose logs -f kafka                    # the broker's side of the story
docker compose ps                               # healthy, or not
```

`dotnet watch` in particular converts the loop from "remember to check" to "cannot avoid knowing." Use it.

### Rule 3 — The Question Ledger

This is the rule that makes the other four survivable, and it's aimed directly at the "I feel UNABLE to move past
this" sentence.

Here's the mechanism behind that feeling: the compulsion to chase every detail isn't really intellectual hunger.
It's **fear of losing the question.** Somewhere you learned that a question you don't answer *now* is a question
you'll never answer, and possibly a landmine in your system. So you can't let go of it. That's not irrational —
it's just solved by writing it down.

Keep a file. `NOTES.md`, scratch pad, whatever. When curiosity fires mid-task, spend **fifteen seconds** writing
the question, and move on:

```markdown
## Open questions — kafka, 2026-08-08
- [ ] What does Acks.All actually wait for? All replicas, or min.insync.replicas?
- [ ] Why does Confluent.Kafka have `Null` AND `Ignore` as key types? What's the difference?
- [ ] Does ProduceAsync batch, or is it one network round-trip per message?
- [ ] What happens if I produce to a topic that doesn't exist? Auto-create — with how many partitions?
- [ ] Why two listeners in the compose file? What is "advertised" actually advertising?
```

Then, at the **end** of the session — after the skeleton walks — spend a fixed budget (30–45 minutes) working down
the list, hardest-first. Answer them in the topic lecture. Anything unanswered rolls forward.

Two things this buys you. First, the question is safe, so deferring it stops feeling like a loss. Second — and
this is the part that will surprise you — **roughly half of them answer themselves** during the build. Two of the
five above (`Null` vs `Ignore`, and auto-create partitions) got answered today just by reviewing the code against
the running system. Chasing them at 15:40 would have been pure waste.

### Rule 4 — Isolate the layer before you debug it

> **Before debugging your code against a system, prove the system works without your code.**

Today, before writing a single line of Kafka C#, the highest-value thirty seconds available were:

```bash
docker compose exec kafka kafka-topics --bootstrap-server localhost:9092 --list
```

That one command partitions the world. It works → the broker is fine, any failure is yours. It fails → stop
reading C# entirely.

Then the console tools:

```bash
docker compose exec kafka kafka-console-producer --bootstrap-server localhost:9092 --topic my-topic
docker compose exec kafka kafka-console-consumer --bootstrap-server localhost:9092 --topic my-topic --from-beginning
```

Now you have a **known-good reference implementation** running beside you. Every subsequent question becomes a
comparison instead of an investigation: "the console consumer sees it and mine doesn't → my consumer config" —
which is a five-minute problem instead of an afternoon.

This generalizes to everything: `curl` the API before debugging the client; `psql` the database before debugging
the ORM; ping the device before debugging the driver. You already do this instinctively with hardware — it's
exactly the logic-analyzer-before-the-firmware move. Carry it across.

### Rule 5 — The Two-Pass Rule (this is where the depth goes)

You do not have to give up understanding things completely. You have to **move it after the first working
version.**

**Pass one — Scout.** Use the library as a black box. Copy from the quickstart. Accept every default. Every time
you don't understand why something works, it goes in the Question Ledger, not into a source-code dive. Goal: it
runs.

**Pass two — Cartographer.** With the system running in front of you and a concrete list of questions, go deep.
And now the depth is *dramatically* more efficient, for reasons that are worth spelling out:

- **You can experiment.** "What does `Acks.All` wait for?" is a 90-second experiment against a live broker. In
  pass one it's a documentation archaeology project.
- **You know which questions are load-bearing.** Reality has already told you which parts of the system you touch.
  Most of the map you'd have drawn in pass one covers territory your route never enters.
- **The knowledge sticks.** Understanding attached to a system you built and a bug you hit is durable. Understanding
  read cold evaporates in about a week — which you've probably already noticed.

This is the reordering that lets you keep being who you are. You are not being asked to know less. You are being
asked to know it in a different order.

---

## 6. Triaging a detail in ten seconds

When a detail grabs you mid-task, three questions:

1. **Does the current step break if I'm wrong about this?**
   No → Ledger. (Most details. "Should the topic have 3 partitions?" — the skeleton works with 1.)

2. **Can I find out cheaply *later*, with the system running?**
   Yes → Ledger. (Almost everything about a running system. This is precisely why the skeleton comes first: it
   converts expensive questions into cheap ones.)

3. **Is being wrong here expensive to undo?**
   Yes → dig **now**. This is the real exception and it's a short list: data models and schemas, public API
   contracts, security boundaries, anything that will have callers you don't control, anything touching money or
   user data.

Two of three answers send you to the Ledger. That's not a coincidence — in backend work most details genuinely are
deferrable, and your current default treats all three branches as "dig now."

Worth noting explicitly: the `Contracts` project you built today *is* a category-3 concern — wire formats are
expensive to change once two services depend on them. Your instinct was right. But it only becomes expensive once
there are two services, and there were zero. **Right concern, wrong week.**

---

## 7. The conversation to have at work

Don't leave this as vibes. Feedback like "you're too slow" is unactionable, and you're entitled to ask for the
mechanism. Ask one of the seniors — ideally the one who said it — something close to:

> "You mentioned I move slowly on tasks. I'd like to fix that and I want to make sure I'm fixing the right thing.
> Can you walk me through a specific recent example? Ideally: what you'd have expected the first hour to look
> like, versus what mine looked like."

Why this phrasing works:

- **A specific example** forces them out of impression and into observation. Sometimes what they produce is
  something entirely different from what you assumed — e.g. "you didn't ask me a question for three days" or "you
  refactored a file I didn't ask you to touch."
- **"The first hour"** is the highest-signal window and the one that differs most between engineers. It's also
  exactly where today's problems lived.
- **"What you'd have expected"** asks them to describe *their own* process, which is the actual thing you want.
  Most seniors have never articulated it and will tell you something genuinely useful when asked directly.

Then, separately, make your work legible. A large fraction of "slow" is really "opaque." A daily one-liner —
*"skeleton end-to-end today; two open questions on offset commit semantics; on track for Thursday"* — costs
nothing and changes the perception substantially, because it converts you from a scheduling risk into someone
whose state is known.

And be open to the possibility that they're partly wrong. Some teams call any non-copy-paste engineer slow. But
today's git log says there's a real thing here, so assume good faith and fix the real thing first — then you'll be
in a much stronger position to push back on the rest.

---

## 8. What not to change

Since this document is mostly criticism, be precise about what's working, because the failure mode of feedback
like this is overcorrecting into someone who ships fast and understands nothing.

Keep all of this:

- **Asking to be audited.** After Tool_Box 1.0 you chose a critical review over a flattering one. That's rare and
  it's senior.
- **Deriving feasibility before committing.** The SPICE concepts document found that the intuitive difficulty
  ordering was backwards — the kind of finding that saves a project in week one instead of sinking it in week
  three.
- **Verifying claims instead of recalling them.** You wrote an MNA solver to check numbers. Almost nobody does
  this.
- **Learning by hand, deliberately, without AI writing the code.** This repo's own rule. It's why today produced
  useful evidence at all.
- **Wanting the mechanism, not the incantation.** Keep it. Just move it to pass two.

None of that is the problem. The problem is a sixteen-minute gap between typing and finding out. That's it.

---

## 9. The drill

Next session, run this and record the numbers at the bottom of the Kafka problem log.

**Before you start:**

- One sentence, written down: *what does the walking skeleton do?* (Today's: "one C# process sends a string, one C#
  process prints it, through a real broker.")
- Open `NOTES.md` for the Question Ledger.
- Timer, 10 minutes, repeating.

**During:**

- Every timer tick: get a signal. No exceptions.
- Every urge to dig: Ledger, 15 seconds, move on.
- Every new project/file/abstraction: ask *"has the current one run yet?"* If no, stop.

**After:**

- Work the Ledger for 30–45 minutes. Write the answers into the topic lecture.
- Record: **time to first signal**, **time to walking skeleton**, **longest gap without a signal**, **Ledger
  entries deferred**, **Ledger entries that turned out to be irrelevant**.

Track those five numbers over sessions. That last one is the most persuasive — once you've watched thirty
questions expire as irrelevant, deferring the thirty-first stops feeling dangerous, which is the actual thing that
has to change.

**Today's baseline**, for comparison:

| Metric | Today |
|---|---|
| Time to first signal | Never — nothing ran |
| Time to walking skeleton | Not reached |
| Longest gap without a signal | The entire session |
| Projects created before one ran | 4 |
| Compiler errors present but undiscovered | 5 |

Every one of those has enormous room to move, and none of them requires you to care less about how things work.

---

## 10. One sentence to keep

> **Scout first, map second. Same depth, better order.**
