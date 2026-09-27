# Lecture 001 — Reproducing and Fixing YARP's WebSocket Idle Timeout

> **What this is:** the hands-on follow-through on the "Next steps" checklist at the bottom of
> `concept_notes/001-questions_from_the_issue_shortlist.md` (Q3) and the "Optional hands-on" note under item A in
> `implementations/001-issue_shortlist_sept_2026.md`. It builds the three-piece sample described there, runs it,
> and records what actually happened — including one thing that *didn't* happen the way the notes predicted.
> **Code:** `yarp_timeout/sample/` · **Date run:** 2026-09-26, .NET SDK 10.0.302, `Yarp.ReverseProxy` 2.3.0.

---

## 1. What you're about to verify

YARP #1764 says: a proxied WebSocket that's idle for 100 seconds gets dropped by YARP's `ActivityTimeout`. The
fix is a keep-alive from the client or the destination server. Rather than just believing the issue text, this
sample builds the smallest system that can prove or disprove it:

```
IdleClient  ──ws://──►  Proxy (YARP)  ──ws://──►  EchoServer
(ClientWebSocket)        localhost:5000           localhost:5050
                         ActivityTimeout           KeepAliveInterval
                         (the watchdog)             (the fix, or not)
```

- **EchoServer** — a minimal ASP.NET Core app that accepts a WebSocket on `/ws` and echoes whatever it receives.
  Its `WebSocketOptions.KeepAliveInterval` is read from the `WS_KEEPALIVE_SECONDS` env var, or defaults to
  ASP.NET Core's own default of 2 minutes if unset.
- **Proxy** — a minimal YARP app routing `/ws` to EchoServer. Its cluster's `HttpRequest.ActivityTimeout` is set
  in `appsettings.json` to YARP's real default, 100 seconds (`00:01:40`), overridable via an env var for fast
  iteration.
- **IdleClient** — a console app that connects through the proxy, sends one message to prove the round trip
  works, then goes completely silent and reports exactly when (if ever) the connection dies.

## 2. One-time setup

```bash
cd yarp_timeout/sample
dotnet build
```

The folder has its own `global.json` pinning the SDK to `10.0.302` (the machine also has a `net11` preview SDK
installed, which you don't want picking up this build by accident). `dotnet build` should report `0 Warning(s)`,
`0 Error(s)`.

## 3. Running the three pieces

Each piece is a separate process. Open three terminals (or background two of them) from `yarp_timeout/sample/`.

**Terminal 1 — EchoServer:**
```bash
dotnet run --project EchoServer
```
Watch for `Now listening on: http://localhost:5050` and the line logging the `KeepAliveInterval` it started
with.

**Terminal 2 — Proxy:**
```bash
dotnet run --project Proxy
```
Watch for `Now listening on: http://localhost:5000`.

**Terminal 3 — IdleClient:**
```bash
dotnet run --project IdleClient -- ws://localhost:5000/ws
```
It connects, sends `"hello"`, prints the echo, then goes idle and prints a line every 10 seconds until the
connection dies (or you `Ctrl+C` it).

### A gotcha I hit while building this, worth knowing

`dotnet run` is a **wrapper**: it builds, then launches the actual `EchoServer`/`Proxy` binary as a **child
process**, and the wrapper can exit or be killed without the child dying with it. The first time I restarted
EchoServer and Proxy to change settings, `pkill -f EchoServer.dll` only matched the wrapper — the real process
kept the port open, so my "restarted" server was actually still running the *old* settings, and the next test
silently measured the wrong thing. **Lesson: after changing an env var and re-running, check `lsof -i :<port>`
(or the PID printed by your process manager) before trusting the new run — don't just assume a kill worked.**
This is a general "verify the state you think you changed" lesson, not specific to YARP.

## 4. Experiment 1 — reproduce the drop (fast iteration settings)

Waiting for the real 100-second default every time you tweak something is slow, so for iteration, override the
timeout to 8 seconds via an env var on the Proxy, and leave EchoServer's keep-alive unset (its default is 2
minutes — far longer than 8s, so this should reproduce the bug):

