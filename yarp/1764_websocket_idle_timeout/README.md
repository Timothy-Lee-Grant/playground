# dotnet/yarp#1764: idle WebSockets are aborted at `ActivityTimeout` unless a keep-alive is shorter than it

**Upstream issue:** [dotnet/yarp#1764](https://github.com/dotnet/yarp/issues/1764) (docs; YARP's docs now live
in [dotnet/AspNetCore.Docs](https://github.com/dotnet/AspNetCore.Docs) under `aspnetcore/fundamentals/servers/yarp/`)
· **Related PR:** not yet · **Status:** reproduction verified; issue comment not yet posted

## Claim

A WebSocket proxied through YARP that carries no traffic for `ActivityTimeout` (default **100 s**) is aborted by
the proxy. A WebSocket-level keep-alive from the destination server (`WebSocketOptions.KeepAliveInterval`) keeps it
alive, **but only if the interval is shorter than `ActivityTimeout`**. ASP.NET Core's default `KeepAliveInterval`
(2 minutes) is longer than YARP's default timeout (100 s), so enabling keep-alives with the default interval is not
enough.

## Environment

- .NET SDK 10.0.302 (pinned in [`sample/global.json`](sample/global.json)), `net10.0`
- `Yarp.ReverseProxy` 2.3.0
- macOS, all three processes on `localhost`
- Runs performed 2026-09-26

## Setup

```
IdleClient  ──ws://──►  Proxy (YARP)  ──ws://──►  EchoServer
(ClientWebSocket)        localhost:5000           localhost:5050
sends 1 message,         ActivityTimeout          KeepAliveInterval
then goes silent         (appsettings: 00:01:40)  (env WS_KEEPALIVE_SECONDS; default 2 min)
```

| Project | What it does |
|---|---|
| [`sample/EchoServer`](sample/EchoServer/Program.cs) | Minimal ASP.NET Core app; echoes WebSocket messages on `/ws`. `KeepAliveInterval` is read from `WS_KEEPALIVE_SECONDS`, default 2 min |
| [`sample/Proxy`](sample/Proxy/Program.cs) | Minimal YARP app routing `/ws` to EchoServer. `ActivityTimeout` set in [`appsettings.json`](sample/Proxy/appsettings.json) to YARP's default of 100 s, overridable by env var |
| [`sample/IdleClient`](sample/IdleClient/Program.cs) | Connects through the proxy, does one echo round trip, then sends nothing and reports when (if ever) the connection dies |

## Reproduce

```bash
cd yarp/1764_websocket_idle_timeout/sample
dotnet build

# Terminal 1: destination server (omit WS_KEEPALIVE_SECONDS for the 2-minute default)
WS_KEEPALIVE_SECONDS=3 dotnet run --project EchoServer

# Terminal 2: YARP (the override shortens the 100 s timeout to 8 s for quick iteration)
ReverseProxy__Clusters__echoCluster__HttpRequest__ActivityTimeout="00:00:08" dotnet run --project Proxy

# Terminal 3: client
dotnet run --project IdleClient -- ws://localhost:5000/ws
```

Before re-running with different settings, confirm the old processes are gone (`lsof -i :5000`, `lsof -i :5050`).
`dotnet run` launches the app as a child process that can outlive the wrapper you killed.

## Results

| # | `ActivityTimeout` | Server `KeepAliveInterval` | Expected | Observed |
|---|---|---|---|---|
| 1 | 8 s (override) | 2 min (ASP.NET Core default) | aborted at ~8 s | **Aborted at 8.0 s**: `WebSocketException: The remote party closed the WebSocket connection without completing the close handshake.` Client state `Aborted` |
| 2 | 8 s (override) | 3 s | survives | **Still open at 30 s+** of application-level silence |
| 3 | 100 s (YARP default) | 2 min (ASP.NET Core default) | aborted at ~100 s | Reported as aborted at 100.0 s in lecture 001 §9; **log not saved** |
| 4 | 100 s (YARP default) | 30 s | survives | Reported as surviving in lecture 001 §7; **log not saved** |

Proxy-side stack trace on abort (experiment 1) shows the byte pump being cancelled:
`Yarp.ReverseProxy.Forwarder.StreamCopier.CopyAsync(..., ActivityCancellationTokenSource activityToken, ...)`.

**Note:** the raw console output for experiments 1 and 2 is recorded in
[`lectures/001-yarp-websocket-activity-timeout.md`](../../lectures/001-yarp-websocket-activity-timeout.md). Output for
experiments 3 and 4 (real defaults) has not been saved yet. Treat those two rows as unverified until the logs are
in `sample/evidence/`.

## Why it happens (short version)

YARP doesn't parse WebSocket frames after the upgrade. It copies bytes in both directions (`StreamCopier`) and
resets an activity watchdog whenever any byte moves. Server Ping/Pong control frames are bytes too, so they reset
the watchdog even though neither application sees them. If nothing moves for `ActivityTimeout`, YARP tears down
both connections without a close handshake. Longer write-up:
[`lectures/001-yarp-websocket-activity-timeout.md`](../../lectures/001-yarp-websocket-activity-timeout.md).
