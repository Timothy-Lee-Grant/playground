# 001: Should I add a new sensor binding to dotnet/iot?

> Date: 2026-10-04 · Upstream checked: dotnet/iot `main` @ `95384e7` (2026-10-01) · Type: investigation
> Raw findings: [`evidence/001-upstream-new-binding-survey-2026-10-04.txt`](evidence/001-upstream-new-binding-survey-2026-10-04.txt)
> Next: [`002-choosing-the-device.md`](002-choosing-the-device.md) → [`003-what-youd-build-and-the-steps.md`](003-what-youd-build-and-the-steps.md)

---

## 0. The short answers

| Your question | Short answer |
|---|---|
| **Would they be happy about it?** | **Yes, with conditions.** "Add bindings for more devices" is the first item on the project's roadmap, and the bindings README says *"Anyone can contribute a binding. Please do!"* Every new-binding PR from #2300 to now has landed on `main`, except one opened 2026-10-03 (#2616, not yet reviewed). The conditions: follow the device conventions, pick a device people actually use, and include tests that run without hardware. |
| **Is "no issue, just a PR" OK?** | **It's normal here, but announce it first.** Two of the recent community bindings filed a short issue describing the device and then opened the PR (Blinkt: #2369 → #2370; AIP31068: #2433 → #2434). Do the same: a short "I'd like to add X, here's the scope" issue. It costs one paragraph and avoids colliding with someone else. |
| **Is it a good direction for me?** | **Yes, as one anchor project, not as your whole track.** It uses your strongest skills, and it also makes you do things a bug fix doesn't: design a public API, build a test harness, write the docs, and own the code afterwards. Keep YARP-style backend work running next to it. |
| **How much would I learn?** | **Moderate on hardware (you know it), high on software design.** The new parts are public API design under conventions, testing hardware code without hardware, numerics, and shipping a component in a package other people use (§3). |
| **Would it impress a hiring manager?** | **It's a good story if you tell it as software engineering.** It shows end-to-end ownership of a shipped component in a Microsoft-maintained .NET library. It won't count as backend experience, so it complements your backend work rather than replacing it (§4). |
| **What device?** | **BMP390** (barometric pressure + temperature). Runner-up: INA226 support in the INA family, which is closest to your professional data path. See [002](002-choosing-the-device.md). |

---

## 1. What a "binding" is, in one paragraph

**A binding is a C# class that turns one chip's registers into a clean .NET API.** It sits on top of the
transport classes (`I2cDevice`, `SpiDevice`, `GpioController`) and below the user's app. A user writes
`using var sensor = new Bmp390(i2cDevice); sensor.TryReadPressure(out Pressure p);` and never sees a register
address. The binding ships inside the `Iot.Device.Bindings` NuGet package (672.5K total downloads as of
2026-10-04), so the code you write ends up in other people's projects. Lecture
[`006`](../iot_concepts/006-how-dotnet-iot-fits-together.md) traces exactly this path for `Bme280`.

```
  user's app ──► Bmp390 (your binding: registers → Pressure, Temperature) ──► I2cDevice ──► Linux /dev/i2c-1 ──► chip
                 └─ you design this public API; it's what reviewers care most about
```

---

## 2. Would the maintainers welcome it? The evidence

### 2.1 What the project says

