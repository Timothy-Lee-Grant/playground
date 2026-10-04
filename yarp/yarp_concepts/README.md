# YARP concepts

Full lectures on YARP and the networking concepts behind it, written to stay useful after the issue that prompted
them. Style and template: root [`CLAUDE.md`](../../CLAUDE.md) §4.4 and §8.5.

| # | Lecture | Prompted by | Status |
|---|---|---|---|
| 001 | [The Gatekeeper in the Middle](001-the-gatekeeper-in-the-middle.md): what a reverse proxy is; sockets, connections and the 4-tuple; routes/clusters/destinations; how YARP handles an HTTP request vs. a WebSocket; who owns and closes connections; the activity-timeout Watchdog; keep-alives and the survival rule | [#1764](../1764_websocket_idle_timeout/) | Complete (2026-09-27) |

The older, narrower lecture on the #1764 repro is still at [`../../lectures/001-yarp-websocket-activity-timeout.md`](../../lectures/001-yarp-websocket-activity-timeout.md);
it moves here when the YARP folder is migrated to the v2 layout (root `CLAUDE.md` §3).

## Audio lectures (for NotebookLM)

Written to be **listened to**: loaded into Google NotebookLM to generate a podcast-style Audio Overview. Only written
when Timothy explicitly asks for one. Format: root [`../../CLAUDE.md`](../../CLAUDE.md) §10.7. Files live in
[`audio/`](audio/): `NNN-audio-<topic>.md` (the source to upload) + `NNN-audio-<topic>.prompt.md` (paste into
NotebookLM's "Customize" box; don't upload it).

| # | Audio lecture | Prompted by | Status |
|---|---|---|---|
| A001 | [YARP from the ground up: what it is, every part that matters, and the .NET ideas underneath](audio/001-audio-yarp-from-the-ground-up.md) ([customize prompts](audio/001-audio-yarp-from-the-ground-up.prompt.md)) | Orientation for all YARP work, incl. #275: library vs framework, middleware/RequestDelegate, DI and lifetimes, routes/clusters/destinations, one request step by step, health, repo tour, Moq/AutoMock and the two traps. Checked against `main` @ `0cae8ca` | Written 2026-10-03 (~10,000 words, split into two episodes via the prompt file); not yet listened to |
