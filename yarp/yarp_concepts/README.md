# YARP concepts

Full lectures on YARP and the networking concepts behind it, written to stay useful after the issue that prompted
them. Style and template: root [`CLAUDE.md`](../../CLAUDE.md) §4.4 and §8.5.

| # | Lecture | Prompted by | Status |
|---|---|---|---|
| 001 | [The Gatekeeper in the Middle](001-the-gatekeeper-in-the-middle.md): what a reverse proxy is; sockets, connections and the 4-tuple; routes/clusters/destinations; how YARP handles an HTTP request vs. a WebSocket; who owns and closes connections; the activity-timeout Watchdog; keep-alives and the survival rule | [#1764](../1764_websocket_idle_timeout/) | Complete (2026-09-27) |

The older, narrower lecture on the #1764 repro is still at [`../../lectures/001-yarp-websocket-activity-timeout.md`](../../lectures/001-yarp-websocket-activity-timeout.md);
it moves here when the YARP folder is migrated to the v2 layout (root `CLAUDE.md` §3).