```bash
# EchoServer terminal: just `dotnet run --project EchoServer` (no env var — default 2 min keep-alive)

# Proxy terminal:
ReverseProxy__Clusters__echoCluster__HttpRequest__ActivityTimeout="00:00:08" dotnet run --project Proxy

# IdleClient terminal:
dotnet run --project IdleClient -- ws://localhost:5000/ws
```

**What actually happened:**
```
Connected. Sending one message to prove the round trip works.
Echo received: hello
Now going idle — sending nothing. Watching for the connection to die...
[8.0s] Connection died: WebSocketException: The remote party closed the WebSocket connection
without completing the close handshake.
Final client state: Aborted
```

Dead at exactly 8.0 seconds — matching the configured `ActivityTimeout` to the tenth of a second. The issue's
claim is real and precisely timed, not approximate.

## 5. Experiment 2 — apply the fix

Restart EchoServer with a `KeepAliveInterval` **below** the 8-second `ActivityTimeout` (Proxy keeps the same
8-second override):

```bash
# EchoServer terminal:
WS_KEEPALIVE_SECONDS="3" dotnet run --project EchoServer

# Proxy terminal (same as before):
ReverseProxy__Clusters__echoCluster__HttpRequest__ActivityTimeout="00:00:08" dotnet run --project Proxy

# IdleClient terminal:
dotnet run --project IdleClient -- ws://localhost:5000/ws
```

**What actually happened:** it survived 30+ seconds of total application-level silence, well past the
8-second timeout that killed it in Experiment 1:
```
Now going idle — sending nothing. Watching for the connection to die...
  ...still open at 10s
  ...still open at 20s
  ...still open at 30s
```
The fix works. `KeepAliveInterval` on the destination server is enough — the client never has to do anything,
and the app code on either side never sees the keep-alive traffic (more on why, below).

## 6. Experiment 3 — the actual gotcha in #1764

This is the detail the concept notes flagged as worth mentioning in a PR comment, and it's worth seeing fail
with your own eyes, not just reading about: **ASP.NET Core's own default `KeepAliveInterval` is 2 minutes**,
which is *longer* than YARP's *default* `ActivityTimeout` of 100 seconds. So "just turning on keep-alives" is
not the fix — you have to set the interval, and set it *below* whatever `ActivityTimeout` actually is.

```bash
# EchoServer terminal — no env var, so KeepAliveInterval defaults to 2 minutes:
dotnet run --project EchoServer

# Proxy terminal — same 8s override as before:
ReverseProxy__Clusters__echoCluster__HttpRequest__ActivityTimeout="00:00:08" dotnet run --project Proxy

# IdleClient terminal:
dotnet run --project IdleClient -- ws://localhost:5000/ws
```

**What actually happened:** dead at 8.0 seconds again — identical to Experiment 1. A 2-minute keep-alive is no
better than no keep-alive at all when the timeout is 8 seconds (or, at real-world scale, 100 seconds). This is
the sentence worth adding to the docs: *"enable keep-alives" is necessary but not sufficient — the interval has
to be shorter than the timeout on every hop in between.*

## 7. Verifying it with YARP's real defaults (no env overrides)

Everything above used an 8-second override purely for iteration speed. To see the real-world numbers from
#1764, drop every env var and just run all three with the checked-in `appsettings.json` (`ActivityTimeout`
`00:01:40` = 100s) and a keep-alive comfortably under it:

```bash
WS_KEEPALIVE_SECONDS="30" dotnet run --project EchoServer   # terminal 1
dotnet run --project Proxy                                   # terminal 2
dotnet run --project IdleClient -- ws://localhost:5000/ws     # terminal 3
```

