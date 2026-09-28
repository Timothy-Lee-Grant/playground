# Lecture 001: The Gatekeeper in the Middle

## How YARP proxies connections, who owns what, and why it hangs up on quiet WebSockets

> **Prompted by:** [dotnet/yarp#1764](https://github.com/dotnet/yarp/issues/1764) and the evidence session in
> [`../1764_websocket_idle_timeout/implementations/001-lab-report-idle-websockets-through-yarp.md`](../1764_websocket_idle_timeout/implementations/001-lab-report-idle-websockets-through-yarp.md)
> · **Date:** 2026-09-27 · **YARP version studied:** 2.3.0 (source references are to `dotnet/yarp` `main`, checked 2026-09-27)
> · **Prerequisites:** none. Concept notes Q3 in the #1764 folder is a shorter first pass over some of this.
>
> **What this lecture is for:** you can already *run* the #1764 experiment. This lecture is about building the model
> underneath it, so the results stop being a list of configurations and become consequences of one picture. It
> answers the questions you raised on 2026-09-27: what a socket and a connection really are; whether a WebSocket
> through YARP runs from the client to YARP or from the client to the server; who manages connections; why YARP
> (and not the server) hangs up; why YARP is still involved after the handshake at all; and how a "stateless
> router" can carry a stateful connection.

---

## 0. The whole lecture on one screen

```
  THE CALLER                 THE GATEKEEPER (YARP, inside an ASP.NET Core app)              THE ECHO
  (browser / app)                                                                         (your server)

  socket ═══ TCP connection #1 ═══ socket   ┌────────────────────────┐   socket ═══ TCP connection #2 ═══ socket
  1.2.3.4:51000 ↔ proxy:443                  │ Pump →  (client→server) │   proxy:61000 ↔ 10.0.0.5:5050
                                             │ Pump ←  (server→client) │
                                             │ Watchdog: 100 s         │
                                             └────────────────────────┘

  1. There is no connection from the Caller to the Echo. There are TWO connections, and YARP owns one end of each.
  2. YARP makes ONE routing decision (at the handshake), then stays in the middle forever, copying bytes.
  3. Every owner of a socket polices its own resources. YARP's policy is a watchdog: no bytes for 100 s → hang up.
  4. Any byte resets the watchdog, including invisible WebSocket keep-alive frames, from either side.
  5. So: an idle WebSocket survives  ⇔  some endpoint sends keep-alives more often than the SMALLEST idle timer on the path.
```

If those five lines already feel obvious, skip to §9 (the rule, applied to your lab) and §12 (check yourself).
Otherwise, read it in order: each section builds on the one before.

---

## 1. What problem does a reverse proxy solve?

### 1.1 The situation without one

Say you run three copies of a web app on three machines (`10.0.0.5`, `10.0.0.6`, `10.0.0.7`), because one machine
can't handle all your users, and because you want to keep serving when one of them crashes or is being upgraded.

Without anything in front, every client has to know those three addresses, pick one, notice when one dies, and
retry. Each app has to handle TLS certificates, authentication, rate limiting, and logging on its own. The machines'
private addresses are exposed to the internet. Deploying a new version means clients see connection failures.

### 1.2 The fix: one front door

A **reverse proxy** is a server that clients talk to *instead of* the real servers. It receives every request and
decides which real server should handle it, forwards the request there, and sends the answer back.

```
                         ┌──────────────► app #1  10.0.0.5
  clients ──► PROXY ─────┼──────────────► app #2  10.0.0.6
  (one address,          └──────────────► app #3  10.0.0.7
   api.example.com)       the proxy decides; clients never see these addresses
```

What the front door buys you:

| Job | What it means |
|---|---|
| **Routing** | `/api/orders/*` goes to the orders service, `/chat` to the chat service. One public address, many services. |
| **Load balancing** | Spread requests across the copies of a service. |
| **Health checking** | Stop sending traffic to a copy that's failing. |
| **TLS termination** | Handle HTTPS certificates in one place; talk plain HTTP (or re-encrypt) inside. |
| **Cross-cutting policy** | Authentication, rate limits, header rewriting, logging, metrics, in one place. |
| **Hiding the topology** | Internal addresses, ports and counts can change without clients knowing. |

**Forward vs. reverse proxy.** A *forward* proxy sits in front of **clients** (a corporate web proxy: "all our
employees' traffic goes out through here"). A *reverse* proxy sits in front of **servers**. YARP is a reverse proxy.
The name is from the servers' point of view: the proxy is "their" front door.

### 1.3 Where YARP fits in the family

| Product | What it is |
|---|---|
| nginx, HAProxy, Envoy | Stand-alone proxy programs, configured with their own config languages |
| Cloud load balancers (Azure Application Gateway, AWS ALB) | Managed proxies you rent |
| **YARP** ("Yet Another Reverse Proxy") | A **.NET library**. You build an ordinary ASP.NET Core app and add proxying to it. The proxy *is* your C# program, so you can customize anything in C#. |

That last row matters for everything below: **YARP runs inside an ASP.NET Core app.** Kestrel (ASP.NET Core's web
server) accepts the client's connection, ASP.NET Core's middleware pipeline processes the request, and YARP is the
last stage of that pipeline. That's why `sample/Proxy/Program.cs` in your repro is 10 lines long.

### 1.4 Layer 4 vs. layer 7 (why YARP can read your URL)

Proxies come in two broad kinds, named after network layers:

| | Layer 4 (transport) proxy | Layer 7 (application) proxy, like YARP |
|---|---|---|
| What it understands | TCP connections and bytes | HTTP: methods, paths, headers |
| Can route on | IP address and port | URL path, host name, headers, cookies |
| Unit of work | A TCP connection | An HTTP request |
| Examples | Azure Load Balancer, AWS NLB, iptables/NAT | YARP, nginx (HTTP mode), Envoy, ALB |

To route on a URL, a proxy must *read* the HTTP request, which means it must be the **endpoint** of the client's TCP
connection. It can't just nudge packets along. That single fact explains most of this lecture.

---

## 2. Layer check: sockets, connections, and the four numbers

You described a WebSocket as "four values: the port and IP address of both the client and the server". That's
very close, but it's the definition of something one layer down, and the difference matters here.

### 2.1 The vocabulary, precisely

| Term | What it is | Who holds it |
|---|---|---|
| **Socket** | One **endpoint**: the operating system's object representing *your end* of a network conversation. Your program sees it as a handle (a file descriptor on Linux/macOS) that it reads and writes bytes on. | One process (and the kernel behind it) |
| **TCP connection** | A reliable, ordered, two-way byte stream between **two sockets**. Identified by the **4-tuple**: *(local IP, local port, remote IP, remote port)*. | Both ends; each kernel keeps its own copy of the connection's state |
| **HTTP** | A request/response *conversation protocol* that runs over a TCP connection. | The programs at each end |
| **WebSocket** | A different conversation protocol that also runs over a TCP connection, reached by "upgrading" an HTTP request. After that, both sides exchange **frames**, whenever they like. | The programs at each end |

So: **the four numbers identify a TCP connection, not a WebSocket.** A WebSocket is a *way of talking* over one TCP
connection. It doesn't have an identity of its own beyond the connection it lives on.

Firmware analogy: the TCP connection is the wire (a UART link, set up and left up). HTTP and WebSocket are two
different protocols you can speak over that wire. The 4-tuple is the label on the wire.

### 2.2 The key consequence: there are two wires

When a Caller connects to a service through YARP, the Caller's TCP connection **ends at YARP**. The Caller's kernel
only knows the proxy's address. YARP then opens a **separate** TCP connection to the destination server.

Here are the actual two connections from your run 004 (from `sample/evidence/004-*-tap-client-proxy.txt` and the
harness settings; the tap adds one extra hop on each side, left out here):

```
 Connection #1: Caller ↔ YARP                       Connection #2: YARP ↔ Echo
 4-tuple: (127.0.0.1:64373, 127.0.0.1:5000)          4-tuple: (127.0.0.1:<YARP's ephemeral port>, 127.0.0.1:5050)

 Caller's socket ══════════════ YARP's socket A      YARP's socket B ══════════════ Echo's socket
 (owned by IdleClient)          (owned by YARP)      (owned by YARP)                (owned by EchoServer)
```

| Question you asked | Answer |
|---|---|
| Is the WebSocket between the client and YARP, or the client and the server? | **Physically, neither is one thing.** There are two TCP connections. *Logically*, the WebSocket conversation (the frames) is between the Caller and the Echo, and YARP relays it without understanding it. |
| Does the client ever connect to the server? | **No.** The client never learns the server's address. (That's one of the reasons proxies exist, §1.2.) |
| Does the server know the client's address? | Not from the TCP connection. It sees YARP's address. YARP adds `X-Forwarded-For` headers on the handshake request so the server can find out. |

### 2.3 Who "manages" a connection?

Nobody manages a connection from the outside. **Each end owns its own socket**, and each end decides for itself when
to close it:

| Layer | What's held | Who holds it |
|---|---|---|
| Kernel (TCP) | Sequence numbers, send/receive buffers, timers | The OS on each machine, one set per socket |
| Process | The handle, plus whatever the program allocated for this connection (buffers, a task that reads from it, per-connection objects) | The program that opened or accepted the socket |

YARP holds **two sockets per proxied WebSocket**, plus two 64 KB copy buffers, two running copy loops, and some
bookkeeping objects (§5). The Caller holds one socket, and so does the Echo. Each party is responsible for its own
resources, and nobody can force another party to release theirs. The only thing one side can do is **close its own
socket**, which the other side then notices as a FIN (graceful TCP close) or a reset.

This is the answer to "why is YARP the one closing things out?" (developed fully in §6): YARP closes *its own*
sockets, because they're *its* resources. The Echo is free to have its own policy too.

---

## 3. The cast of characters inside YARP

Here are the components a request meets, personified. Real type names are in the right-hand column so you can find
them in the source.

| Character | What they do | In the code |
|---|---|---|
| **The Doorman** | Kestrel. Accepts TCP connections on the proxy's port, parses HTTP, hands each request to the pipeline. Owns socket A. | ASP.NET Core Kestrel server |
| **The Map Reader** | Matches the request against the configured **routes** (path, host, headers, method) and attaches the matched route and cluster to the request. | ASP.NET Core endpoint routing + YARP's `ProxyPipelineInitializerMiddleware` |
| **The Dispatcher** | Picks one **destination** in the route's **cluster**, according to the load-balancing policy (default *PowerOfTwoChoices*: pick two at random, send to the less busy). Also applies session affinity and passive health. | `LoadBalancingMiddleware`, `SessionAffinityMiddleware`, `PassiveHealthCheckMiddleware` |
| **The Courier** | Builds the outgoing request and sends it to the destination over a pooled outgoing connection. Owns socket B. | `HttpForwarder` (`IHttpForwarder`) using an `HttpMessageInvoker` / `SocketsHttpHandler` |
| **The Two Pumps** | Only for upgraded connections (WebSockets). One copies Caller→Echo, the other Echo→Caller, byte for byte, without reading the contents. | `StreamCopier.CopyAsync`, called twice from `HttpForwarder.HandleUpgradedResponse` |
| **The Watchdog** | A timer that both Pumps kick every time they move bytes. If it expires, it cancels both Pumps. | `ActivityCancellationTokenSource`, timeout from the cluster's `HttpRequest.ActivityTimeout` (default 100 s, `HttpForwarder.DefaultTimeout`) |

### 3.1 Routes, clusters and destinations: the configuration vocabulary

Your sample's `Proxy/appsettings.json`, annotated:

```jsonc
"ReverseProxy": {
  "Routes": {
    "wsRoute": {                    // ROUTE: "which requests?"  (the Map Reader's job)
      "ClusterId": "echoCluster",   //   → send matches to this cluster
      "Match": { "Path": "/ws" }    //   matching rule
    }
  },
  "Clusters": {
    "echoCluster": {                // CLUSTER: "a group of interchangeable servers + how to talk to them"
      "HttpRequest": {              //   per-cluster outgoing-request settings (ForwarderRequestConfig)
        "ActivityTimeout": "00:01:40"   //   ← the Watchdog's setting lives HERE, on the cluster
      },
      "Destinations": {             // DESTINATIONS: the actual servers  (the Dispatcher chooses among them)
        "echo-destination": { "Address": "http://localhost:5050/" }
      }
    }
  }
}
```

| Concept | Question it answers | Example |
|---|---|---|
| **Route** | Which incoming requests does this rule catch? | "path `/ws`" |
| **Cluster** | Which group of servers handles them, and with what settings? | "echoCluster, 100 s activity timeout" |
| **Destination** | Which concrete server, at what address? | `http://localhost:5050/` |

You had "clusters map to routes"; it's the other way round: **many routes can point at one cluster**, and a cluster
contains destinations. Because `ActivityTimeout` is a *cluster* setting, the standard way to give WebSockets a
different timeout from ordinary API calls is to give the WebSocket route its own cluster (it can list the same
destinations).

---

## 4. Journey 1: an ordinary HTTP request (the "stateless" case)

### 4.1 Control flow

```
 Caller                 Doorman        Map Reader     Dispatcher        Courier                   Echo
   │ GET /api/x            │               │              │                │                        │
   │──────────────────────►│ parse         │              │                │                        │
   │                       │──────────────►│ route? →     │                │                        │
   │                       │               │ cluster ────►│ pick a         │                        │
   │                       │               │              │ destination ──►│ take a pooled          │
   │                       │               │              │                │ connection, send ─────►│
   │                       │               │              │                │◄──────── response ─────│
   │◄──────────────────────┼───────────────┼──────────────┼── copy back ───│ connection back        │
   │  response             │               │              │                │ to the pool            │
```

1. The Doorman parses the request off connection #1.
2. The Map Reader finds the route and its cluster.
3. The Dispatcher picks a destination **for this request**.
4. The Courier borrows an idle outgoing connection to that destination from its pool (or opens one), sends the
   request, and streams the response back.
5. When the response is finished, the outgoing connection goes back into the pool for the next request, which may
   be from a completely different Caller.

### 4.2 "Stateless" means requests, not connections

You said routing "implies that the requests are stateless". That's right, and it's worth being exact about what's
stateless:

- **HTTP requests are independent of each other.** Request 2 doesn't depend on which server handled request 1. So
  the Dispatcher can send each request to a different destination. Load balancing is decided **per request**
  (`ILoadBalancingPolicy.PickDestination(HttpContext, ...)`).
- **TCP connections are not stateless, and they're reused.** The Caller keeps connection #1 open across many
  requests (HTTP keep-alive). YARP keeps a pool of outgoing connections to each destination and reuses them for
  requests from many Callers.

So even for ordinary HTTP, YARP always sits in the middle of two sets of connections. The difference is that each
connection is shared by many short, independent requests, and nobody holds an outgoing connection for long.

> **The request is the unit of routing. The connection is the unit of transport.** Ordinary HTTP lets YARP decide
> per request and share connections. WebSockets break that.

---

## 5. Journey 2: a WebSocket (the stateful case)

### 5.1 The handshake is just an HTTP request

A WebSocket starts as an ordinary HTTP/1.1 GET with `Upgrade: websocket` headers (or, over HTTP/2, an extended
`CONNECT`). So it goes through exactly the same journey as §4: the Map Reader matches it, the Dispatcher picks a
destination (**once**), the Courier forwards it.

The destination answers `101 Switching Protocols`. YARP detects this in `HttpForwarder` (paraphrased from the
source):

```csharp
// HttpForwarder.cs: is this an upgraded connection?
if (destinationResponse.StatusCode == HttpStatusCode.SwitchingProtocols          // HTTP/1.1 upgrade
    || (destinationResponse.StatusCode == HttpStatusCode.OK                       // or HTTP/2 extended CONNECT
        && destinationResponse.Version == HttpVersion.Version20
        && destinationRequest.Method.Equals(HttpMethod.Connect) ...))
{
    await HandleUpgradedResponse(...);
}
```

### 5.2 After the 101: the conversation is no longer HTTP

From this moment, both TCP connections carry WebSocket frames, not HTTP. YARP doesn't parse frames. Instead,
`HandleUpgradedResponse` does four things (from the source):

```csharp
// 1. Turn off ASP.NET Core's per-request timeout: a WebSocket can legitimately last for hours.
context.Features.Get<IHttpRequestTimeoutFeature>()?.DisableTimeout();

// 2. Start the two Pumps, both kicking the SAME Watchdog.
var requestTask  = StreamCopier.CopyAsync(isRequest: true,  clientStream,      destinationStream, ..., activityCancellationSource, ...);
var responseTask = StreamCopier.CopyAsync(isRequest: false, destinationStream, clientStream,      ..., activityCancellationSource, ...);

// 3. Wait until either direction finishes (closed, failed, or cancelled).
var firstTask = await Task.WhenAny(requestTask, responseTask);

// 4. Classify what happened. If the Watchdog did it:
if (activityCancellationSource.IsCancellationRequested && !activityCancellationSource.CancelledByLinkedToken)
    error = ForwarderError.UpgradeActivityTimeout;   // ← the exact warning in your proxy logs
```

And each Pump is a simple loop (`StreamCopier.CopyAsync`, 64 KB buffer):

```
 loop:
   read  up to 64 KB from input       ← waits here while the connection is idle
   activityToken.ResetTimeout()       ← kick the Watchdog
   write those bytes to output
   activityToken.ResetTimeout()       ← kick it again
```

### 5.3 Why YARP can't "step out" after the handshake

This was your central confusion: *"if there's already a permanent connection between the client and the server, it
seems like YARP isn't even involved anymore."*

There is no connection between the client and the server to step out of (§2.2). The Caller's bytes arrive at
**YARP's** socket A, because that's the only socket with the Caller's 4-tuple. If YARP stopped reading socket A,
nobody else would. The Echo only receives what YARP writes to socket B. **YARP is not a switch that connects two
wires and walks away. It's the only thing connecting them, for the entire life of the WebSocket.**

```
 What you pictured:                          What actually happens:

 Caller ═══════════════════════ Echo          Caller ════ [YARP: socket A ⇄ Pump ⇄ socket B] ════ Echo
          (YARP set this up                             every byte, both directions, forever,
           and left)                                     passes through YARP's memory
```

Could a proxy step out? Only in ways that give up what a layer-7 proxy is for:

| Way out | What it costs |
|---|---|
| HTTP redirect (`307` to the server's real address) | The client connects directly, so the server's address is public and there's no load balancing, TLS termination or policy in the middle any more |
| Kernel-level splicing / a layer-4 load balancer | Still in the path (it forwards packets), just cheaper per byte. It can't route on URLs. |
| Direct server return (a special L4 trick where replies bypass the balancer) | Complex network setup; not available to an HTTP-level proxy |

### 5.4 How a stateless router carries a stateful connection

The routing decision (route → cluster → destination) is made **once**, on the handshake request, and then it's
**pinned** for the lifetime of the WebSocket: all later frames go through the same socket B to the same destination,
because that's the only place socket B leads. Nothing re-routes individual frames (YARP can't even see frame
boundaries).

Consequences worth knowing:

| Consequence | Why | What people do about it |
|---|---|---|
| **Load imbalance** | Balancing happened at connect time. If destination #1 got 10,000 long-lived sockets yesterday and #2 was added today, #2 gets none of the old ones. | Clients reconnect periodically; balance on connection count (`LeastRequests`) |
| **Deploys drop connections** | Restarting a destination or the proxy closes its sockets, so every WebSocket through it dies at once | Graceful drain; clients reconnect with backoff (SignalR does this) |
| **The proxy's resource cost scales with open sockets, not requests per second** | Every idle WebSocket still holds two sockets, buffers, and two waiting tasks in YARP | Idle timeouts (the Watchdog), capacity planning by concurrent connections |

---

## 6. Why the Gatekeeper hangs up (and not the server)

### 6.1 The problem the Watchdog solves: half-open connections

When a laptop lid closes or Wi-Fi drops, the Caller's machine just stops sending. No FIN, no reset, nothing. From
the other end, **a dead peer and a quiet peer look exactly the same**: no bytes arrive.

TCP itself only discovers that a peer is gone when it tries to *send* something and gets no acknowledgement (and
then only after retries, which can take many minutes). If nobody sends anything, a dead connection can sit there
**forever**. This is called a **half-open connection**. TCP has an optional keep-alive probe (`SO_KEEPALIVE`), but
it's off unless a program enables it, and the usual OS default waits **2 hours** before the first probe. (TCP
keep-alive probes are handled by the kernel and never reach YARP's code, which is why YARP's docs say they don't
reset `ActivityTimeout`.)

