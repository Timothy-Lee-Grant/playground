# Scouting 002 — Conversation Log: choosing the first code PR

> **What this is:** the single, linear record of choosing between the two picks in
> [`../002-code_pr_shortlist_week_of_2026-09-29.md`](../002-code_pr_shortlist_week_of_2026-09-29.md):
> questions, answers and decisions **in the order they happened**. Once an issue is chosen, the work moves to its own
> issue folder (`iot/2328_*/` or `yarp/275_*/`) and this log ends with a link there.
>
> **Rules:** append only; only "Where we are now" and the Index are edited in place. Anything not backed by a run
> is labeled **unverified**.
>
> **Entry types:** 📍 Progress · ❓ Question (Timothy, in his words) → 💬 Answer · 🧭 Decision · 🔁 Correction
>
> **Started:** 2026-09-29 (Tuesday). **Deadline:** PR opened by Sunday 2026-10-04.

---

## Where we are now *(updated in place)*

| | |
|---|---|
| **Stage** | Choosing. Nothing posted upstream yet. |
| **Last entry** | #1 (2026-09-29) |
| **Open decisions** | (1) Which issue is this week's PR (§5). (2) Whether to post the "ask" comment on the other one too. |
| **Next step** | Timothy reads entry #1 and asks questions. Then decide, then post the comment(s). |

---

## Index

| # | Date | Type | Title |
|---|---|---|---|
| 1 | 2026-09-29 | 📍 Briefing | The two candidates in depth: what they are, the work, the skills, the risks, and a recommendation |

---

## #1 · 2026-09-29 · 📍 Briefing: the two candidates in depth

Source code below was read from both repos' `main` branches on 2026-09-29 (dotnet/iot `1eb0b2f`, YARP `0cae8ca`,
both dated 2026-09-11). Issue state was checked the same day.

### 0. The short version

| | **A. dotnet/iot #2328** | **B. YARP #275** |
|---|---|---|
| **One line** | A button that's already held down when the program starts is reported as "not pressed" | Rip an old test helper library (Autofac) out of YARP's test suite |
| **Kind of work** | Bug fix: small code change, a new test harness, one real design question | Refactor: no behavior change, 7 test files rewritten, packages removed |
| **What's hard** | Getting the *first* reading right (see §1.5, it's subtler than it looks) | Doing a lot of careful mechanical edits without changing what any test proves |
| **Size of the diff** | ~10 lines of product code + ~80–150 lines of tests | ~7 files touched, net *removal* of code, 3 package references deleted |
| **Where you build it** | Mac or desktop (SDK 9, rolls forward to 10) | Mac or Codespaces only (needs the .NET 11 RC1 SDK) |
| **Your strengths it uses** | GPIO, pull-ups, debounce, hardware-to-software mapping | Reading an unfamiliar codebase, DI, careful refactoring |
| **What's new for you** | Test doubles for hardware; constructor side effects; building on someone else's open PR | Test doubles in depth (mocks, auto-mocking); a big Microsoft repo's build system; YARP internals |
| **Risk of the ground moving** | Medium: a maintainer has an open PR in the same files right now | Low: nobody has touched it in 6 years. The real risk is "not wanted anymore" |
| **Fit for Sunday** | ✅ Good | ⚠️ Possible, but only if a maintainer answers by ~Thursday |

**My recommendation (details in §5):** A for this week. Post the "ask" comment on B tonight too, so B is lined up
as next week's PR.

---

### 1. Candidate A — dotnet/iot #2328: `GpioButton.IsPressed` is wrong at startup

#### 1.1 Purpose first: what the `Button` binding is for

