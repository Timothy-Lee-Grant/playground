# Scouting 002 — First code PR, week of 2026-09-29

> **Date researched:** Tue 2026-09-29 · **Scope:** dotnet/iot, dotnet/yarp, dotnet/aspnetcore
> **Goal:** open a real code PR (not docs) by Sun 2026-10-04. Merge timing is up to the maintainers.
> **Filter used:** code change + test, no open PR from anyone, nobody claiming it in comments, small enough for
> weeknight study + a weekend of work, buildable on the Mac or the desktop, no special hardware.
> **Labels:** 🟢 open, available · 🟡 open, claimed · ⚫ closed · 🛠️ contribute · 📖 learn · ⚓ anchor · 🧭 explorer

Why the last two slipped away: #2403 and #2600 were already in motion (Copilot agent PRs, a reasoned root-cause
comment). This time every pick below was checked for linked PRs, PR searches and claim comments, and the "ask
first" step comes **before** any code, so you find out on Tuesday, not Saturday.

---

## At a glance

| Rank | Issue | State · Intent | Kind | Effort | Build on | Why |
|---|---|---|---|---|---|---|
| **1** | [iot #2328: `GpioButton.IsPressed` not initialized](https://github.com/dotnet/iot/issues/2328) | 🟢 · 🛠️ | ⚓ bug fix + unit test | 6–10 h | Mac or desktop (SDK 9, roll-forward) | Confirmed still broken on `main`; no PR, no claim; tiny, testable without hardware |
| **2** | [YARP #275: remove Autofac from tests](https://github.com/dotnet/yarp/issues/275) (phase 1 only) | 🟢 · 🛠️ (ask first) | 🧭 test refactor | 8–12 h | **Mac or Codespaces** (needs .NET 11 RC1 SDK) | `help wanted`, 0 comments, no PR ever; exactly 7 test files; fills your empty "Testing infrastructure" row |
| 3 | [iot #1715: Button fires `Press` at startup](https://github.com/dotnet/iot/issues/1715) | 🟢 · 🛠️ (ask first) | ⚓ behavior fix | 6–10 h | Mac or desktop | Fix already agreed in triage; same files as #2328, natural second PR |
| 4 | [iot #1663: `Mcp23xxx` ctor resets all pins](https://github.com/dotnet/iot/issues/1663) | 🟢 · 🛠️ | ⚓ bug + small API addition | 8–12 h | Mac or desktop | Register-level, has mock-driver tests; adds a ctor parameter, so review is slower |

**Recommendation:** do **#1** as this week's PR. Post the "ask first" comment on **#2** tonight too, so its answer
is waiting for you; if the YARP maintainers say yes, #2 becomes next week's PR (one active PR at a time, §6.7).

---

## 1. iot #2328 — `GpioButton.IsPressed` is always `false` right after construction ⚓

**State (checked 2026-09-29):** open, `bug` + `up-for-grabs`, Priority 3. Assigned to raffaeler (dotnet/iot assigns a
maintainer as area owner; `up-for-grabs` means they still want outside help). One maintainer comment (krwq,
2024-06-27). No linked PR; a PR search for `IsPressed` finds nothing relevant.

**The bug.** `ButtonBase.IsPressed` starts as `false` and only changes inside `HandleButtonPressed/Released`, which run
on an **edge**. If the button is already held down when the app starts, there's no edge, so `IsPressed` lies until the
first release. Confirmed on `main` (commit 2026-09-11):

```csharp
// ButtonBase.cs:73
public bool IsPressed { get; set; } = false;

// GpioButton.cs ctor: opens the pin, subscribes to edges, never reads the current level
_gpioController.OpenPin(_buttonPin, _gpioPinMode);
_gpioController.RegisterCallbackForPinValueChangedEvent(_buttonPin, PinEventTypes.Falling | PinEventTypes.Rising, PinStateChanged);
```

**Likely fix (a few lines).** After `OpenPin`, read the pin once and set the initial state from the active level:
pull-up wiring → pressed = `Low`; pull-down → pressed = `High`. Setting the state must **not** raise
`ButtonDown`/`Press` (that would recreate #1715).

**The real work is the test.** `Button.Tests` only tests a `TestButton` that bypasses `GpioButton`. You'd add a test
that builds a `GpioButton` over a fake driver that reports "already low", and asserts `IsPressed == true` (plus the
pull-down mirror case). The repo already has the pattern: `src/System.Device.Gpio.Tests/MockableGpioDriver.cs` and
the Mcp23xxx/Pcx857x tests.

**Coordination risk: pgrawehr's open [PR #2608](https://github.com/dotnet/iot/pull/2608)** ("The button tests are still
flaky", 2026-09-27) rewrites `ButtonBase.cs`, `GpioButton.cs` and `ButtonTests.cs` (adds a `TimeSource`). It doesn't
touch `IsPressed` initialization, but you'll conflict. Mention it in your comment and offer to build on top of it.
Upside: a maintainer is actively in these files right now, so review is likely to be quick.

**Concepts you'll meet:** test doubles for hardware (`GpioDriver` as a seam), what "state vs. edge" means in an
event-driven driver, the constructor-side-effects problem, rebasing onto someone else's in-flight PR.

**Hardware:** none required. A Pi + a button is a nice extra "verified on hardware" line in the PR.

---

## 2. YARP #275 — clean Autofac (and later Moq) out of the tests 🧭

**State (checked 2026-09-29):** open, `help wanted` + `Type: Task`, 0 comments since 2020. PR searches for `Autofac`
and `Moq` find no attempt. Nobody claims it.

**Measured scope on `main`:** Autofac is used by **one helper** (`test/Tests.Common/TestAutoMockBase.cs`, wraps
`Autofac.Extras.Moq.AutoMock`) and **7 test classes** that inherit it:

| File | Lines |
|---|---|
| Forwarder/StreamCopierTests.cs | 338 |
| Forwarder/ForwarderMiddlewareTests.cs | 166 |
| Forwarder/ForwarderHttpClientFactoryTests.cs | 417 |
| Model/ProxyPipelineInitializerMiddlewareTests.cs | 173 |
| LoadBalancing/LoadBalancingPoliciesTests.cs | 182 |
| Delegation/HttpSysDelegatorTests.cs | 359 |
| Delegation/HttpSysDelegatorMiddlewareTests.cs | 160 |

Phase 1 = rewrite those 7 to build the class under test with plain `new` (keep Moq for now), delete
`TestAutoMockBase`, drop the Autofac package refs from two `.csproj`s and `eng/Versions.props`. Moq is in 25 files;
leave that for later PRs, a folder at a time.

**Ask first, because:** the issue is six years old. The comment should propose phase 1 only, list the 7 files,
and ask whether they still want it. (A side point worth one sentence: Moq is pinned at 4.18.4, before the 2023
SponsorLink controversy, which is one more reason some .NET repos have been moving off it.)

**Build constraint:** YARP's `global.json` pins **SDK 11.0.100-rc.1**, which won't run on the desktop (x86-64-v2).
Use the Mac (clone is small) or Codespaces.

**Concepts you'll meet:** auto-mocking containers vs. explicit construction, DI in tests, how YARP's forwarder,
load-balancing and HttpSys delegation are wired (reading 7 test files is a guided tour), Microsoft.Testing.Platform.

---

## 3. iot #1715 — Button raises `Press` at startup (backup / follow-on)

Open since 2021. raffaeler's triage comment (2021-11-11) states the agreed fix: move the native pin subscription
out of the constructor into the button events' `add` accessors; keep removal in `Dispose`. He said in 2024 he'd do
it, and hasn't. Same files as #2328 and #2608, so do it **after** #2328, and ask raffaeler if he minds you taking it.
Trickier than it looks: five events (`ButtonDown`, `ButtonUp`, `Press`, `DoublePress`, `Holding`) share one native
subscription, so you subscribe on the first `add` from any of them, thread-safely.

## 4. iot #1663 — `Mcp23xxx` constructor zeroes every pin (backup)

The ctor always writes `IODIR=0xFF, GPIO=0x00, IPOL=0x00`, so a service restart drops every relay. The maintainer
asked for a non-breaking opt-out (an extra ctor `bool`, following the #1479 precedent). Register-level and has mock
tests (`Mcp23xxxTest.cs`), but it adds public API, so review may take longer than a week.

---

## Looked at and ruled out this week

| Item | Why not now |
|---|---|
| **dotnet/aspnetcore** (any issue) | In Jan 2026 a bot marked many old issues as community candidates, and they're being claimed fast: #5902 (PR #69402, Sep 19), #5938 (PR #69106), #4382 (volunteer, Aug 28). The repo is also a very heavy build (too much for the Mac's ~20 GB; Codespaces only). Worth a scouting pass later, not for a Sunday deadline. |
| YARP #3016 (container logging) | davidfowl's own feature spec; he's building the container features himself (#3022). |
| YARP #3051 (ingress TLS key) | The reporter already has draft PR #3054. |
| YARP #3033 (K8s `WatchAsync`) | An idea, not agreed; needs a Kubernetes setup. |
| iot #2600, #2602, #2604 | Copilot/community PRs already open (#2601, #2605). 📖 learn only. |
| iot #1887 (Pca9685/MotorHat) | Reasonable, but assigned to pgrawehr with a design choice (which ctor to call) that needs his input first. Keep for a later list. |

---

## Draft comments (edit into your own voice before posting)

**#2328:**
> I'd like to pick this up if it's still wanted. I checked `main` and `GpioButton` still never reads the pin's
> current level, so `IsPressed` stays `false` until the first release edge. My plan: after `OpenPin`, read the pin
> once and set the initial state from the active level (Low for pull-up, High for pull-down), without raising
> `ButtonDown`/`Press`, plus unit tests using a mockable `GpioDriver` for both cases. I see @pgrawehr's #2608 is
> reworking the same files; I'm happy to base my change on it once it merges, or on `main` if you prefer.
> @raffaeler, OK for me to take it?

**YARP #275:**
> Is this still wanted? I'd like to take a first, bounded step: remove Autofac only. It's used by
> `TestAutoMockBase` and 7 test classes (listed). I'd construct the classes under test directly, keep Moq as is,
> and delete the helper and the package refs. Moq removal could follow folder by folder in later PRs if you'd like.
