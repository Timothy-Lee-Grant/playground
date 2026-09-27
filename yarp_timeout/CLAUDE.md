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
- Both use `NNN-title.md` naming, sequential per folder, oldest first (same convention as
  `hand_experiments/lectures/`).
- (Not yet created) a runnable dotnet project — a minimal YARP instance proxying to a WebSocket backend —
  once the research phase below is far enough along to know what to build.

## Status — research phase, no runnable code yet

- `implementations/001-issue_shortlist_sept_2026.md` is a broader OSS-issue shortlist across several repos,
  not specific to this experiment. Item **A** in it is the YARP WebSocket keep-alive doc issue that seeded
  this folder.
- `concept_notes/001-questions_from_the_issue_shortlist.md` is the Q&A unpacking sockets, the WebSocket
  handshake, YARP's proxy byte-pumping, idle timeouts, and keep-alives/heartbeats — motivated by item A.

## Next steps

1. Reproduce the idle timeout locally: a minimal YARP proxy in front of a WebSocket backend, with a client
   that goes quiet.
2. Pin down exactly where the timeout comes from — YARP config, Kestrel, or OS socket defaults — and its
   default value(s).
3. Decide whether the write-up turns into a contribution back to YARP #1764, or just stays as notes here.