dotnet/iot is a **library** (you call it; it doesn't call your `Main`). Inside it, `Iot.Device.Button` turns a raw
GPIO pin into a *button* with human-level events: "pressed", "released", "clicked", "double-clicked", "held down".
It's the software half of a job you've done in firmware: pin edges in, debounced button events out.

#### 1.2 The cast of characters

| Character | Job title | What they do | Where |
|---|---|---|---|
| **`GpioController`** | The pin switchboard | Opens pins, reads levels, and calls you back when a pin's edge changes | `System.Device.Gpio` |
| **`GpioDriver`** | The hardware whisperer | The layer under the controller that actually talks to libgpiod/sysfs. **This is the seam tests replace** | `System.Device.Gpio` |
| **`GpioButton`** | The wiring translator | Knows how the button is wired (pull-up or pull-down), so it knows whether "Low" means pressed or released. Converts edges into "pressed"/"released" | `src/devices/Button/GpioButton.cs` |
| **`ButtonBase`** | The button's brain | Hardware-independent. Keeps the state (`IsPressed`), does debounce, runs the holding timer, decides which events to raise | `src/devices/Button/ButtonBase.cs` |
| **Your app** | The listener | Subscribes to `ButtonDown`, `ButtonUp`, `Press`, `DoublePress`, `Holding`; reads `IsPressed` | — |

The split is deliberate: `ButtonBase` has no idea a GPIO pin exists, so the same logic works for any source of
"pressed/released" (the tests use a `TestButton` that calls the handlers directly).

#### 1.3 Control flow today

```
 new GpioButton(pin: 17, isPullUp: true)
   │
   ├─ ButtonBase ctor:   IsPressed = false          ◄── a guess, never checked against the pin
   ├─ controller.OpenPin(17, InputPullUp)
   └─ controller.RegisterCallbackForPinValueChangedEvent(17, Falling|Rising, PinStateChanged)
                                                     (from now on, only EDGES reach the button)

 Later, the pin changes:
   Falling edge ─► PinStateChanged ─► pull-up? yes ─► HandleButtonPressed()  ─► IsPressed = true,  ButtonDown
   Rising edge  ─► PinStateChanged ─► pull-up? yes ─► HandleButtonReleased() ─► IsPressed = false, ButtonUp, Press
```

The rule: **the button learns its state only from edges.** A level that's already there at startup produces no
edge, so the button never hears about it.

#### 1.4 The bug, and what it actually breaks

The issue reports only the property being wrong. Reading `ButtonBase` shows it's worse than that. There are two
cases when the button is held down at startup and then released (**unverified**: this is from reading the code;
the first job of the test is to prove it):

| Case | What happens on the release | Effect on the app |
|---|---|---|
| No debounce (`debounceTime = 0`, the default) | `HandleButtonReleased` runs: `ButtonUp` and `Press` fire, **with no `ButtonDown` before them** | An orphan "click" the app never saw start |
| Debounce on | `HandleButtonReleased` starts with `if (_debounceTime.Ticks > 0 && !IsPressed) return;` | **The release is swallowed completely.** No `ButtonUp`, no `Press`. The first press is lost |

So the wrong initial value isn't cosmetic; with debounce enabled it silently eats a user action. That's a
stronger PR description than the issue itself has, and it's something you found, not something handed to you.

#### 1.5 The fix, and the one real design question

The obvious fix goes in the `GpioButton` constructor, right after `OpenPin`:

```csharp
// sketch, not final
PinValue initial = _gpioController.Read(_buttonPin);
IsPressed = _eventPinMode == PinMode.InputPullUp ? initial == PinValue.Low : initial == PinValue.High;
```

Two rules for it:

1. **Set the state; raise no events.** Raising `ButtonDown` at construction would recreate issue #1715.
2. **Mind the external-resistor case.** With `hasExternalResistor: true` the pin mode is plain `Input`, but
   `_eventPinMode` still records the wiring, so the formula above still picks the right active level. A test
   should cover it.

**The design question: can the first reading be trusted?** Issue #1715 (same binding, open since 2021) says the
button fires a false `Press` at startup because right after the pull-up is switched on, the pin voltage hasn't
settled yet: the RC time of the pull-up and the line capacitance. If that's true, then **a `Read()` immediately
after `OpenPin` can see the not-yet-settled level**: on a pull-up line it may read Low and report "pressed" when
nothing is touching the button. Firmware analogy: reading a GPIO in the same instruction you enabled the internal
pull-up.

Options to put to the maintainers in the issue comment (don't pick one alone):

| Option | Idea | Trade-off |
|---|---|---|
| a | Read once immediately | Simplest. Can be wrong on a slow line; would need a doc note |
| b | Short settle delay (e.g. a few ms) before the read | Blocks the constructor; how long is enough is hardware-dependent |
| c | Read lazily: take the reading the first time someone subscribes to an event (the fix #1715 already agreed on moves subscription into the event's `add`) | Solves #2328 and #1715 together, but it's a bigger change and it touches the files in PR #2608 |

Raising this question is exactly the kind of thing that makes maintainers trust a new contributor. It shows you
read the neighboring issue and understand the hardware.

#### 1.6 The test: this is most of the work

Today's tests (`ButtonTests.cs`, 8 tests) only drive `TestButton`, which skips `GpioButton` entirely. So nothing
tests the wiring translator. You'd add the harness the repo already uses elsewhere:

```
  your test ──► new GpioButton(17, isPullUp: true, gpio: controller)
                                                   │
                                      new GpioController(fakeDriver)
                                                   │
                            Mock<MockableGpioDriver>  ◄── you script it: "pin 17 reads Low",
                                                          "pin mode InputPullUp is supported"
```

- The pattern exists in `src/devices/Ili934x/tests/` (`MockableGpioDriver.cs` + `new Mock<MockableGpioDriver>()`
  + `new GpioController(_gpioDriverMock.Object)`). Moq is already available to every device test project via
  `eng/Versions.external.props`.
- Tests to write (each fails before the fix, passes after): pull-up + Low → pressed; pull-up + High → not pressed;
  pull-down mirror; external resistor; and the §1.4 swallowed-release scenario (fire a Rising edge through the fake
  driver with debounce on and assert `ButtonUp` fired).

#### 1.7 The coordination risk: PR #2608

pgrawehr (a maintainer) opened [PR #2608](https://github.com/dotnet/iot/pull/2608) on 2026-09-27, "The button tests
are still flaky". It changes `ButtonBase.cs`, `GpioButton.cs`, `ButtonTests.cs` and `TestButton.cs`, replacing
`DateTime.UtcNow` with an injectable time source. It does **not** touch `IsPressed` initialization.

What that means for you:

- **Merge conflicts are likely.** Plan to base your branch on his PR (or rebase after it merges). Rebasing onto a
  moving target is a real professional skill; this is a gentle first exposure to it.
- **Good news:** a maintainer is working in these exact files right now, so your comment will probably be read by
  someone who has the code loaded in their head.
- **The risk:** he could decide to fold the fix into #2608. That's why the comment goes out first, and why it
  mentions #2608 by name.

#### 1.8 Skills and concepts

| Using what you already have | Meeting something new |
|---|---|
| Pull-up/pull-down wiring, active-low logic | Test doubles for hardware: replacing `GpioDriver` so logic runs with no Pi |
| Debounce, edge vs level | Moq: `Setup`, `Returns`, `Verify`; loose vs strict mocks |
| RC settle time on a GPIO line | Constructor side effects and why they make classes hard to test |
| Reading a driver's control flow | Working on top of someone else's open PR (branching, rebasing) |
| | xUnit in a big repo; running one test project with `dotnet test` |
| | Making a behavior-change argument in a PR (who could this break?) |

Exposure map rows it fills: Hardware protocols & drivers (to *Contributed*), Testing infrastructure (new),
Error handling & API compatibility (a behavior change in a public binding).

#### 1.9 A week on A

| Day | Work | Done when |
|---|---|---|
| **Tue (tonight)** | Post the comment on #2328 (draft in the shortlist, plus the §1.5 settle question) | Comment is live |
| Wed | Fork + clone dotnet/iot (outside `exercises`), build and run `src/devices/Button/tests` | 8 existing tests pass on your machine |
| Thu | Read `ButtonBase` + `GpioButton` + PR #2608's diff; copy the mock-driver pattern into Button tests | One trivial `GpioButton` test runs against the fake driver |
| Fri | Write the failing tests from §1.6 | Red tests that prove the bug, including the swallowed release |
| **Sat** | Apply the fix the maintainers prefer (or option a with a note if no reply); optional: verify on a Pi with a real button | Tests green; evidence saved |
| **Sun** | Rebase on `main`/#2608, write the PR description, open the PR | PR link in this log |

---

### 2. Candidate B — YARP #275: take Autofac out of YARP's tests

#### 2.1 Purpose first: why anyone would want this

YARP's tests use **Autofac.Extras.Moq** ("AutoMock"): a tool that builds the class under test for you and invents a
fake for every constructor parameter automatically. The issue's author (2020) argues this doesn't pay for itself:
the tests end up about as long without it, and they're harder to read because **the test no longer shows what the
class needs**. Removing it means one less dependency and tests that state their inputs explicitly.

This is a *refactor*: every test must prove exactly what it proved before. Nothing about YARP's behavior changes.

#### 2.2 The cast of characters

| Character | Job title | What they do |
|---|---|---|
| **The class under test** (e.g. `ForwarderMiddleware`) | The subject | Takes its dependencies through its constructor (constructor injection) |
| **Moq** | The prop department | Makes a stand-in object for an interface; you script its answers (`Setup`) and check it was called (`Verify`) |
| **AutoMock** (Autofac.Extras.Moq) | The casting agent | Given a class, looks at its constructor, asks the prop department for a stand-in for every parameter, and builds the class. Hands you the same stand-in later if you ask for it by type |
| **Autofac** | The agent's agency | A full DI container. It's only here because AutoMock is built on it |
| **`TestAutoMockBase`** | YARP's front desk for the agent | A base class that gives tests `Create<T>()`, `Mock<T>()`, `Provide<T>()` |

What a test looks like with the casting agent vs. without:

```csharp
// today (AutoMock): the constructor's 4 parameters are invisible
Mock<IHttpForwarder>().Setup(...);
var sut = Create<ForwarderMiddleware>();

// after: every dependency is named at the call site
var forwarder = new Mock<IHttpForwarder>();
forwarder.Setup(...);
var sut = new ForwarderMiddleware(next, NullLogger<ForwarderMiddleware>.Instance, forwarder.Object, randomFactory);
```

`ForwarderMiddleware`'s real constructor is
`(RequestDelegate next, ILogger<ForwarderMiddleware> logger, IHttpForwarder forwarder, IRandomFactory randomFactory)`,
and it throws on any `null`, so every one of those needs a real value or a stand-in.

#### 2.3 Measured scope

| File | Uses the helper how | Effort |
|---|---|---|
| `Forwarder/StreamCopierTests.cs` | Inherits `TestAutoMockBase` but doesn't use it | Trivial: delete `: TestAutoMockBase` |
| `Forwarder/ForwarderHttpClientFactoryTests.cs` | Only `Mock<ILogger<…>>()` | Small |
| `LoadBalancing/LoadBalancingPoliciesTests.cs` | `Provide<IRandomFactory>` + 6× `Create<…Policy>()` | Small: each policy has 0–1 ctor params |
| `Forwarder/ForwarderMiddlewareTests.cs` | `Create<ForwarderMiddleware>()` + shared `Mock<IHttpForwarder>()` | Medium |
| `Model/ProxyPipelineInitializerMiddlewareTests.cs` | `Provide<RequestDelegate>` + `Create<…>()` | Medium |
| `Delegation/HttpSysDelegatorMiddlewareTests.cs` | `Provide`, `Create`, several `Mock<T>()` | Medium |
| `Delegation/HttpSysDelegatorTests.cs` | ~13 `Mock<T>()` calls with mocks wired into other mocks | Largest |

Then: delete `test/Tests.Common/TestAutoMockBase.cs`, remove the `Autofac` and `Autofac.Extras.Moq` package
references from two `.csproj` files, remove two version properties from `eng/Versions.props`. Moq stays (25 files
use it directly; removing Moq would be a separate, much larger discussion).

#### 2.4 The subtle part: "same test, different wiring"

The trap in this kind of refactor is making a test **pass for a different reason than before**. Two places to watch:

- **Shared stand-ins.** With AutoMock, `Mock<IHttpForwarder>()` in the test and the forwarder injected into the
  middleware are the *same object*. When you rewrite, you must pass that same mock into the constructor, or the
  `Setup`/`Verify` calls silently check a different object.
- **Loose defaults.** AutoMock makes *loose* mocks: any call nobody scripted returns a default (`null`, `0`). If a
  dependency was silently returning defaults before, your hand-built version must behave the same (usually a loose
  `new Mock<T>()` or a `NullLogger`).

A good self-check: temporarily break the product code a test is meant to guard, and confirm the rewritten test goes
red. Evidence of that belongs in the `sample/` folder.

#### 2.5 Build and platform notes

- `global.json` pins **SDK 11.0.100-rc.1**. `./restore.sh` downloads that SDK into a `.dotnet/` folder inside the
  clone. It still won't run on the desktop (x86-64-v2), so: **Mac or Codespaces**.
- Tests run on Microsoft.Testing.Platform (`./test.sh`, or `dotnet test` on the one project).
- **Unverified:** whether the two `HttpSysDelegator*` test files run on macOS. HttpSys is Windows-only, so they may
  be skipped or compiled out there. If so, you can't run the hardest file locally and must rely on the PR's CI
  (which runs on Windows). Check this on Wednesday; it changes the risk.

#### 2.6 Skills and concepts

| Using what you already have | Meeting something new |
|---|---|
| Reading unfamiliar code carefully | Test doubles in depth: mock vs stub vs fake; loose vs strict; auto-mocking containers |
| DI and constructor injection (from your .NET service work) | Why reviewers value tests that "show their inputs" |
| Git discipline | A Microsoft repo's build system (Arcade, repo-local SDK, `eng/Versions.props`) |
| | A guided tour of YARP's core: forwarder middleware, load-balancing policies, pipeline initialization, HttpSys delegation |
| | Behavior-preserving refactoring and proving you preserved it |

Exposure map rows it fills: Testing infrastructure (new), Build/packaging & CI (new), Networking & proxies
(deeper, via YARP's internals).

#### 2.7 A week on B

Same shape as A, but everything hinges on a "yes, still wanted" from a YARP maintainer. Six-year-old `help wanted`
issues can get "we'd rather not churn the tests now". If no answer by ~Thursday, opening the PR anyway is allowed
but risky; better to keep B for next week.

---

### 3. Side-by-side on your criteria (from `open_source_persona.md` §5)

| Criterion | A: iot #2328 | B: YARP #275 |
|---|---|---|
| Exposure value | Medium-high: testing with hardware fakes, behavior-change reasoning | High: test design, a big Microsoft build, YARP's internals |
| Strength fit | **High**: pull-ups, debounce, settle time | Medium: careful reading, DI |
| Feasibility | ✅ any of your machines, no hardware needed | ⚠️ Mac/Codespaces only; one file may not run on macOS |
| Scope & maintainer signal | ✅ `up-for-grabs`, maintainer active in the area | ⚠️ `help wanted`, but silent since 2020 |
| Career signal | Bug fix in a Microsoft repo with a hardware story | Cleanup in YARP (Microsoft's reverse proxy); good backend story |
| Discovery (what you'd find yourself) | The swallowed release (§1.4) and the settle-time question (§1.5) | Mostly mechanical; the discovery is YARP's architecture |
| Ground moving under you | Medium (PR #2608) | Low |

### 4. What you'd need to study (either way)

Short list, in the order you'd hit it. A lecture on any of these is available on request, but only if it helps;
running the code comes first.

| Topic | A | B | Why |
|---|---|---|---|
| Test doubles: mock, stub, fake; Moq's `Setup`/`Returns`/`Verify`; loose vs strict | ✅ | ✅ | The core skill in both |
| xUnit basics in a big repo (`[Fact]`, `[Theory]`, running one project) | ✅ | ✅ | |
| Fork → branch → rebase → PR flow on GitHub | ✅ | ✅ | A adds rebasing onto #2608 |
| `GpioController`/`GpioDriver` split and how a fake driver plugs in | ✅ | | §1.6 |
| Constructor injection and what a DI container does | | ✅ | §2.2 |
| Repo-local SDKs and Arcade (`restore.sh`, `global.json`) | | ✅ | §2.5 |

### 5. Recommendation

**Do A this week; queue B.**

1. A is the right size for Sunday, builds on any machine, and uses your hardware instincts in a way reviewers will
   notice (§1.4, §1.5). It still gives you a real code PR with real tests, not a one-line change.
2. The main risk on A (PR #2608) is handled by commenting tonight and naming it.
3. Post B's comment tonight too. It costs five minutes, and a "yes" from a YARP maintainer turns B into next week's
   PR without the sand moving. (Your rule is one active *PR* at a time; questions can run in parallel.)

**Decision needed from you:** A, B, or A-now/B-queued. Once decided, the work moves to its own issue folder with a
`CLAUDE.md`, `conversation/`, `sample/` and `report/`, and this log closes with a link.

### Takeaways / next steps

1. Read this entry and ask questions (entry #2 onward).
2. Decide (🧭 entry).
3. Post the comment(s). Claude can adapt the drafts in the shortlist to include the §1.4 finding and the §1.5
   question.
