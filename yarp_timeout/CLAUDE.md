# YARP WebSocket Idle-Timeout Experiment

## What this is

A play project to observe and document the timeout YARP (Microsoft's reverse proxy) applies to WebSocket
connections that go idle, and why. It grew out of scanning open-source issues for something worth
contributing to: [YARP #1764](https://github.com/dotnet/yarp/issues/1764) asks for docs explaining that
clients need to send keep-alives or their idle WebSocket connections through YARP get dropped. Rather than
just writing the doc fix from the maintainer's existing explanation, the plan is to reproduce and understand
the behavior myself first, then write it up.

## Unlike `hand_experiments/`

That folder's hard rule is "no AI-written code, review and teach only." That rule does **not** apply here —
AI can write implementation code directly in this folder. See the [repo-root `CLAUDE.md`](../CLAUDE.md) for
how the two halves of the repo differ.

## Structure

- `concept_notes/` — an append-only Q&A log. As I read source or the implementation notes below and hit
  something I don't understand, I write the question down and the answer gets appended here, newest at the
  bottom of the file. Short and issue-focused, not a full lecture.
- `implementations/` — research and build logs: what was explored, what was decided, and the plan going
  forward.
- `lectures/` — step-by-step, teach-me-what-happened write-ups of the hands-on experiments, once there's
  something to report. Longer-form than `concept_notes/`.
- `sample/` — the runnable dotnet solution: `EchoServer` (destination, configurable `KeepAliveInterval`),
  `Proxy` (YARP, configurable `ActivityTimeout`), `IdleClient` (a console client that goes idle and reports
  when/if the connection dies). Pinned to SDK `10.0.302` via its own `global.json`.
- All markdown folders use `NNN-title.md` naming, sequential per folder, oldest first (same convention as
  `hand_experiments/lectures/`).

## Status — hands-on repro built and verified

- `implementations/001-issue_shortlist_sept_2026.md` is a broader OSS-issue shortlist across several repos,
  not specific to this experiment. Item **A** in it is the YARP WebSocket keep-alive doc issue that seeded
  this folder.
- `concept_notes/001-questions_from_the_issue_shortlist.md` is the Q&A unpacking sockets, the WebSocket
  handshake, YARP's proxy byte-pumping, idle timeouts, and keep-alives/heartbeats — motivated by item A.
- `lectures/001-yarp-websocket-activity-timeout.md` builds and runs the three-piece sample, reproduces the
  100s-idle drop, confirms the `KeepAliveInterval` fix, and confirms the gotcha that ASP.NET Core's own
  default `KeepAliveInterval` (2 min) is *not* enough against YARP's default `ActivityTimeout` (100s) — all
  verified against running processes, not just read from docs.

## Next steps

1. Decide whether the write-up turns into an actual comment/PR on YARP #1764 (draft is in
   `concept_notes/001-...md` Q2, now backed by `lectures/001-...md`'s verified numbers), or just stays as
   notes here.
2. Optional: wire up `Yarp.Telemetry.Consumption` in the `Proxy` project to directly observe
   `WebSocketCloseReason.ActivityTimeout` firing, closing the loop from Q2's research.