Now multiply by scale. A proxy fronting a chat service might have 100,000 WebSockets open. If 5% of those Callers
vanish every hour without closing, YARP accumulates 5,000 zombie connection pairs an hour, each holding two sockets,
two 64 KB buffers and two tasks, until it runs out of memory or file handles.

### 6.2 Each owner protects its own resources

Remember §2.3: each party owns its own sockets and can only close its own. So each party that holds connections
needs its **own** policy for detecting dead peers. They're independent, and they don't coordinate:

| Party | Its own "is this connection dead?" policy | Default |
|---|---|---|
| **YARP** | Activity timeout: no bytes in either direction for `ActivityTimeout` → cancel both Pumps, close both sockets | **100 s**, on |
| **ASP.NET Core server** (the Echo) | `WebSocketOptions.KeepAliveTimeout`: after sending a Ping, if no Pong comes back in time, abort | **Off** (infinite). Its `KeepAliveInterval` (2 min) only *sends* keep-alive frames; it doesn't police anything |
| **.NET `ClientWebSocket`** | Same pair of options on the client side | Sends every **30 s**; timeout off |
| **Browser** | No API to configure anything | Nothing; notices only when TCP fails |
| **Kernel (TCP)** | Retransmission failure; optional keep-alive probes | Probes off, 2 h if enabled |
| **Cloud load balancers, NAT routers** (in real deployments, not in your lab) | Their own idle timers | Often minutes (for example, AWS ALB 60 s and Azure Load Balancer 4 min by default; check your provider) |