| Source (on `main`) | What it says |
|---|---|
| `Documentation/roadmap.md` | First "vNext" deliverable: **"Add bindings for more devices"** |
| `src/devices/README.md`, "Contributing a binding" | **"Anyone can contribute a binding. Please do!"** Then the must-haves: project file, README, buildable sample, use the `System.Device` APIs. Unit tests are optional but must **not** need hardware |
| `Documentation/Devices-conventions.md` | The rules reviewers check: `TryRead*` methods, UnitsNet types, methods for values that change and properties for values that don't, `enum Register : byte`, who disposes what |
| `.github/PULL_REQUEST_TEMPLATE.md` | "If your PR is adding device binding please make sure to read conventions for devices APIs" |
| Branch `copilot/create-ai-skills-based-on-copilot` (krwq, open PR #2473) | `.github/skills/add-device-binding/SKILL.md`: a maintainer-written checklist for new bindings. Not merged, but it shows what krwq expects |

### 2.2 What the project has actually done

Every PR numbered #2300 or higher that adds a new binding project (method in the evidence file):

| PR | Binding | Who | Roughly how long | Outcome | What reviewers asked for |
|---|---|---|---|---|---|
| #2309 | LPS22HB (pressure) | community | ~5 weeks | ✅ merged 2024-06-06 | a timeout on a polling loop; `TryRead*` instead of properties; README listed features the API didn't have |
| #2370 | Pimoroni Blinkt | community (filed issue #2369 first) | ~2 weeks | ✅ merged 2025-01-13 | make a timing constant a settable property; remove an extra README; fix the linter |
| #2374 | TCA955x | community | ~2 months | ✅ landed on `main` (2024-11 → 2025-01) | use the new constants instead of magic numbers like `0x20` |
| #2434 | AIP31068 LCD | community (filed issue #2433 first) | ~1 week | ✅ merged 2025-10-30 | **cite datasheet pages for every delay and init step** |
| #2459 | INA236 | pgrawehr (maintainer) | ~4 months | ✅ merged 2026-03-08 | split unrelated changes out; add a plain Raspberry Pi sample |
| #2616 | CAP1208 | community, AI-assisted | opened 2026-10-03 | ⏳ no review yet | (watch this one: it's the closest comparison to what you'd send) |

**The pattern:** maintainers merge new bindings, and their review comments are about **conventions and
evidence** (the `TryRead` pattern, timeouts, datasheet references, accurate READMEs), not about whether the
device belongs in the repo. Review time ranges from a week to months.

### 2.3 The honest caveats

| Risk | Why it matters | What to do |
|---|---|---|
| **Slow reviews** | A few volunteers review everything. Some PRs have been open since 2024 (#2324, #2359) | Keep the PR small and clean so it's an easy "yes"; one polite ping after ~2 weeks |
| **Maintenance burden** | Every binding is code they'll carry forever. An obscure chip is a cost with no benefit to them | Pick a device people actually buy (§002); say in the issue that you'll maintain it |
| **AI-assisted PR volume** | On 2026-10-03 one contributor opened 3 PRs in a day, including the CAP1208 binding, with an AI co-author trailer | What makes yours different: **real-hardware evidence**, a pre-announced scope, and answering review comments yourself. Disclose AI help briefly, as you did on #2611 |
| **Hand-editing generated files** | TM1650 (#1855) broke the build by editing `Device-Index.md` | Add a `category.txt`; don't touch `Device-Index.md` or the category list in `src/devices/README.md`. Maintainers regenerate them with `tools/device-listing` |
| **Someone else is already on it** | I couldn't search upstream issues from this session (PRs were checked: none add a BMP3xx) | Search GitHub issues for "BMP390" and "BMP388" before posting |

---

## 3. How much would you learn?

Mapped onto your exposure map (`open_source_persona.md` §7.2). "New" means beyond what you do in your day job.

| Area | What this project makes you do | How new | Depth you'd reach |
|---|---|---|---|
| **.NET API design and review** | Design a public API that has to stay stable once it ships: names, types, `Try*` vs exceptions, enums vs raw bytes, what's public vs `protected`, consistency with sibling bindings | 🆕 **High.** It's on your "actively deepening" list and a bug fix never asks for it | Contributed |
| **Testing infrastructure** | Build a **simulated chip** on `I2cSimulatedDeviceBase` so the tests run in CI with no hardware; use the datasheet's and Bosch's numbers as test vectors | 🆕 **High.** It's an empty row on your map | Contributed |
| **Numerics and bit manipulation in C#** | Signed/unsigned mixed-width fields, little-endian 24-bit values, power-of-two scaling, `double` vs `long` compensation, `BinaryPrimitives`, `Span<byte>` | 🟡 Medium. You know the bits; the C# idioms are new | Contributed |
| **Build, packaging and CI** | How one folder becomes part of `Iot.Device.Bindings` (a wildcard `ProjectReference`), the device-listing tool, Arcade CI | 🟡 Medium | Read → Contributed |
| **Error handling and API compatibility** | What happens when the chip ID is wrong, the data isn't ready, or a read times out; why a shipped API is hard to change (semver) | 🟡 Medium. Continues #2403 / #2328 | Contributed |
| **Hardware protocols and drivers** | Register map, I2C transactions, power modes, oversampling | 🟢 **Low.** This is your strength, and why the project is feasible | Can explain |
| **Concurrency and events** (optional v2) | A data-ready interrupt pin → a GPIO event → a .NET event | 🟡 Medium. Builds directly on #2328 | Later |
| **Open-source process** | Proposing work nobody asked for; carrying a larger PR through several review rounds; owning code after the merge | 🆕 High | Contributed |

**What you would *not* learn:** networking, cloud, distributed systems, databases. That's the main reason to keep
YARP (or another backend repo) running alongside this.

**Compared with a bug fix like #2328:** a bug fix teaches you to read someone else's design. A binding makes you
*be* the designer, then defend the design in review. That's the step from "can fix code" to "can own a component".

---

## 4. Would it help with the Microsoft goal?

### 4.1 What a hiring manager can see

| They see | What it shows them |
|---|---|
| A component you designed, merged into a Microsoft-maintained .NET library and shipped in its NuGet package | You can carry work from an idea to production, through review, in someone else's codebase |
| The review thread with the maintainers | How you take feedback and explain your decisions in writing |
| Unit tests that run in CI against a simulated chip | You know how to make code testable, which matters in every software role |
| A clean public API that matches its sibling bindings | You think about the people who'll use your code |

### 4.2 Where it's strong and where it's weak

- **Strong for:** any Microsoft role at the hardware/software boundary. Example (one posting seen, not a
  survey): a *Software Engineering IC2* role in Microsoft Devices Operations (Surface/Xbox), posted 2026-08-22,
  building "full-stack features for our services on C#/.Net and Python". .NET + devices + services is exactly
  this profile.
- **Neutral to good for:** general software engineering roles, **if you tell it as a software story**
  (API design, testability, abstraction, ownership) and not as "I wrote a sensor driver".
- **Weak for:** backend/cloud roles on its own. It doesn't show HTTP, services at scale, or data stores. YARP
  and your backend projects cover that side.

### 4.3 The interview story it gives you

> *"I proposed and contributed a new device binding to dotnet/iot. The interesting parts were the API design,
> keeping it consistent with the existing Bosch bindings so users could swap sensors, and making it testable
> without hardware: I built a simulated chip and used the vendor's reference values as test vectors. Then I
> worked it through review with the maintainers."*

That covers design, testing, collaboration and ownership: the four things behavioral and design interviews ask about.

### 4.4 Recommendation

Do it, **as your next ⚓ anchor**, under these rules:

1. **Start after #2611 settles** (one active PR at a time, `open_source_persona.md` §6.7). The hardware
   walking skeleton (no upstream activity) can start now.
2. **Pair it with one 🧭 explorer outside the device domain** (YARP #275 is already in progress), per the
   system's half-anchor, half-explorer mix.
3. **Keep v1 small:** read temperature, pressure and altitude, with configuration. FIFO and interrupts come
   later, as follow-up PRs. Small first PRs get merged; big ones stall.

---

## 5. Things I couldn't verify (yet)

| Claim | Status |
|---|---|
| Maintainers would welcome a BMP390 specifically | **Unverified.** Only general welcome is evidenced. The pre-announce issue answers this |
| Nobody has an open *issue* asking for a BMP3xx binding | **Unverified.** PRs checked; issues not searchable from this session |
| Review times above | Approximated from commit dates on the PR branches, not PR timestamps |
| Effort estimates in [003](003-what-youd-build-and-the-steps.md) | Estimates, from the size of Ina236 (~450 lines with tests) and Bmxx80 |

## Sources

- dotnet/iot `main` @ `95384e7`: [roadmap.md](https://github.com/dotnet/iot/blob/main/Documentation/roadmap.md) ·
  [src/devices/README.md](https://github.com/dotnet/iot/blob/main/src/devices/README.md) ·
  [Devices-conventions.md](https://github.com/dotnet/iot/blob/main/Documentation/Devices-conventions.md)
- PRs: [#2309](https://github.com/dotnet/iot/pull/2309) · [#2370](https://github.com/dotnet/iot/pull/2370) ·
  [#2434](https://github.com/dotnet/iot/pull/2434) · [#2459](https://github.com/dotnet/iot/pull/2459) ·
  [#1855](https://github.com/dotnet/iot/pull/1855) · [#2616](https://github.com/dotnet/iot/pull/2616) ·
  [open PR list](https://github.com/dotnet/iot/pulls) (fetched 2026-10-04)
- [Iot.Device.Bindings on nuget.org](https://www.nuget.org/packages/Iot.Device.Bindings)
- [Microsoft "Software Engineering IC2" posting](https://jobs.anitab.org/companies/microsoft-2/jobs/90884762-software-engineering-ic2) (job board, fetched 2026-10-04)
