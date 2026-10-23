# Scouting 003 — Shortlist for the Microsoft goal (October 2026)

> **Date researched:** Thu 2026-10-22 (all upstream states checked that day) · **Previous passes:**
> [`001`](001-issue_shortlist_sept_2026.md) (broad, Sept) · [`002`](002-code_pr_shortlist_week_of_2026-09-29.md) (first code PR)
> **Question for this pass:** with 2 merged dotnet/iot PRs and a YARP PR in review, which repos and issues move you
> furthest toward **a backend / distributed-systems or AI engineering role at Microsoft**?
> **Labels:** 🟢 open, available · 🟡 open, claimed · ⚫ closed · 🛠️ contribute · 📖 learn · ⚓ anchor · 🧭 explorer

---

## 0. The answer in one picture

```
                       WHAT A MICROSOFT HIRING LOOP CAN SEE ON YOUR GITHUB
                                         │
        ┌────────────────────────────────┼─────────────────────────────────┐
        ▼                                ▼                                 ▼
  LANE 1 ⚓ "THE BENCH"           LANE 2 🧭 "THE BACKEND"           LANE 3 🧭 "THE AI TRACK"
  dotnet/runtime                  microsoft/aspire                 MCP C# SDK · Agent Framework
  (System.IO.Ports) + dotnet/iot  (Microsoft's distributed-app     (watch + pounce; crowded)
                                   stack: Azure, containers, OTel)
  You can verify what almost      Fills the empty rows of your     Keeps the AI story warm
  nobody else can: real ports,    exposure map: cloud, deployment,  without competing in the
  real wires, a logic analyzer.   containers, observability.        busiest queues on GitHub.
  Career signal: highest .NET     Career signal: a flagship         Career signal: high, but
  repo there is.                  product built by the .NET team.   only if you land something.
```

**Recommendation:** move your center of gravity from dotnet/iot to **dotnet/runtime's `System.IO.Ports`** (lane 1) and
**Aspire** (lane 2), and keep dotnet/iot as the place you already have trust. Details and reasoning in §1; the picks in §2.

---

## 1. What changed, and why the plan changes with it

### 1.1 Three findings from today's search

| Finding | Evidence (2026-10-22) | What it means for you |
|---|---|---|
| **"help wanted" queues in hot repos are claimed within days**, often by contributors posting agent-style plans | MCP C# SDK: #788, #1239, #1185, #1862 all have a claimant and an open PR. Aspire #11121 (good first issue) has a PR. aspnetcore #2694 claimed in August. iot #1663 (from list 002) claimed 2026-10-10. | Racing for labeled issues is a losing game. Your edge has to be work others **can't** easily do. |
| **dotnet/runtime's `System.IO.Ports` has 14 open `help wanted` issues with no PR and no assignee**, several Linux-only, several reported on a Raspberry Pi | e.g. #97741, #64507, #96267, #121627; area owner (jeffhandley, 2025-02-09): "I don't expect our team will be able to investigate this soon … in case a community member is interested" | The team has explicitly asked for exactly your skill set (termios, native C shim, P/Invoke, hardware repro), inside the most prestigious .NET repo. |
| **Aspire is where the .NET team's backend energy is** (6.4k★, davidfowl and JamesNK active daily) and it has small, unclaimed bugs if you look past the labels | #20434 and #16273 below: real bugs, no claimant, no PR | One repo covers cloud (Azure Bicep), deployment, containers, telemetry and a Blazor UI: five empty rows of your exposure map (§7.2). |

### 1.2 How this maps to the goal

- **Interview stories beat PR counts.** The strongest story is one where you found a root cause nobody else could see.
  "I put a logic analyzer on the DTR line, found why .NET resets an Arduino on `Close()`, and fixed the native shim in
  dotnet/runtime" is that story.
- **Breadth for a backend role.** Your record is all device-side so far. Aspire is the cheapest way to add
  "distributed apps, Azure, deployment" to it with real merged code.