So "wouldn't it be the server's responsibility?" — the server *may* police its own sockets, and a well-configured one
should (set `KeepAliveTimeout`). But the server's policy protects the **server's** memory, not YARP's. YARP can't rely on
every backend behind it being configured carefully, so it protects itself. In your lab, YARP's 100 s timer is simply the
**shortest active timer on the path**, so it's the one that fires first.

### 6.3 What "hanging up" looks like: an abort, not a goodbye

When the Watchdog fires, YARP cancels both Pumps and closes both sockets at the TCP level. It does **not** send a
WebSocket `Close` frame, because YARP doesn't speak the WebSocket protocol after the 101: it's copying opaque bytes.

| Graceful WebSocket close | YARP's activity-timeout abort (what you captured) |
|---|---|
| One side sends a `CLOSE` frame with a code, the other replies with `CLOSE`, then TCP closes | TCP FIN on both hops, no `CLOSE` frame (your taps in 004, 008) |
| .NET: `ReceiveAsync` returns `MessageType == Close` | .NET: `WebSocketException: ... closed the WebSocket connection without completing the close handshake` (client **and** Echo, run 004) |
| Browser: `close` event with the sender's code, `wasClean = true` | Browser: code **1006** ("abnormal closure"), `wasClean = false` (run 008) |
| YARP log: nothing special | YARP log: `warn ... HttpForwarder[48] UpgradeActivityTimeout` |

