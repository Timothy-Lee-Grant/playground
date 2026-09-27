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
| [dotnet/yarp#1764](https://github.com/dotnet/yarp/issues/1764): document the WebSocket keep-alive requirement | [`yarp/1764_websocket_idle_timeout/`](yarp/1764_websocket_idle_timeout/) | 3-process repro (client → YARP → echo server) of the idle-WebSocket abort at `ActivityTimeout`, and the `KeepAliveInterval` fix | Repro verified · issue comment not yet posted |
| [dotnet/iot#2403](https://github.com/dotnet/iot/issues/2403): `GpioPin` event handlers receive the wrong `sender` | [`iot/2403_gpiopin-event-handler-assing-wrong-sender-value/`](iot/2403_gpiopin-event-handler-assing-wrong-sender-value/) | Investigation of which object each GPIO driver passes as `sender` | Exploring (started 2026-09-27) · nothing verified or posted yet |

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