- **Being known by a team.** A few PRs in one Microsoft-owned repo, reviewed by the same people, is worth more than one
  PR in each of five repos. Pick one home in each lane and stay.

### 1.3 Machines (re-checked 2026-10-22)

| Repo | SDK pin | Where to build |
|---|---|---|
| dotnet/runtime | `11.0.100-rc.1` | **Codespaces** (repo ships a `.devcontainer/libraries` config made for this) or the Mac + external SSD. Not the desktop (needs x86-64-v2). Hardware tests on the Pi with the shipped NuGet package (see #97741). |
| microsoft/aspire | `11.0.100-rc.2` | Mac + SSD or Codespaces. Bicep tests are snapshot tests; **no Azure account needed** (unverified until you run one). |
| dotnet/iot | unchanged | as before |

---

## 2. The shortlist at a glance

| # | Item | State · Intent | Lane · Kind | Effort | Why |
|---|---|---|---|---|---|
| **1** | [runtime #97741](https://github.com/dotnet/runtime/issues/97741): `SerialPort.Close()` resets an Arduino on Linux even with HUPCL disabled | 🟢 · 🛠️ (investigate → comment → PR) | 1 · ⚓ bug, native C | 15–25 h | Hardware root-cause in dotnet/runtime; reported on a Pi 4; team asked for community help |
| **2** | [runtime #64507](https://github.com/dotnet/runtime/issues/64507): 250000 baud on Linux | 🟢 · 🛠️ (verify + comment) | 1 · ⚓ verification | 2–4 h | A maintainer thinks it's fixed by #80534 and asked to confirm. A hardware check closes it. Cheap first post in runtime. |
| **3** | [Aspire #20434](https://github.com/microsoft/aspire/issues/20434): a Choice input can't select `editor` when `editorswiss` exists | 🟢 · 🛠️ | 2 · 🧭 bug + test | 6–10 h | Small, clear repro, triaged as a valid bug, no claimant. Your first Aspire PR. |
| **4** | [Aspire #16273](https://github.com/microsoft/aspire/issues/16273): Bicep duplicate identifier when a blob container is named `blobs`; CLI hides Bicep errors | 🟢 · 🛠️ (ask first) | 2 · 🧭 bug, Azure IaC | 10–15 h | Azure infrastructure-as-code in Aspire. Still present on `main`. Fills "Cloud" and "Containers & deployment". |
| **5** | [iot #2615](https://github.com/dotnet/iot/issues/2615): `QueryComponentInformation()` throws with `RaspberryPi3Driver` | 🟢 · 🛠️ | 1 · ⚓ quick bug | 3–5 h | Still broken on `main`; testable on your Pi 4; keeps your iot streak going on a weeknight |
| **6** | [iot #1519](https://github.com/dotnet/iot/issues/1519): I2C "is device ready" / ACK polling API | 🟢 · 🛠️ (design comment first) | 1 · ⚓→🧭 API design | 20–40 h | Pure I2C home turf, and a route into .NET **API review** (an empty row and an interview topic) |
| 7 | [runtime #96267](https://github.com/dotnet/runtime/issues/96267) / [#20370](https://github.com/dotnet/runtime/issues/20370): `SerialPort.Write` doesn't block or time out | 🟢 · 📖 now, 🛠️ later | 1 · 🧭 design | — | Needs an owner's decision; read after #1 |
| 8 | [Aspire #20957](https://github.com/microsoft/aspire/issues/20957): telemetry API returns 500 while dashboard reports Healthy (davidfowl) | 🟢 · 📖 / 🛠️ only if invited | 2 · 🧭 health checks | — | How a real product decides "healthy". Great reading; design is the team's call |

**Suggested order:** #2 and #5 this week (small, visible, low risk) → start #1's investigation on the bench and #3 in
parallel → #4 after a reply → #6 as the long-running API project.

---

## 3. The picks in detail

### #1 · runtime #97741 — `SerialPort.Close()` triggers the hang-up the user disabled ⚓ ⭐

**State:** 🟢 open since 2024-01-31, `help wanted`, `area-System.IO.Ports`, 2 comments, no PR, no assignee.

**The bug.** A user disables HUPCL (`stty -F /dev/ttyUSB0 -hupcl`) so closing the port won't reset an Arduino.
Python respects it; .NET still resets the board on `Close()`. Windows is fine. Reported on a Raspberry Pi 4.

**What the code shows (read, not run):**

| Where | What it does |
|---|---|
| `src/native/libs/System.IO.Ports.Native/pal_termios.c`, `SystemIoPortsNative_TermiosReset` | Reads the current termios, calls `cfmakeraw`, then sets `CLOCAL | CREAD` and the frame bits. **Never mentions `HUPCL`**, so it isn't obviously the culprit. |
| `SerialStream.Unix.cs` open path (~line 628) | Sets `DtrEnable = dtrEnable` on every open. The default is `false`, so .NET **drives DTR** itself. |

So the root cause is **not yet known**, which is the point: the open path toggles DTR, the close path may too, and an
Arduino resets on a DTR edge. Finding out which edge, when, means watching the DTR line. That's a logic-analyzer job.

**Plan:** reproduce on the Pi 4 with the shipped .NET 10 `System.IO.Ports` package (no runtime build needed) → capture
DTR on open and close with and without `-hupcl` → compare with Python's `pyserial` → read the native shim and
`Dispose` path → post the root cause with traces → fix in a fork built in Codespaces.

**Concepts you'll meet:** POSIX termios and modem-control lines (DTR/RTS, `TIOCMSET`), how dotnet/runtime splits a
library into managed code + a native "PAL" shim, building one library in a huge repo, behavior differences across OSes.
**Exposure map:** Native interop (empty) · Build, packaging & CI (empty) · Hardware protocols.
**Hardware:** Pi 4, a USB-serial Arduino (or any board with an auto-reset circuit), logic analyzer.
**Upstream rules:** CONTRIBUTING.md has no AI-disclosure rule (checked 2026-10-22). Record runtime's rules in a new
`runtime/CLAUDE.md` before the first post.

### #2 · runtime #64507 — does 250000 baud work on Linux now? ⚓

**State:** 🟢 open, `help wanted`. In 2024-01 wfurt (Microsoft) wrote that it "should be fixed by #80534 … I think
this issue could be closed", with no reply since. `pal_termios.c` on `main` now has a `termios2` path
(`SystemIoPortsNative_Termios2SetSpeed`), which supports non-standard rates.

**The contribution:** open a port at 250000 on the Pi with .NET 10, send to a board running at 250000, capture the bit
time on the logic analyzer (4 µs per bit), and post the result with versions. A short, factual comment that lets a
maintainer close a 4-year-old issue. Your first name on a dotnet/runtime thread, for an evening's work.

### #3 · Aspire #20434 — Choice input picks the wrong option 🧭

**State:** 🟢 open, filed 2026-09-24, triaged by joperezr as a valid S3 bug (backlog), no claimant, no PR.

**The bug.** An `InteractionInput` of type `Choice` with keys `editor` and `editorswiss`: picking `editor` returns
`editorswiss`. The value is probably matched by "contains" or prefix rather than equality somewhere between the
dashboard's combobox and the value sent back to the AppHost. Find that comparison, make it exact, add a test.

**Why it's a good first Aspire PR:** small, and it walks you through the whole interaction round trip: AppHost →
dashboard (Blazor + Fluent UI) → back to the AppHost over gRPC. That's the core architecture of Aspire.
**Concepts:** Aspire app model, the dashboard ↔ AppHost resource service, Blazor components, the repo's Copilot-review
expectation (CONTRIBUTING: address the review comments before merge).
**Before starting:** re-check that nobody has commented. Aspire has no claim process you need to wait on; a short
"I'm taking this, plan: …" comment then a PR fits its norms.

### #4 · Aspire #16273 — Bicep identifier collision + swallowed errors 🧭

**State:** 🟢 open since 2026-04-17 (filed by maddymontaquila, labeled `aspirifridays`), no claimant, no PR.

**The bug, confirmed on `main`:** `AzureStorageExtensions.cs` line ~100 creates `new BlobService("blobs")` with a
comment saying the name is kept "for backward compatibility". Name your container resource `blobs` and the generated
Bicep declares `blobs` twice (`BCP028`). Separately, the CLI prints only "az bicep build returned exit code 1" and
hides the real error.

**Why ask first:** the obvious fix (rename the service) is a breaking change, which that comment forbids. The options
are renaming the *container's* identifier on collision or failing early with a clear message, and that's the team's
call. The second half (surface the Bicep compiler output) is probably a clean, separate PR.
**Concepts:** infrastructure-as-code, Azure.Provisioning (C# → Bicep), snapshot testing with Verify, compatibility of
generated artifacts. **Exposure map:** Cloud & distributed systems (empty), Containers & deployment (empty).

### #5 · iot #2615 — `QueryComponentInformation()` throws on the default Pi driver ⚓

**State:** 🟢 open, untriaged, 0 comments, no PR in dotnet/iot (the reporter fixed it only in his own repo).
Confirmed on `main` today: `RaspberryPi3Driver.QueryComponentInformation()` (line 281) calls
`_internalDriver.GetChipInfo()`, and `RaspberryPi3LinuxDriver` doesn't override `GetChipInfo`, so the base class throws
`NotSupportedException`. Fix: implement `GetChipInfo` for the Pi driver, or don't call it there; ask pgrawehr which he
prefers in the comment, add a test, verify on the Pi 4.

### #6 · iot #1519 — "is the device ready?" for I2C ⚓→🧭

**State:** 🟢 open, `up-for-grabs`. An API (`I2cBus.IsDeviceReady(address)`, `I2cDevice.IsDeviceReady()`) was
**approved in 2021**, a PR (#1525) failed because Linux won't send an address-only write through the normal path, and
the approval was withdrawn in 2022.

**What you'd bring:** the missing piece is the Linux mechanism: an SMBus *quick write* (`I2C_SMBUS_QUICK`), which is how
`i2cdetect` probes. Prove on the Pi + a 24LC256 EEPROM (or your BMP390) with the analyzer that a quick write gives a clean
ACK/NACK, then post a design comment: mechanism per platform, behavior on drivers that can't do it, test plan.
**Why it matters for Microsoft:** you'd go through .NET's API design process (proposal → review → approval), which is
how every public .NET API is made, and a great interview topic. Long-running; treat it as the "big project" after #2627.

---

## 4. Lane 3 (AI): watch, learn, pounce

The MCP C# SDK and Agent Framework are the right AI repos for Microsoft, but today every labeled MCP issue worth doing
has a claimant, and Agent Framework's tracker is flooded with machine-written reports. So for now:

| Item | State · Intent | Why |
|---|---|---|
| [MCP #1239](https://github.com/modelcontextprotocol/csharp-sdk/issues/1239) + [PR #1713](https://github.com/modelcontextprotocol/csharp-sdk/pull/1713): notifications stop after ~3 min over Streamable HTTP | 🟡 · 📖 | Long-lived HTTP connections, SSE reconnection, one-shot guards. Pairs with what you learned in YARP #1764 |
| [MCP #1185](https://github.com/modelcontextprotocol/csharp-sdk/issues/1185): remove `AsyncLocal` for task status | 🟡 · 📖 | `AsyncLocal` and execution-context flow: a concurrency topic interviewers like. Read halter73's review reasoning |
| [Agent Framework #9252](https://github.com/microsoft/agent-framework/issues/9252): span tag `message.type` collides with an OpenTelemetry convention | 🟢 · 📖 (🛠️ only if a maintainer agrees to rename) | Small, but renaming a telemetry tag is a compatibility decision. A neat lesson in semantic conventions |
| Self-sourced MCP idea | 💡 | Your differentiator: an **MCP server for dotnet/iot devices** (read your BMP390, toggle GPIO from an AI client), published as a sample + video like the BMP390 one. Combines your two pillars; no queue to race. |

**Pounce rule:** check MCP's `ready for work` list weekly; an unclaimed item there goes straight to your top 3.

---

## 5. Looked at and ruled out (2026-10-22)

| Item | Why not |
|---|---|
| iot #1663 (Mcp23xxx reset, from list 002) | 🟡 claimed by managedmetal 2026-10-10; pgrawehr chose his option 1 |
| iot #2617 (DCMotor H-bridge), #2614 | 🟡 reporter implementing (#2617); #2614 fixed by pgrawehr's #2610 (merged 2026-10-22) |
| iot #2619 (other callbacks removed on unregister) | Filed by pgrawehr himself, and maintainers opened Copilot PRs #2629/#2630 on GPIO event cleanup the same day; likely in flight |
| Aspire #11121 (CSV export), #11299 | 🟡 PR #18639 open; #11299 already fixed on `main` per a comment |
| MCP #788, #1862 | 🟡 PRs open (#1709, #1875) |
| aspnetcore #2694 | 🟡 claimed 2026-08-09; aspnetcore is still too heavy for your machines |
| runtime #51613 (hosted services start after stop) | Triage 2026-01: "very low priority"; a previous PR was closed. Not worth the build cost |
| efcore "good first issue" (#19287, #29306) | Real and unclaimed, but query translation is far from your lanes. Keep for a later pass |
| OpenTelemetry .NET / Contrib | Labeled backlog is mostly `keep-open` design items. Your telemetry exposure now comes through Aspire and Agent Framework |
| Garnet, Orleans | Impressive Microsoft projects, but their `help wanted` lists are big features or stale. Revisit Orleans for distributed systems later |

---

## 6. Progress tracker

| Item | State (2026-10-22) | Next action | Done |
|---|---|---|---|
| YARP #275 / PR #3063 | in review | answer review promptly | |
| runtime #64507 | 🟢 | bench check at 250000 baud, post result | |
| iot #2615 | 🟢 | comment with fix options, then PR | |
| runtime #97741 | 🟢 | reproduce on Pi + capture DTR | |
| Aspire #20434 | 🟢 | clone Aspire, find the comparison, comment + PR | |
| Aspire #16273 | 🟢 | comment asking which fix; separate PR for error output | |
| iot #1519 | 🟢 | quick-write experiment on the bench, then design comment | |

---

## 7. Your feedback (fills `open_source_persona.md` §10.2–10.3)

1. Which lane excites you most: the bench (runtime/iot), the backend (Aspire), or AI?
2. Is moving the center of gravity to dotnet/runtime + Aspire right, or do you want to stay mostly in dotnet/iot?
3. Do you have a USB-serial Arduino (or similar) and a 24LC256-type EEPROM? (Decides #1 and #6.)

---

## Sources (checked 2026-10-22)

GitHub issue threads and timelines for every item linked above (via the GitHub REST API) · dotnet/runtime `main`:
`src/native/libs/System.IO.Ports.Native/pal_termios.c`, `src/libraries/System.IO.Ports/src/System/IO/Ports/SerialStream.Unix.cs`,
`global.json`, `.devcontainer/libraries/`, `CONTRIBUTING.md` · microsoft/aspire `main`: `global.json`,
`docs/contributing.md`, `src/Aspire.Hosting.Azure.Storage/AzureStorageExtensions.cs` · dotnet/iot `main`:
`RaspberryPi3Driver.cs`, `RaspberryPi3LinuxDriver.cs`, open PR list.