---

## 7. The Watchdog, up close

`ActivityCancellationTokenSource` (in `src/ReverseProxy/Utilities/`) is a `CancellationTokenSource` with three extras:

1. **It's a resettable timer.** `ResetTimeout()` calls `CancelAfter(timeout)` again, pushing the deadline forward.
   For performance it skips the reset if the last one was under ~20 ms ago (`TimeoutResolutionMs`), so on a busy
   connection it isn't resetting a timer for every packet.
2. **It's linked to other reasons to stop.** It's rented with `context.RequestAborted` (the Caller disconnected) and
   the forwarder's own cancellation token. If one of *those* fires, `CancelledByLinkedToken` is true, and YARP reports
   a different error (`UpgradeRequestCanceled` / `UpgradeResponseCanceled`), not `UpgradeActivityTimeout`.
3. **It's pooled.** Instances are rented and returned (up to 1,024 kept) to avoid allocating one per request.

The one fact that decides your whole lab: **both Pumps share a single Watchdog.** So bytes moving in *either*
direction reset it for the *whole* connection. That's why a server that sends and a Caller that never answers (run
009, Chrome) still keeps the connection alive.

```
        Pump → (Caller→Echo) ──kick──┐
                                     ├──► ONE Watchdog (100 s)  ── expires ──► cancel both Pumps
        Pump ← (Echo→Caller) ──kick──┘
```

