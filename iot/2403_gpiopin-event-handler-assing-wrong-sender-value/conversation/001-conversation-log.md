# dotnet/iot#2403 — Conversation Log

> **What this is:** the single, linear record of working on [dotnet/iot#2403](https://github.com/dotnet/iot/issues/2403):
> progress, Timothy's questions and Claude's answers, and decisions, **in the order they happened**. Read it top to
> bottom and you have the full context. This is the file a new Claude session (desktop or CLI) reads first, right
> after this folder's `CLAUDE.md`.
>
> **Rules:**
> - **Append only.** New entries go at the bottom. Never rewrite an earlier entry; if something in it turns out to
>   be wrong, append a 🔁 Correction entry that links back to it.
> - The only parts edited in place are **"Where we are now"** and the **Index** table.
> - Keep entries about *this issue*. When an answer turns into a general lesson, write it as a lecture in
>   [`../../iot_concepts/`](../../iot_concepts/) and link to it from here.
> - Anything not backed by a saved run in `../sample/evidence/` is labeled **unverified**.
>
> **Entry types:** 📍 Progress · ❓ Question (Timothy, in his words) → 💬 Answer · 🧭 Decision · 🔁 Correction
>
> **Started:** 2026-09-27

---

## Where we are now *(updated in place)*

| | |
|---|---|
| **Stage** | Explore: understanding the issue. No code yet, nothing posted upstream. |
| **Last entry** | #3 (2026-09-28) |
| **Open decision** | Which route to take (entry #1 §10). |
| **Next step** | Timothy works through lectures 002 and 003 (and their "Try it" programs on the Mac), then asks questions (entry #4 onward). Then pick a route. |

---

## Index

| # | Date | Type | Title |
|---|---|---|---|
| 1 | 2026-09-27 | 📍 Briefing | What #2403 is, what's going on, what the source shows, and whether it's still a good pick |
| 2 | 2026-09-27 | 📍 Progress | Wrote concepts lecture 001 (the big picture of dotnet/iot); one new docs lead |
| 3 | 2026-09-28 | ❓→💬 / 📍 | Concept questions (bindings, OOP/factory, delegates/events/threads, `IDisposable`/ownership, UnitsNet, testing, extension methods and `this`) answered as lectures 002 and 003 |

---

## #1 · 2026-09-27 · 📍 Briefing: what #2403 is about

### 1. The issue in one paragraph

When you subscribe to a GPIO pin's `ValueChanged` event, your handler gets called as
`handler(object sender, PinValueChangedEventArgs e)`. By .NET convention, `sender` is the object that raised the
event, which normally means the object you subscribed on. In dotnet/iot, `sender` is instead **whatever internal
object the hardware driver happens to use**: a private helper class, the driver itself, the controller, and in
one driver a boxed `int`. You never get your `GpioPin` back. The maintainers agree this is a bug, but they also
call the fix a **breaking change that needs a new major release**, and that shapes everything about how (and
whether) to work on it.

### 2. The facts (checked 2026-09-27)

| | |
|---|---|
| **Title** | "GpioPin event handler passing wrong sender value." |
| **Opened** | 2025-07-08 by **RoySalisbury** |
| **Labels** | `bug`, `Priority:2` (triage first set `Priority:1`, then lowered it) |
| **Assignee / milestone / PR** | none / none / none (GitHub search for PRs mentioning 2403: 0 results) |
| **Last activity** | 2025-07-10 (the triage comments). Nothing since. |
| **Where it came from** | Roy left a review comment on commit [`dd8e964`](https://github.com/dotnet/iot/commit/dd8e964069771ea2f82e9e0f51562998a8219566) (PR #2368, "Add virtual gpio controller, attempt 2", Patrick Grawehr, 2025-01-18), on `VirtualGpioController.cs` line 243, the `Invoke(this, …)` call. The issue was created from that comment. |
| **Roy's follow-up** | The same problem exists in the standard `GpioController` too, not only the virtual one. |

**The maintainer's triage (krwq, 2025-07-10)**, two comments:

1. **The rule:** "Expected sender should be whichever object you registered the event on." So
   `controller.RegisterCallbackForPinValueChangedEvent(...)` should give you the **controller**, and
   `pin.ValueChanged += ...` should give you the **pin**.
2. **The cost** (paraphrased): the fix looks correct, but it's a breaking change, so it needs a new major release
   and thorough testing across every driver implementation. The current behavior isn't blocking anyone.

### 3. The cast of characters

| Character | Real type | Where it lives (dotnet/iot) | Its job |
|---|---|---|---|
| **The App** | your code | — | Subscribes to a pin and wants to know *which* pin changed. |
| **The Front Desk** | `GpioPin` | `src/System.Device.Gpio/System/Device/Gpio/GpioPin.cs` | The object you hold for one pin. It does no work itself: every call is forwarded to the controller with its pin number attached. |
| **The Switchboard** | `GpioController` | `.../Gpio/GpioController.cs` | Owns the open pins and routes every call to the driver. |
| **The Hardware Interpreter** | a `GpioDriver` subclass: `LibGpiodV2Driver`, `LibGpiodDriver`, `SysFsDriver`, `RaspberryPi3Driver`, … | `.../Gpio/Drivers/` and some in `src/devices/` | Talks to the Linux kernel or the chip. Keeps the list of callbacks. |
| **The Runners** | internal helpers: `LibGpiodV2EventObserver`, `LibGpiodDriverEventHandler`, `UnixDriverDevicePin` | `.../Gpio/Drivers/` | Background watchers that notice an edge on the wire and **actually call your handler**. |
| **The Stand-in Switchboard** | `VirtualGpioController` + `VirtualGpioPin` | `src/devices/Board/` (in the `Iot.Device.Bindings` package) | Takes pins from one or more real controllers and exposes them under new numbers. |

### 4. How an event travels

```
 SUBSCRIBE (going down)
 ─────────────────────
 App             pin.ValueChanged += OnChanged
                        │
 Front Desk      GpioPin.ValueChanged.add ── passes OnChanged down, unchanged
                        ▼
 Switchboard     GpioController.RegisterCallbackForPinValueChangedEvent(pin#, Rising|Falling, OnChanged)
                        ▼
 Interpreter     driver.AddCallbackForPinValueChangedEvent(...) ── stores OnChanged with a Runner

 FIRE (coming back up)
 ─────────────────────
 kernel: "edge on line 17"
                        ▼
 Runner          OnChanged.Invoke(this, args)        ◄── "this" is the Runner
                        │   (the Front Desk and the Switchboard are not on this path at all)
                        ▼
 App             OnChanged(sender = the Runner, e)   ◄── expected: the GpioPin
```

**The key point:** `GpioPin` and `GpioController` hand your delegate straight down and are **never on the way back
up**. Only the object that calls `Invoke` decides what `sender` is, and that's always something inside the driver.

### 5. What `sender` actually is, driver by driver

From reading the source on `main` @ `1eb0b2f` (2026-09-24). **Unverified by running**; that's the sample's first job.

| How you subscribed | Driver | `sender` you get | Should be (triage rule) |
|---|---|---|---|
| `pin.ValueChanged +=` or `controller.Register…` | `LibGpiodV2Driver` (modern Linux, Pi 5) | `LibGpiodV2EventObserver` (internal class) | pin / controller |
| same | `LibGpiodDriver` (libgpiod v1) | `LibGpiodDriverEventHandler` (internal) | pin / controller |
| same | `SysFsDriver` | `UnixDriverDevicePin` (internal) | pin / controller |
| same | `RaspberryPi3Driver` | passes through to its internal driver, so one of the three above | pin / controller |
| same | `Mcp23xxx`, `Tca955x`, `KeyboardGpioDriver` | the driver itself | pin / controller |
| same | `ArduinoGpioControllerDriver` | a **boxed `int`** (the pin number) | pin / controller |
| `virtualController.Register…` | `VirtualGpioController` | the `VirtualGpioController` | controller ✅ |
| `virtualPin.ValueChanged +=` | `VirtualGpioPin` | forwards whatever the *underlying* driver sent | the virtual pin |

Two things stand out:

- **The line Roy flagged is the one path that's already right** under krwq's later rule. `VirtualGpioController`
  passes `this` (the controller), and you registered on the controller. Every *other* path breaks the rule.
- **The bug is older than the commit it was reported on.** Before `dd8e964`, `GpioPin` (added in #1895, Dec 2022)
  handed your delegate straight to the driver, exactly as it does now. It has behaved this way since `GpioPin`
  existed.

### 6. Why `sender` matters

The standard .NET event shape is `(object sender, TEventArgs e)`. `sender` is what lets **one handler serve many
sources**:

```csharp
// One handler for eight buttons
void OnButton(object sender, PinValueChangedEventArgs e)
{
    var pin = (GpioPin)sender;   // today: InvalidCastException, sender is a driver-internal object
    Console.WriteLine($"Button on pin {pin.PinNumber} → {e.ChangeType}");
}
```

The workaround is `e.PinNumber`, which is why triage said "not blocking". But `e.PinNumber` is only a number in
*some* controller's numbering. With two controllers (say, the Pi's own GPIO plus an MCP23017 expander), pin 3 on
each looks identical to a shared handler, and you can't get back to the `GpioPin` or its `Controller`.

### 7. Why the maintainers call the fix "breaking"

- **It changes the behavior of a public contract.** Anyone who depends on today's `sender` (Arduino users doing
  `(int)sender`, or code that checks `sender is SomeDriver`) would break **at run time, not compile time**. That's
  the worst kind of break: it compiles fine and then fails in the field.
- **Every driver is involved**, and many can only be tested on real hardware (triage: "thorough testing for all
  drivers").
- **Semantic versioning:** breaking changes go in a new major version. Current release is **v4.2.0**
  (2026-03-14). The release doc names **4.3.0** as next. There's no v5 milestone.
- **Precedent, both ways:**
  - #2341, "Planned breaking changes for v4.0", was a tracking issue listing what went into the last major. A v5
    list like that doesn't exist yet.
  - The removal of the public `PinNumberingScheme` enum (#2421, merged 2025-10-16) landed **between v4.0.0 and
    v4.1.0**, in a minor release. But it was announced in advance (marked obsolete in #2358, 2024) and was on the
    #2341 plan. So a breaking change *can* land in a minor if it's announced and agreed first. That's worth asking
    about.
- The repo's own `.github/copilot-instructions.md` (guidance for AI assistants) says not to introduce breaking
  public-API changes without maintainer direction.

### 8. What makes the fix itself tricky

- **Where to fix it.** At the top (the pin and the controller wrap your delegate so they call it with themselves as
  `sender`) or at the bottom (change every driver)? The top covers every driver in one place, so it's probably the
  right shape. But `GpioPin` reaches the driver *through* the controller's public virtual method, so who wraps what
  needs care.
- **Delegate identity.** If `GpioPin` registers a wrapper lambda instead of your handler, then `-=` has to remove
  *that same wrapper instance*, so the pin needs a map from your handler to its wrapper. It also has to keep normal
  event semantics: the same handler added twice fires twice, removed once fires once. And subscriptions can come
  from any thread.
- **Overrides.** `RegisterCallbackForPinValueChangedEvent` is `public virtual`, `GpioPin.ValueChanged` is
  `virtual`, and `VirtualGpioController` and `VirtualGpioPin` override them. A fix has to behave the same in the
  subclasses.

We'll design this properly in a later entry. This is just the shape of the problem.

### 9. Things noticed nearby (leads, **unverified**)

Reading `VirtualGpioController` and `VirtualGpioPin` for this briefing turned up code that looks wrong on its own,
separate from `sender`:

| # | Where | What the code does | Possible effect |
|---|---|---|---|
| L1 | `VirtualGpioController.RegisterCallback…` | Subscribes to the underlying pin **only if `_callbackEvents.Count == 0`** | A callback on a *second* virtual pin may never fire. |
| L2 | same | `_callbackEvents.TryAdd(pinNumber, …)` | A second handler on the same pin is **silently dropped**. |
| L3 | `VirtualGpioController.UnregisterCallback…` | Removes by pin number, ignores which callback you passed | Removing one handler removes them all. |
| L4 | `VirtualGpioPin.ValueChanged.add` | Registers `OldValueChanged` with the real controller **on every `+=`** | With two handlers, each might fire twice. |

The existing tests (`src/devices/Gpio/tests/VirtualGpioTests.cs`, `Callback1`–`Callback3`) each use one pin and one
handler, which would explain how these went unnoticed. If they reproduce, they're **non-breaking** bug fixes, which
could be mergeable now.

### 10. Is this still a good pick? (correction to scouting item G)

The scouting entry (G in `scouting/001-issue_shortlist_sept_2026.md`) rated this "🟢 open, available · 🛠️
contribute, ~4–8 h", with a step to *ask* whether it's a breaking change. The thread already answers that: **yes,
and it wants a major release.** So a PR for the full fix could sit unmerged for a long time. The options:

| Route | What it is | Could merge soon? | What you learn |
|---|---|---|---|
| **A. Ask first** | Comment on #2403: offer to implement; ask whether they'd take it now as a pre-announced change (the `PinNumberingScheme` path) or want it parked for v5. Attach the driver table (§5) as evidence. | Depends on the reply | Events, upstream process |
| **B. Build the fix anyway** | Fork, failing tests, wrapper fix, open as a draft PR marked for the next major | Probably not for months | The most engineering |
| **C. Non-breaking slice** | Document today's behavior in the XML docs of `ValueChanged` and `RegisterCallback…` ("`sender` is driver-specific; use `e.PinNumber`") | Likely | Smaller |
| **D. The virtual-controller leads (§9)** | Reproduce L1–L4 with unit tests; fix whichever are real | Likely | High, and it's the same code area |

**Claude's recommendation:** capture evidence first (§11), then do **A** with that evidence attached, and **D** in
parallel if the leads reproduce. This is Timothy's call. 🧭 **Decision pending.**

### 11. What the sample could do first (no hardware needed)

| Exp | What | Proves |
|---|---|---|
| **E1** | Console app on NuGet `System.Device.Gpio` 4.2.0 + `Iot.Device.Bindings` 4.2.0, with a tiny fake `GpioDriver` (the same trick as the repo's `MockableGpioDriver`). Subscribe four ways (pin, controller, virtual controller, virtual pin); print `sender?.GetType().FullName` and `ReferenceEquals(sender, pin)`. | The pass-through in §4: the pin and controller never substitute themselves as `sender`. |
| **E2** | Same app: two virtual pins, two handlers per pin, add and remove. | Whether L1–L4 are real. |
| **E3** *(optional, needs a Pi)* | `LibGpiodV2Driver` on real hardware, jumper from an output pin to an input pin, toggle, print `sender`. | §5's top row on a real driver. |

E1 and E2 run on macOS. Everything goes to `../sample/`, raw output to `../sample/evidence/NNN-*.txt`.

### 12. Next steps / questions for Timothy

1. Read this entry and ask whatever's unclear, in your words. Each question becomes the next entry.
2. Pick the first concept lecture for `iot_concepts/`. Candidates this issue touches:
   - **.NET events and delegates:** `event` accessors, multicast delegates, delegate identity, why `-=` needs the same instance.
   - **dotnet/iot architecture:** controller / driver / pin / bindings, and why the driver owns the callbacks.
   - **GPIO on Linux:** sysfs vs libgpiod v1 vs v2, edge events, how the kernel reports a pin change.
   - **Breaking changes in libraries:** binary vs source vs behavioral breaks, semver, how .NET projects manage them.
3. Do you have a Raspberry Pi available for E3?
4. Decide the route (§10).

### Sources (checked 2026-09-27)

- Issue: https://github.com/dotnet/iot/issues/2403 (body, 3 comments, timeline)
- Origin comment: commit `dd8e964`, review comment r161692505 on `src/devices/Gpio/Drivers/VirtualGpioController.cs` (position 243)
- Source read at `main` @ `1eb0b2f` (2026-09-24): `GpioPin.cs`, `GpioController.cs`, `GpioDriver.cs`,
  `PinChangeEventHandler.cs`, `Drivers/LibGpiodV2EventObserver.cs`, `Drivers/LibGpiodDriverEventHandler.cs`,
  `Drivers/UnixDriverDevicePin.cs`, `devices/Board/VirtualGpioController.cs`, `devices/Board/VirtualGpioPin.cs`,
  `devices/Arduino/ArduinoGpioControllerDriver.cs`, `devices/Board/KeyboardGpioDriver.cs`, tests in
  `System.Device.Gpio.Tests/` and `devices/Gpio/tests/VirtualGpioTests.cs`
- Release history: tags v4.0.0 (2025-05-09), v4.1.0 (2026-01-16), v4.2.0 (2026-03-14); `Documentation/creating-new-release.md`
- Precedent: #2341 (planned breaking changes for v4.0), #2358 (obsolete `PinNumberingScheme`), #2421 (removal)
- Contribution guidance: `Documentation/CONTRIBUTING.md`, `.github/PULL_REQUEST_TEMPLATE.md`, `.github/copilot-instructions.md`

---

## #2 · 2026-09-27 · 📍 Progress: big-picture lecture written

Before going deeper into #2403, Timothy asked for a holistic lecture on dotnet/iot: what the repo is, how to use it,
where it runs, and the concepts he'll need.

- **Written:** [`../../iot_concepts/001-the-big-picture-dotnet-iot.md`](../../iot_concepts/001-the-big-picture-dotnet-iot.md).
  It covers the purpose and history, a C/Linux → dotnet/iot mapping table, the layered architecture, the cast of
  characters, the two NuGet packages and the repo layout, GPIO in depth (including the callback path #2403 is
  about), how `new GpioController()` picks a driver, Linux GPIO interfaces (sysfs / libgpiod v1 / v2 /
  `/dev/gpiomem`), I2C/SPI/PWM, bindings and their conventions, a platform matrix, four ways to run code (fake
  driver on the Mac, Raspberry Pi, FT232H on the Mac, Arduino/Firmata), contributor workflow, and a ranked list of
  concepts to learn.
- **Recommended reading order:** lecture 001 first, then entry #1 again. §6.3 and §14 of the lecture lead
  straight into this issue.
- **New lead (unverified, docs-only):** `Documentation/gpio-linux-libgpiod.md` shows `new GpioController(chipNumber)`,
  but the current source only has `GpioController()` and `GpioController(GpioDriver)`. The same snippet also
  declares `gpioController` and then uses `controller`. That's a small, non-breaking docs fix and a possible easy
  first dotnet/iot PR alongside route C/D (entry #1 §10).
- **New cheap experiment idea (E0):** run `new GpioController()` on the Mac. The lecture (§7) predicts
  `PlatformNotSupportedException("No unix driver appears to be runnable")`, because .NET reports macOS as
  `PlatformID.Unix`. It's a two-minute walking skeleton that confirms the driver-selection path before E1.
- Nothing posted upstream. Route decision still pending.

## #3 · 2026-09-28 · ❓ → 💬 Concept questions, answered as two lectures

### ❓ The question (summarized from Timothy's message)

After lecture 001: what does *binding* actually mean? Abstraction, strategy and factory selection (seen before in
other projects but never understood). Delegates, multicast delegates, event accessors, and how threads interact with
callbacks, since his habit is a polling `while` loop and his hand-written `reactive/` attempt at events didn't work.
`IDisposable` and all the other interfaces on a class: how should he read them? Ownership, with examples. What
"every sensor binding returns UnitsNet types" means. How testing, fakes, mocks and test traits work. Extension methods
and the `this` keyword, which made the sentence *"sender is the object that raised the event, which normally means
the object you subscribed on"* unreadable. And: is it a bad idea to learn these from code (#2403) that is broken in
exactly these concepts?

### 💬 Short answer

Two lectures, each teaching from dotnet/iot code that does things **right**:

- [`../../iot_concepts/002-delegates-events-callbacks-and-threads.md`](../../iot_concepts/002-delegates-events-callbacks-and-threads.md):
  delegates (as function pointer + captured target), multicast, delegate identity, events and custom accessors, the
  `(sender, e)` convention (with the quoted sentence explained step by step in §6.2), the four meanings of `this`,
  extension methods (`I2cBusExtensions.PerformBusScan`, `builder.Services.AddSingleton`), which thread runs your
  handler, a full trace of one button press from kernel to handler, the `reactive/` attempt revisited, and **#2403 as
  a spot-the-difference exercise at the end** (§12).
- [`../../iot_concepts/003-bindings-oop-architecture-disposal-and-testing.md`](../../iot_concepts/003-bindings-oop-architecture-disposal-and-testing.md):
  what a binding is (BME280 traced from I2C bytes to `Temperature`), OOP building blocks, reading a class
  declaration, Strategy / Template Method / Factory / Adapter, `IDisposable` and the `Dispose(bool)` pattern,
  ownership (`shouldDispose`), UnitsNet, and testing (xUnit, fakes vs. mocks, Moq, `MockableGpioDriver`, traits),
  ending with a **sketch of the failing unit test for #2403** (§8.8).

On the "broken code" question: the concepts are taught from working code (`ButtonBase`/`GpioButton` follow the
event convention correctly), and #2403 comes last, once the correct shape is familiar. Seen that way, the broken
code shows exactly which rule it breaks.

### Takeaways / next steps

1. Work through 002, then 003. Run the "Try it" programs on the Mac (002 §15, 003 §10). They're the fastest way to
   make these concepts concrete, and any output that differs from the prediction is worth an entry here.
2. The #2403 test sketch in 003 §8.8 is effectively experiment **E1** as a unit test. It's a natural first thing to
   build in `../sample/`.
3. New leads noticed while writing (all **unverified**, from reading only):
   - An exception thrown by a user's handler under `LibGpiodV2Driver` appears to end that request's observer loop
     and remove its subscriptions, so the pin silently stops reporting (002 §9.3).
   - `LibGpiodV2EventObserver` calls user handlers while holding its lock (002 §9.5).
   - `ButtonBase._holdingTimer` is touched from two threads without a lock (002 §9.4).
4. Route decision for #2403 still pending (entry #1 §10).

