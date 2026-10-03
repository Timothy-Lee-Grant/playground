# Open-Source Workbench

My workspace for contributing to open-source projects, mostly in the .NET ecosystem (dotnet/iot, YARP, the MCP
C# SDK, OpenTelemetry .NET).

For each issue I work on, this repo keeps:

- a **minimal reproduction** that shows the problem (or proves a fix) on a real running system,
- the **captured output** from those runs, with the exact SDK and package versions,
- a **lab report** that walks through the problem, how each result was tested, and what it showed, and
- my **notes and lectures** on the concepts I needed to learn along the way.

Upstream pull requests and comments link back to the matching issue folder, so reviewers can check the evidence or
run it themselves.

> **If you arrived here from a PR or issue comment:** open the issue folder linked below. Its `README.md` points to
> the report, the runnable sample and the raw evidence.

---

## Issues

| Upstream issue | Folder | What's here | Status |
|---|---|---|---|
| [dotnet/yarp#1764](https://github.com/dotnet/yarp/issues/1764): document the WebSocket keep-alive requirement | [`yarp/1764_websocket_idle_timeout/`](yarp/1764_websocket_idle_timeout/) | 3-process repro (client → YARP → echo server) of the idle-WebSocket abort at `ActivityTimeout`, and the `KeepAliveInterval` fix | PR [dotnet/AspNetCore.Docs#37747](https://github.com/dotnet/AspNetCore.Docs/pull/37747) open, issue commented (2026-09-28) · awaiting review |
| [dotnet/iot#2403](https://github.com/dotnet/iot/issues/2403): `GpioPin` event handlers receive the wrong `sender` | [`iot/2403_gpiopin-event-handler-assing-wrong-sender-value/`](iot/2403_gpiopin-event-handler-assing-wrong-sender-value/) | Investigation of which object each GPIO driver passes as `sender` | Exploring (started 2026-09-27) · nothing verified or posted yet |
| [dotnet/iot#2600](https://github.com/dotnet/iot/issues/2600): `LibGpiodV2Driver` aborts the process on 32-bit ARM | [`iot/2600_libgpiodv2-edge-event-abort-on-arm32/`](iot/2600_libgpiodv2-edge-event-abort-on-arm32/) | Test of the proposed root cause (C `unsigned long` declared as `ulong` in the P/Invoke binding) on 64-bit and 32-bit ARM | Exploring (started 2026-09-29) · nothing verified or posted yet |
| [dotnet/iot#2328](https://github.com/dotnet/iot/issues/2328): `GpioButton.IsPressed` is wrong when the button is held at startup | [`iot/2328_gpiobutton-ispressed-not-initialized/`](iot/2328_gpiobutton-ispressed-not-initialized/) | Unit tests with a mocked GPIO driver proving the startup-state bug, then the fix | **PR [#2611](https://github.com/dotnet/iot/pull/2611) open** (2026-10-02); issue commented 2026-10-01 · awaiting maintainer reply, PR planned |
| [dotnet/yarp#275](https://github.com/dotnet/yarp/issues/275): remove Autofac from YARP's tests | [`yarp/275_remove-autofac-from-tests/`](yarp/275_remove-autofac-from-tests/) | Behavior-preserving rewrite of 7 test classes, with evidence each test still guards what it did | Set up 2026-09-30 · queued; asking maintainers first |

Candidate issues I'm still weighing are in [`scouting/`](scouting/). How I choose them, and what I'm aiming to learn, is in
[`open_source_persona.md`](open_source_persona.md).

---

## Layout

```
.
├── README.md                   ← you are here (public index)
├── CLAUDE.md                   ← workflow, conventions and templates (for me and for AI assistants)
├── open_source_persona.md      ← my contributor profile: goals, strengths, how I choose issues, record
├── persona.md                  ← how I learn (and how AI assistants should teach me)
├── scouting/                   ← issue searches: dated shortlists of candidate issues across repos
├── <repo>/                     ← one folder per upstream project (iot/, yarp/, ...)
│   ├── CLAUDE.md               ← orientation for that upstream project
│   ├── <repo>_concepts/        ← lectures on that project's concepts, reusable across issues
│   └── <issue#>_<slug>/        ← one folder per issue I'm working on
│       ├── README.md           ← landing page: claim · status · where to look
│       ├── CLAUDE.md           ← orientation and status for this issue
│       ├── conversation/       ← linear log of the work: questions, answers, progress, decisions
│       ├── sample/             ← runnable experiments
│       │   └── evidence/       ← captured run output, dated, with versions
│       └── report/             ← the finished lab report
├── lectures/                   ← lectures on concepts that aren't specific to one project,
│                                  plus a cross-repo catalog of architecture patterns
└── hand_experiments/           ← older, hand-written practice projects (Kafka, Redis, Rx, ...); not OSS work
```

`yarp/1764_websocket_idle_timeout/` predates this layout (it has `concept_notes/` and `implementations/` instead of
`conversation/` and `report/`) and will be migrated when that issue is finished.

Upstream source code is **not** vendored here. Forks are cloned separately; this repo only holds reproductions,
evidence and notes.

---

## How I use AI here

Code in the issue folders (reproductions, experiments) and many of the write-ups are produced with AI assistance
(Claude), then run and checked on my machine. Anything I submit upstream follows that project's contribution and
AI-disclosure policy.