When does the 100 s clock start? At the last successful read or write. In your run 004, the last bytes were the
`hello` echo at 0.94 s into the tap log, and both FINs appear at 100.99 s: 100.05 s later. The client's stopwatch
(started just after the echo) reads 100.1 s.

---

## 8. Keep-alives: who can kick the Watchdog, and how often

### 8.1 WebSocket frames, briefly

After the handshake, everything is a **frame**. Data frames carry your messages (text or binary). **Control frames**
manage the connection:

| Frame | Purpose | Rule (RFC 6455) |
|---|---|---|
| `Ping` | "Are you there?" | The receiver must answer with a `Pong` |
| `Pong` | The answer to a Ping, **or** an unsolicited one-way heartbeat | Never answered |
| `Close` | Start the goodbye handshake | Answered with a `Close` |

Frames from a client to a server are **masked** (XOR'd with a random key) and server-to-client frames aren't. That's
how your taps can tell which direction a frame came from (`PONG masked` = from the Caller in run 003).

WebSocket libraries send and absorb control frames automatically. **Your application code never sees them.** That's
why "keep-alive traffic" can keep the Watchdog quiet while both apps believe the connection is completely idle.

### 8.2 Who sends keep-alives by default

| Endpoint | Keep-alive setting | Default | Frame used | Source |
|---|---|---|---|---|
| .NET `ClientWebSocket` | `Options.KeepAliveInterval` (= `WebSocket.DefaultKeepAliveInterval`) | **30 s** | unsolicited `Pong` (a `Ping` if `KeepAliveTimeout` is set) | measured, run 002 |
| ASP.NET Core server `UseWebSockets` | `WebSocketOptions.KeepAliveInterval` | **2 min** | same | measured, run 002 |
| Browser JavaScript `WebSocket` | none: the API has no way to send a Ping or configure keep-alives | **never** | (browsers do answer Pings with Pongs automatically) | observed, run 008 |
| SignalR (ASP.NET Core real-time library) | `HubOptions.KeepAliveInterval` (server); the clients also send pings | 15 s | an application-level ping *message* | SignalR docs (not tested here) |

Two ways to keep a connection alive, then:

- **Protocol-level keep-alive:** Ping/Pong control frames, sent by the WebSocket library. Available to servers and
  non-browser clients.
- **Application-level heartbeat:** an ordinary data message your app defines (`{"type":"ping"}`), sent on a timer.
  Works from browsers, and passes through anything. SignalR does this for you, which is why most SignalR apps never
  hit #1764.

### 8.3 Is it "normally the client" that sends pings?

Either side may, and neither is special to YARP. What you may have picked up from concept notes Q3 is that *browser*
apps usually do their keep-alive **in the application** (heartbeat messages), because the browser API can't send
Pings. On the protocol level, **servers** are the ones that most commonly send keep-alive frames, because they're
the ones that can always do it.

**Client keep-alives are valid through YARP.** Your run 003 is the proof: the .NET client's Pong every 30 s kept the
connection open with the server at its defaults. The maintainer's line in #1764, *"These can be enabled on either
the client or server (not the proxy)"*, means exactly this: either endpoint works; YARP itself won't generate them.

### 8.4 The rule

```
   An idle proxied WebSocket survives
       ⇔
   min( keep-alive interval of every endpoint that sends one )  <  min( idle timeout of every hop on the path )
```

In your lab, the right-hand side is just YARP's `ActivityTimeout` (loopback, no load balancer, no NAT). In
production, it's the smallest of YARP's timeout, the load balancer's, any NAT's, and so on. "Keep-alives enabled" is
not a yes/no question; it's a comparison between two durations.

---

## 9. The rule, applied to your lab

Every run in the lab report is one row of this table. Nothing in it needs memorizing: each outcome follows from
§8.4.

| Run | Caller's keep-alive | Echo's keep-alive | Smallest | YARP timeout | Smallest < timeout? | Outcome |
|---|---|---|---|---|---|---|
| 003 | 30 s (.NET default) | 2 min (default) | 30 s | 100 s | yes | open at 310 s |
| 004 | off | 2 min (default) | 2 min | 100 s | **no** | aborted at 100.1 s |
| 005 | off | 30 s | 30 s | 100 s | yes | open at 310 s |
| 006 | off (no taps) | 2 min | 2 min | 100 s | **no** | aborted at 100.1 s (control) |
| 007 | 30 s (default) | 2 min | 30 s | **8 s** | **no** | aborted at 8.0 s |
| 008 | **none (browser)** | 2 min (default) | 2 min | 100 s | **no** | aborted at 100.1 s, code 1006 |
| 009 | none (browser) | 30 s | 30 s | 100 s | yes | open at 310 s |

Read it as three knobs and one comparison:

```
  KNOB 1: does the Caller send keep-alives, and how often?    (.NET: 30 s by default · browser: never)
  KNOB 2: does the Echo send keep-alives, and how often?      (ASP.NET Core: 2 min by default)
  KNOB 3: how long will YARP wait?                            (100 s by default)

  compare:  min(KNOB 1, KNOB 2)  vs  KNOB 3
```

And the two lessons the table encodes:

- **007 is why the early 8 s runs misled everyone:** shrinking only KNOB 3 made the .NET client's 30 s keep-alive
  "too slow" as well, so it looked as if defaults always die. A scale model has to scale every timer.
- **003 vs. 008 is the heart of #1764:** with server defaults, a .NET client survives (it brings its own keep-alive)
  and a browser dies (it can't). Real WebSocket users are usually browsers.

---

## 10. Which knob should you turn?

| Option | How | Good for | Cost |
|---|---|---|---|
| **Server keep-alive under the timeout** | `app.UseWebSockets(new() { KeepAliveInterval = TimeSpan.FromSeconds(30) })` | Browser clients; one setting protects every client | A few bytes per connection per interval |
| **Also detect dead peers on the server** | Add `KeepAliveTimeout = TimeSpan.FromSeconds(15)` (switches to Ping, expects a Pong) | Servers that want to free their own resources promptly | Slightly more traffic |
| **Application heartbeat from the client** | A timer in the JS app sends a small message; the server ignores or answers it | When you don't control the server; when the client also wants to detect a dead server and reconnect | Message volume at scale; mobile battery |
| **Raise YARP's `ActivityTimeout` for WebSocket routes** | Give the WebSocket route its own cluster with a longer `HttpRequest.ActivityTimeout` | When keep-alives aren't possible | Dead connections linger longer in the proxy (§6.1). Doesn't help with other hops' timers |
| **Use SignalR** | Built-in keep-alives, timeouts and reconnects | New real-time apps | A library and its protocol |

The rule of thumb: pick an interval comfortably under the smallest idle timer on the path (a third to a half of it),
so one delayed or lost keep-alive doesn't cost you the connection.

---

## 11. Edge cases, gotchas and common mistakes

### 11.1 Gotchas

| Gotcha | Detail |
|---|---|
| **The per-request timeout vs. the activity timeout** | ASP.NET Core's request timeouts (.NET 8+) are disabled for upgraded requests (`DisableTimeout()`); `ActivityTimeout` isn't. The WebSockets docs page mentions only the first. That gap is #1764. |
| **HTTP/2 pings don't count** | HTTP/2 has its own PING frames at the connection level. YARP's docs say they don't reset `ActivityTimeout`; only bytes read or written on the request's streams do. (Not tested in your lab, which used HTTP/1.1.) |
| **"Final client state: Aborted" in a surviving run** | Your harness stops the watch by cancelling `ReceiveAsync`, which aborts a `ClientWebSocket` by design. Read the line before it. |
| **A proxy restart kills every WebSocket** | Long-lived connections make deploys visible to users. Clients need reconnect logic regardless of keep-alives. |
| **Keep-alive is not liveness checking** | Sending a Pong every 30 s keeps *middle boxes* happy. It doesn't tell the sender whether the other end is alive. Only a Ping that expects a Pong (`KeepAliveTimeout`) or an app-level request/response does that. |

### 11.2 Common mistakes

| Mistake | Correct model |
|---|---|
| "The WebSocket is a connection between the client and the server" | Two TCP connections; the proxy owns one end of each (§2.2) |
| "After the handshake, the proxy is out of the picture" | The proxy is in the data path for the socket's whole life (§5.3) |
| "The server should close idle connections, not the proxy" | Every owner of sockets polices its own; the shortest active timer fires first (§6.2) |
| "Keep-alives are on, so we're fine" | Compare the interval with the smallest idle timeout on the path (§8.4) |
| "Only the client (or only the server) can keep it alive" | Any byte from either side resets the shared Watchdog (§7) |
| "Shrink one timeout to test faster" | Scale every timer, or check which one wins (§9, run 007) |

---

## 12. Interview relevance

- **"Our WebSocket connections drop after about two minutes behind the load balancer. Why?"** Walk the path, list
  every hop's idle timer, find the smallest, and compare it with the keep-alive interval of whichever endpoint sends
  one. Mention that browsers can't send protocol pings, so it's the server's interval or an app heartbeat.
- **"What's the difference between an L4 and an L7 load balancer?"** Connections vs. requests; what each can route
  on; why an L7 proxy terminates the client's connection (§1.4).
- **"How would you load-balance long-lived connections?"** Balancing happens at connect time and is pinned; discuss
  imbalance, connection-count-based policies, periodic reconnects, graceful drain on deploy (§5.4).
- **"How do you detect a dead client?"** Half-open connections, why TCP doesn't notice without sending, TCP
  keep-alive defaults, application-level heartbeats with timeouts (§6.1, §11.1).
- **"Graceful close vs. abort"** Close handshake vs. TCP teardown, and how each looks to .NET and to browsers (§6.3).
- **Design sense:** a proxy protecting itself with an activity timeout is a general pattern: any intermediary that
  holds resources on behalf of others needs a policy for reclaiming them from peers that silently vanish.

---

## 13. Real-world production usage

- **Chat, notifications, live dashboards, collaborative editors, multiplayer games** all hold WebSockets open for
  long idle periods. Every one of them, behind YARP or any cloud load balancer, needs keep-alives tuned to the path.
- **SignalR** (ASP.NET Core) sends keep-alive messages every 15 s by default and has client reconnect logic, so most
  SignalR apps are unaffected by #1764. Raw `UseWebSockets` apps with browser clients are the ones that get bitten.
- **dotnet/yarp#2615** (2024) is a real report: *"YARP keep terminating the WebSocket after around 2 minutes"*, error
  `UpgradeActivityTimeout`, fixed by raising `ActivityTimeout`. That's the scenario the #1764 docs change is for.
- **Kubernetes / cloud** deployments often have *three* idle timers in a row (cloud load balancer → ingress proxy →
  sidecar), each with different defaults. The rule in §8.4 is how you reason about all of them.

---

## 14. Check yourself

Try each one before reading the answer.

1. A browser connects to `wss://chat.example.com/ws` through YARP. How many TCP connections exist, and whose
   addresses are in each 4-tuple?
   *Two: (browser IP:port, YARP's public IP:443) and (YARP's IP:ephemeral port, destination IP:port).*
2. The Dispatcher uses RoundRobin across three destinations. A WebSocket is established, then 50 frames are sent.
   How many load-balancing decisions were made?
   *One, for the handshake request. Frames aren't routed.*
3. The Echo sends a Pong every 30 s; the browser never sends anything. Why doesn't YARP time out?
   *Both Pumps share one Watchdog; the Echo→Caller Pump kicks it every 30 s.*
4. You set the Echo's `KeepAliveInterval` to 90 s. YARP is at the default. There's also an AWS ALB with its default
   idle timeout in front of YARP. Does an idle browser connection survive?
   *No. The smallest idle timer is the ALB's 60 s, and 90 s is longer. Keep-alives have to beat every hop's timer.*
5. Why does your YARP log say `UpgradeActivityTimeout` and not `UpgradeRequestCanceled` when the Watchdog fires?
   *The Watchdog cancelled on its own timer, not because a linked token (like `RequestAborted`) fired, so
   `CancelledByLinkedToken` is false.*
6. Why is there no WebSocket `Close` frame in your 004 tap logs?
   *YARP doesn't speak the WebSocket protocol after the 101; it closes the TCP connections directly.*
7. Why did the early 8 s-override runs make "defaults on both sides" look fatal, when at 100 s it isn't for a .NET
   client?
   *At 8 s, the client's 30 s keep-alive is also too slow. Only one timer was scaled.*

---

## References

**YARP source (`dotnet/yarp`, `main`):**
- [`src/ReverseProxy/Forwarder/HttpForwarder.cs`](https://github.com/dotnet/yarp/blob/main/src/ReverseProxy/Forwarder/HttpForwarder.cs): `DefaultTimeout` (100 s), `HandleUpgradedResponse`, the two `StreamCopier.CopyAsync` calls, `Task.WhenAny`, `UpgradeActivityTimeout` classification, `DisableTimeout()`
- [`src/ReverseProxy/Forwarder/StreamCopier.cs`](https://github.com/dotnet/yarp/blob/main/src/ReverseProxy/Forwarder/StreamCopier.cs): the copy loop, 64 KB buffer, `ResetTimeout()` after each read and write
- [`src/ReverseProxy/Utilities/ActivityCancellationTokenSource.cs`](https://github.com/dotnet/yarp/blob/main/src/ReverseProxy/Utilities/ActivityCancellationTokenSource.cs): the Watchdog
- [`src/ReverseProxy/Forwarder/ForwarderRequestConfig.cs`](https://github.com/dotnet/yarp/blob/main/src/ReverseProxy/Forwarder/ForwarderRequestConfig.cs): `ActivityTimeout` and its doc comment
- [`src/ReverseProxy/Forwarder/ForwarderError.cs`](https://github.com/dotnet/yarp/blob/main/src/ReverseProxy/Forwarder/ForwarderError.cs): the `Upgrade*` error values
- [`src/ReverseProxy/Routing/ReverseProxyIEndpointRouteBuilderExtensions.cs`](https://github.com/dotnet/yarp/blob/main/src/ReverseProxy/Routing/ReverseProxyIEndpointRouteBuilderExtensions.cs): the default pipeline (session affinity → load balancing → passive health → limits → forwarder)

**Docs:**
- [YARP Timeouts](https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/timeouts) · [YARP WebSockets](https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/websockets) · [YARP load balancing](https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/load-balancing)
- [`WebSocketOptions.KeepAliveInterval`](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.builder.websocketoptions.keepaliveinterval) · [`WebSocket.DefaultKeepAliveInterval`](https://learn.microsoft.com/dotnet/api/system.net.websockets.websocket.defaultkeepaliveinterval) · [SignalR configuration](https://learn.microsoft.com/aspnet/core/signalr/configuration)
- [RFC 6455](https://www.rfc-editor.org/rfc/rfc6455.html) (WebSocket protocol: §5.2 framing and masking, §5.5 control frames)

**This repo:**
- Lab report: [`../1764_websocket_idle_timeout/implementations/001-lab-report-idle-websockets-through-yarp.md`](../1764_websocket_idle_timeout/implementations/001-lab-report-idle-websockets-through-yarp.md)
- Evidence: [`../1764_websocket_idle_timeout/sample/evidence/`](../1764_websocket_idle_timeout/sample/evidence/)
- Earlier, narrower lecture: [`../../lectures/001-yarp-websocket-activity-timeout.md`](../../lectures/001-yarp-websocket-activity-timeout.md)
