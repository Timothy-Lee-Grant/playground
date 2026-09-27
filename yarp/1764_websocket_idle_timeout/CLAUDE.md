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
  dies). Pinned to SDK `10.0.302` by its own `global.json`.
- `sample/evidence/`: captured run output (not created yet; see next steps).
- `concept_notes/001-questions_from_the_issue_shortlist.md`: append-only Q&A. Q1 covers why maintainers leave
  small docs issues open, Q2 where YARP's docs live and what the Timeouts page already covers (plus a draft
  comment), and Q3 sockets, the WebSocket handshake, proxy byte-pumping, idle timeouts and keep-alives.
- `lectures/001-yarp-websocket-activity-timeout.md`: builds and runs the sample, reproduces the drop, confirms
  the fix, and shows the gotcha that ASP.NET Core's default 2-minute keep-alive is longer than YARP's 100 s timeout.
- `implementations/`: plans and build logs for the actual docs change (empty so far).

## Status

- 2026-09-26: issue open and unclaimed (re-check before commenting).
- 2026-09-26: found that the YARP **Timeouts** page already covers most of this in its WebSockets section. The
  **WebSockets** page doesn't mention `ActivityTimeout`. The remaining gap is likely one cross-reference sentence
  plus the default-interval gotcha (concept notes Q2).
- 2026-09-26: reproduction built and run. Verified with captured console output at an 8 s override: abort at
  8.0 s with no effective keep-alive; survives with a 3 s keep-alive; the 2-minute default keep-alive does not help.
  Runs at the real 100 s default are described in lecture 001 but the output wasn't saved.
- Nothing posted upstream yet.

## Next steps

1. Re-run experiments 1–4 from `README.md` and save each run's output to `sample/evidence/NNN-<name>.txt` with a
   version header. The two real-default (100 s) runs matter most, since those are the numbers the comment cites.
2. Re-check #1764 for new comments or claims, then post the comment drafted in concept notes Q2, updated with the
   verified numbers and a link to this folder's `README.md`.
3. Depending on the maintainers' answer, either open a small PR in `dotnet/AspNetCore.Docs` (one section in
   `websockets.md` cross-referencing the Timeouts page and calling out the default-interval gotcha), or let them
   close it as covered. Log the plan in `implementations/001-...md`.
4. Optional: add `Yarp.Telemetry.Consumption` to `Proxy` to observe `WebSocketCloseReason.ActivityTimeout`
   directly.
5. Update the root `README.md` issues table and the scouting tracker when anything is posted.