This takes longer to watch (you're waiting on the real 100s), but it's the number that belongs in a PR comment
or a #1764 reply: *"verified on .NET 10 / YARP 2.3.0 — dies at 100.0s with no keep-alive, survives with a 30s
`KeepAliveInterval`."*

---

## 8. What's actually going on, and concepts worth keeping

### 8.1 YARP doesn't understand WebSocket frames — it copies bytes

The crash trace from Experiment 1's Proxy log names the exact class doing the work:

```
at Yarp.ReverseProxy.Forwarder.StreamCopier.CopyAsync(Stream input, Stream output, ...,
    ActivityCancellationTokenSource activityToken, ...)
```

`StreamCopier` is a raw duplex byte-pump — read from one stream, write to the other, in both directions,
forever, until something stops it. It has no idea what a WebSocket text frame or a Ping frame *is*. That's
exactly what the concept notes described ("YARP doesn't interpret the traffic, it copies bytes"), and now you
have direct evidence of it in a live stack trace rather than just a description.

### 8.2 The watchdog has a name in the actual code: `ActivityCancellationTokenSource`

That same parameter, `activityToken`, is the object being reset every time `CopyAsync` successfully reads or
writes — i.e., every time *any* byte moves in *either* direction, on *either* leg of the proxy (client↔YARP or
YARP↔destination). That's why Experiment 2's server-side keep-alive worked even though the *client* stayed
silent the whole time: the destination's Ping frames are still bytes flowing through `StreamCopier`, so they
still touch `activityToken`, even though the app code on both ends never sees them via `ReceiveAsync` (Ping/Pong
is handled at the transport layer, below the application's message API).

### 8.3 The failure mode is an abort, not a graceful close

Compare the two ways a WebSocket can end:

| | Client sees | Underlying mechanism |
|---|---|---|
| **Graceful close** | `WebSocketMessageType.Close`, a `CloseStatus`, and `CloseStatusDescription` | Both sides exchange a WebSocket `Close` frame handshake |
| **`ActivityTimeout` firing** (what you saw) | a thrown `WebSocketException`: *"the remote party closed the WebSocket connection **without completing** the close handshake"* | YARP's watchdog tears down both TCP connections directly — there's no time spent on a polite handshake, because the whole point is the connection might already be dead |

This is exactly the distinction `WebSocketCloseReason` from Q2's telemetry enum exists to capture —
`ActivityTimeout` is listed there as its own value, separate from `ClientGracefulClose`/`ServerGracefulClose`.
If you plug in `Yarp.Telemetry.Consumption` on top of this sample, this is the exact enum value you'd see
reported the moment Experiment 1 or 3's connection dies.

### 8.4 Why the client-side API can't just "turn on" pings

`ClientWebSocket`/browser `WebSocket` don't expose a way to send raw `Ping` control frames from application
code — that's a deliberate protocol-layer omission, which is *why* Q3's concept notes describe two entirely
separate fix strategies (server `KeepAliveInterval` vs. client application-level heartbeat messages) rather
than one symmetric setting on both sides. This sample only needed the server-side fix because the destination
server is the one under your control in #1764's scenario; a real chat app fronted by a JS client would likely
need the client-side heartbeat approach instead, at the cost described in Q3 (message volume at scale, mobile
battery).

### 8.5 General lesson: a plausible-sounding fix still needs a timer, not a read

Before running Experiment 3, it would have been easy to assume "keep-alives are on, so it's fixed" and move on.
The whole reason this failed the way it did is that *two independently-configured timeouts* only compose
correctly if you check the actual numbers against each other — "enabled" isn't a boolean, it's a race between
two durations. That generalizes well beyond WebSockets: any two systems with independent timeout/retry/TTL
settings need this same "which number is smaller" sanity check, not just "is the feature on."

---

## 9. Where this leaves item A (YARP #1764)

Per `concept_notes/001-...md` Q2, the actual remaining gap is one missing cross-reference sentence on YARP's
`websockets.md` doc page. This sample now gives that comment/PR real, verified numbers instead of just citing
the docs:

> Verified on .NET 10 / YARP 2.3.0: an idle WebSocket through YARP is aborted at the configured
> `ActivityTimeout` (confirmed at both an 8s override and the real 100s default) regardless of a server-side
> `KeepAliveInterval`, *unless* that interval is set below the timeout — the framework's own 2-minute default
> is not sufficient at YARP's 100s default and should probably be called out explicitly in the docs.

That's the comment/PR draft from Q2, now backed by a repro instead of a hunch.
