# Open-Source Workbench

My workspace for contributing to open-source projects, mostly in the .NET ecosystem (YARP, dotnet/iot, the MCP
C# SDK, OpenTelemetry .NET).

For each issue I work on, this repo keeps:

- a **minimal reproduction** that shows the problem (or proves a fix) on a real running system,
- the **captured output** from those runs, with the exact SDK and package versions, and
- my **notes and write-ups** on the concepts I needed to learn along the way.

Upstream pull requests link back to the matching issue folder, so reviewers can check the evidence or run it
themselves.

> **If you arrived here from a PR or issue comment:** open the issue folder linked below. Its `README.md` has
> the claim, the environment, the exact commands to reproduce it, and the observed results.

---

## Issues

| Upstream issue | Folder | What's here | Status |
|---|---|---|---|
| [dotnet/yarp#1764](https://github.com/dotnet/yarp/issues/1764): document the WebSocket keep-alive requirement | [`yarp/1764_websocket_idle_timeout/`](yarp/1764_websocket_idle_timeout/) | 3-process repro (client → YARP → echo server) of the idle-WebSocket abort at `ActivityTimeout`, and the `KeepAliveInterval` fix | Repro verified · issue comment not yet posted |

Candidate issues I'm still weighing are in [`scouting/`](scouting/).

---

## Layout

```
.
├── README.md                     ← you are here (public index)
├── CLAUDE.md                     ← workflow and conventions for working in this repo (human + AI)
├── scouting/                     ← issue searches: dated shortlists of candidate issues across repos
├── lectures/<topic>/             ← concept write-ups reusable across issues (e.g. websockets, async)
├── <repo>/<issue#>_<slug>/       ← one folder per issue I'm actually working on
│   ├── README.md                 ← public evidence page: claim · environment · repro steps · results
│   ├── CLAUDE.md                 ← working notes: status, decisions, next steps
│   ├── sample/                   ← runnable reproduction / experiment code
│   │   └── evidence/             ← captured run output, dated, with versions
│   ├── concept_notes/            ← append-only Q&A log while reading the issue and source
│   ├── lectures/                 ← longer, issue-specific write-ups of experiments
│   └── implementations/          ← plans and build logs for the fix itself
└── hand_experiments/             ← older, hand-written practice projects (Kafka, Redis, Rx, ...); not OSS work
```

Upstream source code is **not** vendored here. Forks are cloned separately; this repo only holds reproductions,
evidence and notes.

---

## How I use AI here

Code in the issue folders (reproductions, experiments) and many of the write-ups are produced with AI assistance
(Claude), then run and checked on my machine. Anything I submit upstream follows that project's contribution and
AI-disclosure policy.
