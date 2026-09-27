# dotnet/yarp#1764: WebSocket idle timeout and keep-alives

## What this is

[YARP #1764](https://github.com/dotnet/yarp/issues/1764) (`Type: Documentation`, `help wanted`) asks for docs
explaining that idle WebSocket connections through YARP get dropped at the activity timeout (100 s by default),
and that keep-alives have to come from the client or the destination server. It's item **A** in
[`scouting/001-issue_shortlist_sept_2026.md`](../../scouting/001-issue_shortlist_sept_2026.md), picked as a
low-risk first PR. Instead of copying the maintainer's explanation into the docs, the plan was to reproduce the
behavior first and then write it up with verified numbers.

The public evidence page for this issue is [`README.md`](README.md). Keep it in sync with what's verified here.

## Upstream rules

- YARP's docs moved into **`dotnet/AspNetCore.Docs`** (`aspnetcore/fundamentals/servers/yarp/`). A docs fix is a
  PR to *that* repo, referencing `dotnet/yarp#1764`.
- AspNetCore.Docs has its own style rules and a PR template. Follow them exactly and keep the change to one
  section.
- Before posting, check the AI-disclosure guidance in both repos' CONTRIBUTING.md and note it here.

## Structure

- `README.md`: public evidence page (claim, environment, repro commands, results). This is the link for the PR.
- `sample/`: the runnable solution. `EchoServer` (destination, `KeepAliveInterval` from `WS_KEEPALIVE_SECONDS`),
  `Proxy` (YARP, `ActivityTimeout` in `appsettings.json`), `IdleClient` (goes idle and reports when the connection
  dies; `WS_CLIENT_KEEPALIVE_SECONDS`, `IDLE_MAX_SECONDS`). Pinned to SDK `10.0.302` by its own `global.json`.
- `sample/tools/`: `run-experiment.sh` (one-command runs; writes headed evidence files; checks ports before and
  after), `wstap.py` (transparent relay that logs WebSocket frames), `KeepAliveDefaults/`, `browser/` (page plus
  `capture.mjs` for scripted headless-Chrome screenshots), and figure generators (`timeline_svg.py`,
  `logpanel.py`, `render.mjs`).
- `sample/evidence/`: raw run output `NNN-*.txt` (001 = first manual attempt, 002–009 = the 2026-09-27 session),
  `screenshots/` (browser runs), `figures/` (timelines generated from the tap logs), `panels/` (console views
  generated from each run's files).
- `concept_notes/001-questions_from_the_issue_shortlist.md`: append-only Q&A. Q1 covers why maintainers leave
  small docs issues open, Q2 where YARP's docs live and what the Timeouts page already covers (plus a draft
  comment), and Q3 sockets, the WebSocket handshake, proxy byte-pumping, idle timeouts and keep-alives.
- `../../lectures/001-yarp-websocket-activity-timeout.md` (repo-root lectures): builds and runs the sample, reproduces the drop, confirms
  the fix, and shows the gotcha that ASP.NET Core's default 2-minute keep-alive is longer than YARP's 100 s timeout.
- `implementations/001-lab-report-idle-websockets-through-yarp.md`: the full lab report for the evidence session
  (system, mechanism, method, every result with figures and screenshots, corrected claims, limitations).

## Status

- 2026-09-26: issue open and unclaimed (re-check before commenting).
- 2026-09-26: found that the YARP **Timeouts** page already covers most of this in its WebSockets section. The
  **WebSockets** page doesn't mention `ActivityTimeout`. The remaining gap is likely one cross-reference sentence
  plus the default-interval gotcha (concept notes Q2).
- 2026-09-26: reproduction built and run. Verified with captured console output at an 8 s override: abort at
  8.0 s with no effective keep-alive; survives with a 3 s keep-alive; the 2-minute default keep-alive does not help.
  Runs at the real 100 s default are described in lecture 001 but the output wasn't saved.
- 2026-09-27: re-verified. #1764 still open, unassigned, no activity since 2023-01-09. Docs unchanged. Found
  dotnet/yarp#2615 (2024), where a user hit exactly this. Evidence plan and comment/PR drafts in concept notes **Q4**.
- 2026-09-27: evidence captured at real defaults (runs 002–009; lab report `implementations/001`). Findings:
  aborted at 100.1 s whenever no endpoint's keep-alive is under 100 s (004, 006, browser 008); a 30 s server
  keep-alive fixes it (005, browser 009). **Correction:** "defaults on both sides → abort" is false for a .NET
  client, because `ClientWebSocket` sends a Pong every 30 s by default (003, and the first attempt 001). It's
  true for browsers, which can't send keep-alives. The Q4 §5.2 comment draft needs its verification sentence
  replaced; the text is in the lab report §6.3 and concept notes Q5.
- 2026-09-27: #1764 re-checked: still open, unassigned, Backlog, no activity since 2023-01-09. Docs unchanged.
- Nothing posted upstream yet.

## Next steps

The live checklist is **concept notes Q5 §4** (it continues Q4 §8). In short:

1. Review the evidence and the lab report; commit and push, so the README link in the comment works.
2. Re-check #1764, then post the comment using the **corrected** verification sentence (lab report §6.3).
3. Follow Q4 §6 depending on the reply; the PR text in Q4 §5.3 still stands as written.
4. Update the trackers (root `README.md`, scouting tracker) when anything is posted.
