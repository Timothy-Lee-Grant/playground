# Concept Notes 001 — Questions From the Issue Shortlist

> **What this is:** a running Q&A log of *concept notes*. As I read `scouting/001-issue_shortlist_sept_2026.md` (at the repo root), I ask about things I don't understand, and each answer is **appended** here, newest at the bottom.
> **Style:** short and issue-focused. Just enough concept to unblock the next step, then concrete next steps. Not a full lecture; full, in-depth lectures belong in the repo-root `lectures/` folder.
> **Started:** 2026-09-26
>
> **Format for each entry:** the question (in my words) → the short answer → the explanation → what to take away / next steps.

---

## Index

| # | Question | Related issue | Topic |
|---|---|---|---|
| Q1 | Why do maintainers open an issue for a simple docs fix instead of just doing it? | YARP #1764 (item A) | How open-source projects actually run |
| Q2 | I found `src/TelemetryConsumption/WebSockets/` in YARP, but it's all C#. Where are the docs, and what am I misunderstanding? | YARP #1764 (item A) | Code vs. docs repos; finding a doc's source; verifying before contributing |
| Q3 | What are sockets and WebSockets, what does YARP do with them, what is the proxy timeout, and how do keep-alives and browser heartbeats fix it? | YARP #1764 (item A) | Sockets, WebSocket handshake, proxy byte-pumping, idle timeouts, keep-alives |
| Q4 | Getting my bearings: where does #1764 stand, what evidence do I still need, and what do I do with it? | YARP #1764 (item A) | Status check, evidence plan, comment + PR plan (tracker continued in Q5) |
| Q5 | My E1 run stayed open past 300 s with defaults on both sides. Did I collect it wrong? | YARP #1764 (item A) | **Correction to Q3/Q4 (C3)**: the .NET client's own 30 s keep-alive; browsers are the real failure case (tracker continued in Q6) |
| Q6 | Checking my mental model (sockets, who owns the connection, why YARP hangs up), are the tests good enough, and how do I close this out? | YARP #1764 (item A) | Model check, evidence verdict, close-out plan (**the current progress tracker**) |

---

## Q1. Why open an issue for a simple docs fix instead of just doing it?

**Related:** [YARP #1764](https://github.com/dotnet/yarp/issues/1764), item A in the shortlist · **Asked:** 2026-09-26

### The question

In YARP #1764, the maintainer already explained exactly what the docs should say (the 100-second activity timeout, and that keep-alives must come from the client or server). It's a small documentation change. So why open an issue, describe it in detail, and then leave it for years instead of spending a few minutes to just fix it, especially when Microsoft engineers have AI and powerful internal tools? I'd understand leaving a hard bug open, or something needing special hardware, but this seems like it should take less effort to fix than to write up.

### The short answer

Because **the scarce resource isn't typing. It's a maintainer's attention**, and an issue is the cheapest way to *record* knowledge without *spending* that attention right now. The explanation in the issue took a couple of minutes, as part of answering a user. Turning it into a merged docs change takes a context switch, a different repo, a review, and a publish cycle. That's small in absolute terms, but it competes with bugs, security fixes, and releases, and it **always loses that competition**. Leaving it labeled `help wanted` also serves a second purpose: it's a deliberate entry point for new contributors like you.

### The full explanation

#### 1. The issue is a "bottom half," not a to-do list

Here's a firmware analogy that fits almost exactly.

```
 Firmware                                   Open-source maintainer
 ────────                                   ──────────────────────
 An interrupt fires                    ≈    A user reports "my WebSocket keeps dropping"
 The ISR (top half) does the minimum:  ≈    The maintainer answers the user in 2 minutes:
   acknowledge, capture state, return         "100 s activity timeout; use keep-alives"
 The rest is deferred to a             ≈    The leftover work ("the docs should say this")
   bottom half / work queue                   is deferred to an issue, labeled and queued
 The scheduler runs deferred work      ≈    Someone picks up the issue when priorities
   when higher priorities allow                allow: a maintainer, or the community
```

The maintainer handled the **urgent** part (unblock the user) and **deferred** the non-urgent part (improve the docs) so it wouldn't be forgotten. Writing the issue wasn't "more work than fixing it." It was the cheapest way to **save the knowledge** while going back to higher-priority work.

#### 2. A priority queue without aging starves low-priority work

YARP has a small core team whose members also own other parts of ASP.NET Core networking. Their queue always contains security issues, regressions, release work, customer escalations, and design reviews for new features. A docs clarification is real but **low severity**: nobody's production is down because of it.

In a scheduler with strict priorities and no **aging** (bumping a task's priority the longer it waits), low-priority tasks can wait forever. That's exactly how an issue like #1764 sits in the `Backlog` milestone for years. Nobody decided "we won't do this." It just never rose to the top. The `help wanted` label is the maintainers' way of saying: *"We won't get to this soon, and we'd welcome someone else doing it."*

#### 3. "Simple" changes aren't free for a maintainer

For you, a first docs PR is a learning exercise. For a maintainer, even a small change has fixed costs:

| Cost | For #1764 specifically |
|---|---|
| **Context switch** | Stop current work, reload the docs structure, find the right page |
| **Different repo, different process** | YARP's docs moved into `dotnet/AspNetCore.Docs`, which has its own style guide, PR template, and reviewers from the docs team |
| **Getting it right** | Correct property names, correct version behavior, a code sample that actually works. Docs are read by thousands of people, so a wrong statement is expensive. |
| **Review and publishing** | Someone must review it; the docs build must pass; it must be published |

Maybe 30–90 minutes in total. That's small, but it's 30–90 minutes that isn't spent on a regression, and there are dozens of these in the backlog.

**Ownership diffusion makes it worse.** When the docs moved to a different repository, "who owns this change?" got fuzzier. The YARP engineers own the knowledge, while the docs repo has different owners. Small tasks that fall between two owners are the ones most likely to sit untouched. This happens inside every large company, not just in open source.

#### 4. `help wanted` issues are deliberately left for newcomers

This is the part that's easiest to miss. Healthy projects **intentionally keep a supply of small, well-described tasks** for new contributors:

- **They're the on-ramp.** A newcomer can't start with a subtle proxy bug. They need a task where the answer is known, so they can learn the process (fork, CLA, review, merge) with low risk.
- **Contributors are a long-term investment.** Every person who lands a first docs PR might become a regular, and regulars eventually take real work off the maintainers. That only happens if easy entry points exist. If maintainers fixed every easy thing themselves, the pipeline would dry up.
- **A detailed description is the point.** The maintainer wrote exactly what the docs should say *so that someone without insider knowledge can do it correctly*. That's a well-written ticket, not wasted effort.

So when you see a small, fully-explained `help wanted` issue, read it as: **"This one was left here on purpose, for someone like you."**

#### 5. Why AI and Microsoft's internal tools don't change this

The bottleneck was never producing the text. It's:

- **Deciding** it's worth doing now, over everything else (attention and priority)
- **Verifying** it's correct (judgment)
- **Reviewing and owning** the change after it ships (accountability)

AI speeds up the writing, but a human maintainer still has to decide, verify, review, and own it. Meanwhile, AI has *increased* the review burden across open source: maintainers now receive many low-effort, AI-generated PRs (Hacktoberfest stopped counting PRs in 2026 for exactly this reason). A careful human contribution that's verified and correct is more valuable to maintainers now, not less.

#### 6. To be fair: sometimes it really is just forgotten

Not every old issue is a deliberate on-ramp. Some are simply forgotten, and a maintainer might fix one in five minutes the day someone mentions it. That's fine too. Commenting "I'd like to take this" either gets you the task or prompts the maintainer to close it. Either way, the backlog gets smaller and you've made useful contact with the project.

### What to take away

- An issue is a **cheap way to save knowledge** without spending attention right now. It's a deferred-work queue, not a failure to act.
- Small, low-severity work **starves** in a strict priority queue. That's why good starter issues can stay open for years.
- `help wanted` + a detailed explanation = **an intentional on-ramp**. You taking it is exactly what the maintainer hoped for.
- The cost of software work is mostly **deciding, verifying, reviewing, and owning**, not typing. AI doesn't remove those costs.
- **Implication for item A:** the fact that the fix is already described isn't a reason to skip it. It's the reason it's a good first contribution. Your value is doing it correctly (right page, right property names, verified behavior) so the maintainer only has to review.

### Interview relevance

This question is really about **prioritization and the true cost of work**, which comes up in behavioral and design interviews ("How do you decide what to work on?", "How do you handle a backlog?"). Useful vocabulary:
- **Opportunity cost:** time spent on X isn't spent on something more important.
- **Context-switch cost:** the fixed overhead of starting any task, independent of its size.
- **Triage:** sorting incoming work by severity and impact.
- **Ownership:** a task with no clear owner tends not to get done.
- **Delegation / growing others:** leaving well-scoped work for less-experienced people is a senior-engineer behavior, not laziness. It's how teams scale.

---

## Q2. Where do the WebSockets docs actually live? (I found a WebSockets folder, but it's all C# code)

**Related:** [YARP #1764](https://github.com/dotnet/yarp/issues/1764), item A · **Asked:** 2026-09-26

### The question

Looking for the WebSockets documentation to fix for item A, I found `yarp/src/TelemetryConsumption/WebSockets/` in my local YARP clone. It only contains C# code, no documentation. Big projects seem to have several folders with the same component names, so I suspect I'm misunderstanding something. Where should I be looking, and what exactly should I do?

### The short answer

You're in the right *project* but the wrong *repository*. **YARP's documentation isn't in the YARP repo at all.** It lives in a separate repository, [`dotnet/AspNetCore.Docs`](https://github.com/dotnet/AspNetCore.Docs), at `aspnetcore/fundamentals/servers/yarp/websockets.md`, and it's published to [learn.microsoft.com](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/websockets). The folder you found is a **library for observing telemetry** about WebSocket connections. Interestingly, it contains code evidence of the exact behavior the doc should describe (see below).

And one new finding, from re-checking the docs while answering this: **most of what #1764 asks for has already been documented**, on the Timeouts page. That changes what item A should be (see "What to do now").

### What you were missing: one feature, many places

In a large project, one feature ("WebSockets") shows up in several places, each doing a different job. They share a name because they're about the same feature, not because they're the same kind of thing:

```
 Feature: "YARP proxies WebSockets"
 │
 ├── PRODUCT CODE         dotnet/yarp  → src/ReverseProxy/...            The proxy itself: forwards the
 │                                                                       HTTP/1.1 Upgrade or HTTP/2 CONNECT,
 │                                                                       then pumps bytes both ways
 │
 ├── TELEMETRY LIBRARY    dotnet/yarp  → src/TelemetryConsumption/       A separate NuGet package
 │                                       WebSockets/   ← you were here   (Yarp.Telemetry.Consumption) that
 │                                                                       lets an app OBSERVE what the proxy did
 │
 ├── TESTS                dotnet/yarp  → test/...                        Proves the behavior
 ├── SAMPLES              dotnet/yarp  → samples/...                     Example apps
 │
 └── DOCUMENTATION        dotnet/AspNetCore.Docs (a DIFFERENT repo)      What users read on learn.microsoft.com
                          → aspnetcore/fundamentals/servers/yarp/
                               websockets.md    ← the page item A was about
                               timeouts.md      ← where ActivityTimeout is documented
```

**The rule to remember:** when a project's docs are published on learn.microsoft.com, the source is usually in a separate `*Docs` repo owned partly by a docs team. YARP's docs used to live inside the YARP repo and were **migrated to AspNetCore.Docs** ([tracking issue #34650](https://github.com/dotnet/AspNetCore.Docs/issues/34650)), which is why older guides and blog posts may point elsewhere.

**The trick that always works:** open the published page on learn.microsoft.com and use its **edit (pencil) link**. It takes you straight to the exact source file on GitHub. That works for any Microsoft Learn page, so you never have to guess which repo or folder.

### The folder you found is still interesting

`src/TelemetryConsumption/WebSockets/` is the **Yarp.Telemetry.Consumption** library. YARP emits runtime telemetry events, and this library lets an application subscribe to them. In that folder is this enum:

```csharp
/// <summary>
/// The reason the WebSocket connection closed.
/// </summary>
public enum WebSocketCloseReason : int
{
    Unknown,
    ClientGracefulClose,
    ServerGracefulClose,
    ClientDisconnect,
    ServerDisconnect,
    ActivityTimeout,     // ← the proxy closed an idle WebSocket
}
```

`ActivityTimeout` is exactly the behavior issue #1764 is about: the proxy closing an idle WebSocket after 100 seconds. So the telemetry library lets an operator *see* when the problem happens. The code, the telemetry, and the docs are three views of one behavior. Connecting them like this is how you build a real map of a codebase. (Firmware analogy: the product code is the peripheral, the telemetry library is the status register that tells you why the link dropped, and the docs are the datasheet.)

### The finding: the Timeouts page already covers it

Here's what the current docs say (checked 2026-09-26):

| Page (in AspNetCore.Docs) | Last updated | What it says about WebSockets |
|---|---|---|
| `yarp/timeouts.md` ([published](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/timeouts)) | `ms.date: 11/01/2025` | Has a **WebSockets section**: request timeouts are disabled after the handshake, but "`ActivityTimeout` does apply to WebSocket requests. WebSocket keep-alives can be enabled by either the client or server talking to the proxy to keep the connection from becoming idle." Also notes that **WebSocket pings reset the timeout**, while TCP keep-alives and HTTP/2 pings don't. Documents the 100 s default and the per-cluster `HttpRequest.ActivityTimeout` setting. |
| `yarp/websockets.md` ([published](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/websockets)) | `ms.date: 2/6/2025` | Its "Timeout" section only says HTTP request timeouts are disabled after the handshake, then links to Timeouts. **It doesn't mention `ActivityTimeout` or keep-alives.** |

So the core knowledge from #1764 **is now documented**, just not on the page a WebSocket user would read first. Meanwhile the issue is still open, probably because nobody connected the new Timeouts content back to it. (That's Q1's point about ownership falling between two repos, happening in real life.)

**Lesson:** always re-read the *current* docs and code before starting, even on a well-described issue. The shortlist was written from the issue text, and the issue text was out of date.

### What to do now (the revised item A)

This is now a **triage + tiny docs PR**. Both are real contributions, and it's even lower risk than before.

**Step 1: read both pages yourself (10 min).** Open the two published pages above and confirm the table. Click each page's edit link to see the source Markdown on GitHub.

**Step 2: comment on YARP #1764.** Something like:

> Hi! I'd like to help close this out. It looks like the core of this is now documented in the [Timeouts page's WebSockets section](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/timeouts) (ActivityTimeout applies to WebSockets; keep-alives from client or server; WebSocket pings reset the timeout). However, the [WebSockets page's Timeout section](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/websockets) only mentions HTTP request timeouts. Would a short addition there (ActivityTimeout still applies after the handshake, default 100 s, enable keep-alives, link to Timeouts#websockets) resolve this? If so I'm happy to open that PR in dotnet/AspNetCore.Docs. Otherwise, perhaps this can be closed as covered.

**Step 3: if they say yes, make the PR without cloning anything.** AspNetCore.Docs is a very large repo, and your MacBook's disk is tight, so use GitHub's web editor:
1. Go to `aspnetcore/fundamentals/servers/yarp/websockets.md` in `dotnet/AspNetCore.Docs` on GitHub and click the pencil ("Edit this file"). GitHub creates a fork and branch for you automatically.
2. In the "Timeout" section, add a sentence or two along these lines (adjust wording to the page's style):

   > Request timeouts are disabled after a WebSocket handshake, but the cluster's `ActivityTimeout` (default 100 seconds) still applies. To keep idle WebSocket connections open, enable WebSocket keep-alives on the client or the destination server. For details, see [Timeouts](xref:fundamentals/servers/yarp/timeouts#websockets).

   *Check the `uid` in `timeouts.md`'s front matter to get the xref right, and look at how other links on the page are written. Match that style.*
3. Read the repo's contributor guidance for AspNetCore.Docs (linked from its README / the PR template). Among other things, it covers whether to update `ms.date`.
4. Open the PR with a description that says what and why, and links `dotnet/yarp#1764`.

**Step 4: if they say "already covered, closing,"** that's still a successful contribution: you triaged a stale issue and got it resolved.

**Optional hands-on (strengthens the comment or PR):** in your YARP clone, look in `samples/` for a WebSocket example (or write a minimal proxy plus an echo server), leave a socket idle for more than 100 seconds, and watch it close. Then enable `KeepAliveInterval` on the server and watch it survive. If you also wire up `Yarp.Telemetry.Consumption`, you can watch `WebSocketCloseReason.ActivityTimeout` get reported. That uses all three layers from the diagram.

### What to take away

- **One feature, many locations:** product code, telemetry, tests, samples, and docs are separate "characters" that share a feature name. Ask "which *job* am I looking for?" before searching by name.
- **Microsoft Learn docs usually live in a separate `*Docs` repo.** Use the page's edit link to find the source.
- **Verify the current state before you start.** Issues describe the world when they were written. This one had been partly overtaken by later docs changes.
- **Triage is a contribution.** Showing maintainers "this is already covered, here's the remaining gap" saves them time and often closes an issue.
- **Constrained machine?** The GitHub web editor is a legitimate way to make docs PRs.

### Interview relevance

"How do you get up to speed in an unfamiliar codebase?" This is a concrete answer: separate the product code from the telemetry, tests, and docs; find the published artifact and trace it back to its source; verify the current state before changing anything. The ActivityTimeout thread (docs ↔ telemetry enum ↔ proxy behavior) is also a nice example of connecting layers, the top-down thinking design interviews look for.

---

## Q3. Sockets, WebSockets, YARP's role, the idle timeout, and keep-alives

**Related:** [YARP #1764](https://github.com/dotnet/yarp/issues/1764), item A · **Asked:** 2026-09-26

### The question

I'm weak on sockets and WebSockets: what they are, how the connection is established (the handshake), and why they work the way they do. Where does YARP fit in, and what exactly is a "proxy timeout"? My reading of the fix: the **destination server** is the ASP.NET Core app YARP routes to, and it has a `KeepAliveInterval` setting in `WebSocketOptions` that prevents the timeout. And what are "application-level heartbeats" in browsers? How do they work, why are they the usual approach, and what do they cost?

### The short answer

- A **socket** is the OS's handle to one end of a network connection (think: a file descriptor you can read and write bytes on). A **TCP connection** is a reliable two-way byte pipe between two sockets.
- Plain **HTTP** uses that pipe for **request → response**, and the server can't speak unless asked. A **WebSocket** starts as an HTTP request that asks to **"upgrade"**. After the server agrees, the same TCP connection becomes a **full-duplex message channel** where either side can send at any time. That's what chat, live dashboards, and notifications need.
- **YARP** is a **reverse proxy**: clients connect to YARP, and YARP opens its own connection to a **destination** (backend) server. For a WebSocket, YARP forwards the upgrade handshake, then just **pumps bytes in both directions** between the two connections.
- The **activity timeout** (default **100 s**) is YARP's rule: "if no bytes move in either direction for 100 s, close both connections." It protects the proxy from holding dead connections forever.
- **Your understanding of the fix is correct**: the destination server is the ASP.NET Core app behind YARP, and `WebSocketOptions.KeepAliveInterval` makes it send small control frames periodically, so the connection is never idle for 100 s. **One gotcha:** the default `KeepAliveInterval` is **2 minutes**, which is *longer* than YARP's 100 s, so with defaults the proxy still cuts idle sockets. You have to set it below 100 s.
- **Browser JavaScript can't send WebSocket ping frames** (the browser API doesn't expose them), so browser apps send their own tiny "heartbeat" messages on a timer. Libraries like SignalR do this for you.

### The explanation

#### 1. The cast of characters

```
  Browser / client app            YARP (reverse proxy)                Destination server
  ────────────────────            ────────────────────                ──────────────────
  socket A ══ TCP conn #1 ══► socket B      socket C ══ TCP conn #2 ══► socket D
                               (YARP accepts)  (YARP connects)        (your ASP.NET Core app)

  Two separate TCP connections. YARP sits in the middle and relays.
```

| Character | Job |
|---|---|
| **Socket** | The OS's endpoint object for one side of a connection. Your code reads/writes bytes on it; the kernel handles TCP. |
| **TCP connection** | Reliable, ordered byte stream between two sockets. Opened with a handshake (SYN, SYN-ACK, ACK) and stays open until one side closes it. |
| **HTTP** | A conversation *protocol on top of* TCP: the client sends a request, the server sends a response. The server never speaks first. |
| **WebSocket** | A different protocol on top of TCP, entered **via** an HTTP request. Once established, it's message frames both ways, anytime. |
| **YARP** | A reverse proxy: accepts client connections, picks a destination per its routes/clusters, and forwards traffic. |
| **Destination server** | The backend app YARP forwards to. In #1764's scenario, an ASP.NET Core app using WebSockets. |

**Firmware analogy:** a TCP connection is like a UART link that's been set up and stays up. HTTP is a strict **master–slave** protocol on that link (like I2C: the controller always initiates). A WebSocket switches the link to **full-duplex messaging**, where either side can transmit whenever it wants. YARP is a **bridge/repeater** between two links.

#### 2. How a WebSocket is established (the handshake)

It begins as an ordinary HTTP/1.1 request with special headers:

```
Client → Server:
  GET /chat HTTP/1.1
  Host: example.com
  Upgrade: websocket                 ← "I want to switch protocols"
  Connection: Upgrade
  Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==     ← random nonce
  Sec-WebSocket-Version: 13

Server → Client:
  HTTP/1.1 101 Switching Protocols   ← "agreed"
  Upgrade: websocket
  Connection: Upgrade
  Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=   ← proves the server understood the request
```

After `101`, **HTTP is over on that connection.** The same TCP connection now carries WebSocket **frames**: text/binary data frames, plus **control frames**: `Ping`, `Pong`, and `Close`. (Over HTTP/2 the start is slightly different, an extended `CONNECT`, but the idea is the same. YARP's WebSockets doc covers both.)

**Why start with HTTP?** So WebSockets work through the same ports (80/443), firewalls, TLS, and proxies as normal web traffic. It's a way into full-duplex communication without needing new infrastructure.

#### 3. What YARP does with a WebSocket

1. The client's upgrade request arrives at YARP like any HTTP request, and YARP's routing picks a destination.
2. YARP forwards the upgrade request to the destination.
3. The destination answers `101`, and YARP passes it back to the client.
4. From then on, YARP **doesn't interpret the traffic.** It copies bytes from client→destination and destination→client until one side closes.

So for the life of a WebSocket, YARP is holding **two TCP connections, buffers, and a running copy task**. That's cheap for one socket, but real for 100,000.

#### 4. The activity timeout: why it exists

A connection can die **silently**: a laptop lid closes, Wi-Fi drops, a phone switches networks. No `Close` frame is sent, and TCP won't notice on its own for a very long time. From YARP's side, a dead connection and a quiet-but-alive one look identical: no bytes moving.

YARP's answer is a **watchdog**: `ActivityTimeout` (default **100 seconds**, configured per cluster under `HttpRequest.ActivityTimeout`). Any successful read/write (including WebSocket ping/pong frames) **resets** the timer. If it expires, YARP closes both connections and frees the resources. (Plain TCP keep-alives don't reset it, because they never reach YARP's code.)

**Firmware analogy:** exactly a watchdog timer. The healthy system must "kick" it periodically, and silence is treated as failure. The keep-alive frame is the kick.

The side effect is the problem in #1764: a **legitimately idle** WebSocket (e.g., a chat app where nobody types for two minutes) gets killed too. The fix is to make sure *something* kicks the watchdog.

#### 5. Fix 1: server-side keep-alives (your reading, confirmed)

In the destination server (ASP.NET Core):

```csharp
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)   // default is 2 minutes, which is LONGER than YARP's 100 s!
});
```

The server now sends a small control frame every 30 s. It passes through YARP, which counts it as activity, so the watchdog never fires. Browsers answer server `Ping` frames automatically at the protocol level, and no JavaScript is involved. Newer ASP.NET Core versions also have `KeepAliveTimeout`: after sending a ping, if no pong comes back in time, the *server* aborts the connection. That gives the server its own dead-peer detection.

Alternatively, raise YARP's `ActivityTimeout` for that route's cluster (e.g., 10 minutes). That's simpler, but it also lets dead connections linger longer. It's a trade-off between resource cleanup and tolerance for idle connections.

#### 6. Fix 2: application-level heartbeats (the browser side)

The browser's JavaScript `WebSocket` API can `send()` data messages and `close()`, but it **can't send `Ping` frames**. So when the *client* needs to keep the connection alive or detect that the server vanished, the application does it itself:

```js
// Browser: send a tiny app-defined message every 30 s
const ws = new WebSocket("wss://example.com/chat");
setInterval(() => ws.send(JSON.stringify({ type: "ping" })), 30_000);
// Server code recognizes {type:"ping"} and may reply {type:"pong"}.
// If no pong arrives within N seconds, the client closes and reconnects.
```

It's "application-level" because it's an ordinary data message whose meaning the *app* defines, not a protocol control frame.

**Why it's the usual approach:** it works regardless of what's in between (YARP, load balancers, corporate proxies, NAT routers with their own idle timers), and it gives the **client** its own liveness check and reconnect logic. That's why real-time libraries build it in. **SignalR** (ASP.NET Core's real-time library) sends keep-alive messages automatically, and its client treats the server as gone if it hears nothing within a timeout.

**The cost:**
- **Per connection:** a few bytes every N seconds. Negligible.
- **At scale:** 1,000,000 connections × one message per 30 s ≈ **33,000 messages per second** for the server to handle, just for heartbeats. The interval is a tuning knob between responsiveness and load.
- **Mobile:** each heartbeat can wake the radio, which costs battery. This is why mobile apps often use longer intervals or push notifications instead.
- **Rule of thumb:** the heartbeat interval must be **shorter than the smallest idle timeout anywhere on the path** (YARP's 100 s, a cloud load balancer's idle timeout, a NAT's timer).

### What to take away

- Socket = OS endpoint; TCP = reliable byte pipe; HTTP = request/response on the pipe; WebSocket = HTTP that **upgrades** into two-way messaging.
- YARP forwards the upgrade, then **relays bytes** and holds two connections per WebSocket.
- `ActivityTimeout` is a **watchdog** against silently dead connections. Idle-but-alive sockets need something to kick it.
- Keep-alives: server `KeepAliveInterval` (**set it below 100 s**; the default 2 min isn't enough), app-level heartbeats from the client, or a longer `ActivityTimeout`.

### Next steps (for item A)

- [ ] **See it happen (~1 h, .NET 10 on any machine):** create (1) a minimal ASP.NET Core **echo WebSocket server**, (2) a minimal YARP app routing `/ws` to it, and (3) a small console client using `ClientWebSocket` that connects through YARP and then sits idle. Watch the connection drop at about 100 s.
- [ ] Set `KeepAliveInterval = TimeSpan.FromSeconds(30)` on the echo server and confirm the connection now survives. Then try the default (2 min) and confirm it still drops. **That gotcha is worth mentioning in your #1764 comment and in the docs sentence.**
- [ ] Optional: open the connection from a browser (the dev tools console is enough), add a `setInterval` heartbeat, and watch the frames in the browser's Network tab (the WS "Messages" view).
- [ ] Update your draft comment on #1764 (from Q2) with what you observed. A comment with a verified repro is much stronger than one that only cites the docs.

---

## Q4. Getting my bearings: where does #1764 stand, what evidence do I still need, and what do I do with it?

**Related:** [YARP #1764](https://github.com/dotnet/yarp/issues/1764), item A · **Asked:** 2026-09-27 ·
**This entry is the current progress tracker for the issue** (checklist in §8).

### The question

After restructuring the repo, I want to get my bearings on #1764. What do I have so far, what's the actual state
of the issue and the docs, what else should I do, what evidence should I collect, and what do I do with that
evidence once I have it?

### The short answer

- **The issue is still open and unclaimed.** No assignee, milestone Backlog, labels `Type: Documentation` +
  `help wanted`, no activity since 2023-01-09 (checked 2026-09-27).
- **The docs gap is real but small.** The Timeouts page covers the core of it; the WebSockets page still doesn't
  mention `ActivityTimeout`. **Neither page mentions that ASP.NET Core's default keep-alive (2 min) is too slow
  for YARP's default timeout (100 s).** That gotcha is the one new thing you bring, and your repro is what proves it.
- **You have a working repro and a solid understanding, but no saved evidence.** There isn't a single log file on
  disk. The only recorded output is pasted into the lecture, and it's all from 8 s override runs. The real-default
  runs (the numbers the comment will quote) were never captured.
- **What's left is about 2–3 hours of work:** one ~30-minute evidence session, one comment, one short docs PR.
  From here the bigger risk is over-investing, not under-investing (see §7).

### Intent Header

| | |
|---|---|
| **Goal** | Get the WebSocket idle-timeout behavior (and the default-interval gotcha) onto the page WebSocket users actually read, and get #1764 closed. |
| **Done when** | A PR is merged in `dotnet/AspNetCore.Docs` and #1764 is closed, **or** maintainers close #1764 as already covered after my comment. Either outcome counts. |
| **Phases** | 1. Capture evidence (real defaults) → 2. Comment on #1764 → 3. PR in AspNetCore.Docs → 4. Close the loop (update trackers). |
| **Not doing (now)** | Telemetry wiring, browser heartbeat experiments, rewriting the lecture, touching YARP's code. All parked in §9. |
| **Unknowns** | Whether maintainers want the text on the WebSockets page, the Timeouts page, or both; how quickly they reply. |

---

### 1. Current state (verified 2026-09-27)

```
 dotnet/yarp#1764 (open since 2022-06-17, opened by Tratcher, a YARP maintainer)
   │  "100s is the default activity timeout... WebSocket or application level keep-alives
   │   are required... enabled on either the client or server (not the proxy)."
   │
   ├── docs live in ──► dotnet/AspNetCore.Docs / aspnetcore/fundamentals/servers/yarp/
   │                     ├── timeouts.md    (ms.date 11/01/2025)  ✅ covers it (WebSockets section)
   │                     └── websockets.md  (ms.date 2/6/2025)    ❌ Timeout section silent on ActivityTimeout
   │
   ├── source of truth ─► dotnet/yarp  ForwarderRequestConfig.ActivityTimeout
   │                        "The default is 100 seconds ... TCP keep-alive packets and HTTP/2 protocol pings
   │                         will not reset the timeout, but WebSocket pings will."
   │                      ForwarderError.UpgradeActivityTimeout
   │                        "An upgraded request was idle and canceled due to the activity timeout."
   │
   └── still biting users ► dotnet/yarp#2615 (2024): "YARP keep terminating the WebSocket after around
                            2 minutes", error UpgradeActivityTimeout; a maintainer explained the 100 s
                            ActivityTimeout again. The user fixed it by raising ActivityTimeout.
```

| Fact | Value | How checked |
|---|---|---|
| #1764 state | Open, unassigned, Backlog, 2 comments, last update 2023-01-09 | GitHub API, 2026-09-27 |
| Open PR for it? | None found | Web search; no cross-references visible on the issue |
| `websockets.md` Timeout section | Only says HTTP request timeouts are disabled after the handshake, then links to Timeouts | Raw file on `main`, 2026-09-27 |
| `timeouts.md` WebSockets section | "`ActivityTimeout` does apply to WebSocket requests. WebSocket keep-alives can be enabled by either the client or server..." Nothing about the interval needing to be shorter than the timeout. | Raw file on `main`, 2026-09-27 |
| `WebSocketOptions.KeepAliveInterval` default | "The default is two minutes." | API docs (aspnetcore-10.0) |
| Latest `Yarp.ReverseProxy` | 2.3.0 (what the sample uses) | NuGet version index |

**Why #2615 matters:** it's independent proof that users still hit this, two years after #1764 was opened. It
makes a better "why this docs change is worth merging" argument than anything you could write yourself. Cite it in
the comment.

---

### 2. What you have (inventory)

| Artifact | State | Strength | Gap |
|---|---|---|---|
| `sample/` (EchoServer, Proxy, IdleClient) | Builds and runs; SDK pinned to 10.0.302, YARP 2.3.0 | Minimal and readable. A maintainer can run it in 5 minutes. | `IdleClient` never exits on its own (fine; Ctrl+C) |
| 8 s override results (experiments 1–3) | Output pasted into `lectures/001-...md` | Shows the mechanism and the exact timing (8.0 s) | Not raw files; no proxy-side log saved; 8 s isn't the number users see |
| Real-default results (100 s) | Described in lecture §7/§9 | — | **No output saved anywhere.** Treat as unverified. |
| Understanding (Q1–Q3, lecture 001) | Deep | You can explain *why* it happens, down to `StreamCopier` and `ActivityCancellationTokenSource` | — |
| `README.md` (public evidence page) | Written | Claim, environment, repro commands, results table | Rows 3–4 have no evidence; no `sample/evidence/` folder |
| Upstream activity | None | — | No comment, no PR |

Q3's "Next steps" checklist status: the repro and the fix are done; the default-interval gotcha was observed at the
8 s scale only; the browser heartbeat experiment was not done (parked); the draft comment hasn't been updated yet
(done below, in §5.2).

---

### 3. What each claim needs as evidence

Only collect evidence for claims you're going to make. There are four:

| # | Claim | Already stated upstream? | Your evidence now | What's needed |
|---|---|---|---|---|
| C1 | An idle WebSocket through YARP is aborted after `ActivityTimeout` (default 100 s) | Yes: the issue, Timeouts page, source comment | 8 s run, pasted | **E1**: real 100 s run, client + proxy logs saved |
| C2 | A server WebSocket keep-alive shorter than the timeout prevents it | Yes: Timeouts page | 3 s vs 8 s run, pasted | **E2**: 30 s keep-alive vs 100 s timeout, watched for ≥ 300 s, saved |
| C3 | **ASP.NET Core's default 2-minute `KeepAliveInterval` does *not* prevent it** | **No. This is the new information.** | 8 s run only | **E1** covers it: both sides left at defaults and it still dies at 100 s |
| C4 | The failure is an abort, not a graceful close | No, but it's a side detail | Client exception text, pasted | Comes free with E1 |

C3 is why your comment is worth more than "+1, the docs are missing a sentence". **E1 is the most important run
you'll do**, because one run with both sides at their out-of-the-box defaults proves C1, C3 and C4 together, at
exactly the numbers a real user would see.

---

### 4. Evidence collection plan

```
 E1  IdleClient ──► Proxy [ActivityTimeout = 100 s default] ──► EchoServer [KeepAliveInterval = 2 min default]
     expected: client aborted at ~100.0 s; proxy logs an error naming the activity timeout

 E2  IdleClient ──► Proxy [ActivityTimeout = 100 s default] ──► EchoServer [KeepAliveInterval = 30 s]
     expected: client prints "...still open at 300s"; proxy logs no error
```

| Run | EchoServer | Proxy | Watch for | Pass if | Save as (in `sample/evidence/`) |
|---|---|---|---|---|---|
| **E1** | `dotnet run --project EchoServer` (no env var) | `dotnet run --project Proxy` (no env var) | ~2 min | Client prints `[100.x s] Connection died` and `Aborted` | `001-defaults-abort-client.txt`, `001-defaults-abort-proxy.txt` |
| **E2** | `WS_KEEPALIVE_SECONDS=30 dotnet run --project EchoServer` | `dotnet run --project Proxy` | ≥ 300 s (3× the timeout), then Ctrl+C | Client prints `...still open at 300s` | `002-keepalive30-survives-client.txt`, `002-keepalive30-survives-proxy.txt` |
| E3 (optional) | Re-run the three 8 s experiments | with the 8 s override | 1 min each | Same as lecture | `003-*.txt`. Only if you want the fast runs as files too. |

**How to capture (macOS, from `yarp/1764_websocket_idle_timeout/sample/`):**

```bash
mkdir -p evidence
lsof -i :5000 -i :5050            # must print nothing before each run: no stale servers (lecture 001 §3 gotcha)

# One header per client file, so the file is self-describing:
{ echo "# run:     E1 defaults on both sides"
  echo "# date:    $(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo "# os:      macOS $(sw_vers -productVersion)"
  echo "# dotnet:  $(dotnet --version)   Yarp.ReverseProxy 2.3.0"
  echo "# echo:    dotnet run --project EchoServer   (no WS_KEEPALIVE_SECONDS)"
  echo "# proxy:   dotnet run --project Proxy        (appsettings ActivityTimeout 00:01:40)"
  echo "# client:  dotnet run --project IdleClient -- ws://localhost:5000/ws"
  echo; } > evidence/001-defaults-abort-client.txt

# Terminal 1
dotnet run --project EchoServer
# Terminal 2
dotnet run --project Proxy 2>&1 | tee evidence/001-defaults-abort-proxy.txt
# Terminal 3
dotnet run --project IdleClient -- ws://localhost:5000/ws 2>&1 | tee -a evidence/001-defaults-abort-client.txt
```

For E2, repeat with the `002-...` names and `WS_KEEPALIVE_SECONDS=30` on EchoServer. Stop the client with Ctrl+C
after the `300s` line, then stop the servers and check `lsof` again.

**What to look for in the proxy log (E1):** the lecture recorded a stack trace through
`StreamCopier.CopyAsync(... ActivityCancellationTokenSource ...)`. #2615's user saw the error name
`UpgradeActivityTimeout`. *Unverified:* whether your Proxy's log level shows that name. Save whatever appears;
don't change code to make it appear.

**Time budget:** E1 about 5 minutes, E2 about 8 minutes, updating the README 15 minutes. **The whole session is
under 45 minutes.** If something fails, that's a signal to debug the setup (is anything listening on the ports?),
not a reason to redesign the sample.

---

### 5. What to do with the evidence

```
 sample/evidence/*.txt ──► README.md results table (rows point at files)
                                   │
                                   ▼
                      comment on dotnet/yarp#1764  (links the README)
                                   │
          ┌────────────────────────┼─────────────────────────────┐
          ▼                        ▼                             ▼
   "yes, please PR"        no reply in ~7 days          "already covered, closing"
          │                        │                             │
          └──────────► PR in dotnet/AspNetCore.Docs ◄────┘       └──► done (triage counts)
                                   │
                                   ▼
          update README table · issue CLAUDE.md · scouting tracker · persona.md
```

#### 5.1 Update the public README

Replace rows 3–4 of the results table with the E1/E2 outcomes and a link to each file. Delete the "log not
saved" note. Rows 1–2 can stay as "8 s override, recorded in lecture 001" unless you do E3.

#### 5.2 The comment on #1764 (draft v2, replacing Q2's draft)

Keep it short: maintainers skim, and the link carries the detail.

> Hi! I'd like to help close this one out.
>
> Most of this is now documented in the [Timeouts page's WebSockets section](https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/timeouts#websockets),
> but the [WebSockets page's Timeout section](https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/websockets#timeout)
> only mentions HTTP request timeouts, so people reading about WebSockets won't find it (for example #2615).
>
> One detail neither page mentions: ASP.NET Core's default `WebSocketOptions.KeepAliveInterval` is 2 minutes,
> which is longer than YARP's default 100 s `ActivityTimeout`. So a destination server that "has keep-alives on"
> with defaults still gets its idle connections aborted. I verified this on .NET 10 / YARP 2.3.0 with a minimal
> client → YARP → echo server repro: with defaults on both sides the connection is aborted at 100 s; with a 30 s
> `KeepAliveInterval` it stays open. Repro and logs: <link to yarp/1764_websocket_idle_timeout/README.md>
>
> Would a short addition to the WebSockets page's Timeout section (ActivityTimeout still applies after the
> handshake, keep-alives must be shorter than it, link to Timeouts) resolve this? If so I'm happy to open the PR
> in dotnet/AspNetCore.Docs.

Before posting: fill in the link, make sure the numbers match your evidence files exactly, and re-check the issue
for new activity. There's no need to @-mention anyone; the issue author gets notified.

#### 5.3 The proposed docs text (for the PR)

The current `websockets.md` Timeout section, with a new second paragraph:

```markdown
## Timeout

[Http Request Timeouts](/aspnet/core/performance/timeouts) (.NET 8+) can apply timeouts to all requests by default or by policy. These timeouts will be disabled after a WebSocket handshake. They will still apply to gRPC requests. For additional configuration see [Timeouts](xref:fundamentals/servers/yarp/timeouts).

The cluster's `ActivityTimeout` (100 seconds by default) still applies after the handshake: if no data or WebSocket ping frames are sent in either direction for that long, YARP closes the connection. To keep idle WebSocket connections open, enable WebSocket keep-alives on the client or the destination server with an interval shorter than `ActivityTimeout`, or increase `ActivityTimeout` for the cluster. ASP.NET Core's default <xref:Microsoft.AspNetCore.Builder.WebSocketOptions.KeepAliveInterval> is two minutes, which is longer than the default `ActivityTimeout`. For more information, see [Timeouts](xref:fundamentals/servers/yarp/timeouts#websockets).
```

Things to confirm when you write the PR: the `#websockets` anchor resolves; the xref form for the API link matches
what other AspNetCore.Docs pages use; whether to bump `ms.date` (the contributor guide says).

#### 5.4 The PR

- Use GitHub's web editor on `aspnetcore/fundamentals/servers/yarp/websockets.md` (no clone needed; the repo is
  huge). One file, one section.
- Title something like *"YARP WebSockets: document ActivityTimeout and keep-alive interval"*.
- Description: what changed, why (link `dotnet/yarp#1764`, mention #2615), and a *Verification* line with the
  environment and a link to the evidence README.
- **AI disclosure:** the page's front matter already has `ai-usage: ai-assisted`. Read AspNetCore.Docs'
  contributor guidance on AI-generated content and follow it. Keep the prose your own words, or disclose it.
- Referencing the YARP issue from a PR in another repo may not auto-close it. After merge, comment on #1764 with
  the PR link so a maintainer can close it.

#### 5.5 Close the loop

Record the comment and PR links, and the outcome, in: this Q4 checklist, `../CLAUDE.md` status, the root
`README.md` issues table, and the tracker in `scouting/001-issue_shortlist_sept_2026.md`.

---

### 6. Decision table: what might happen after the comment

| Maintainer response | Your move |
|---|---|
| "Yes, please PR" | §5.3–5.4 |
| "Put it in Timeouts instead / as well" | Add the gotcha sentence to `timeouts.md`'s WebSockets section instead of (or as well as) `websockets.md`. Still one small PR. |
| "Already covered, closing" | Done. Update the trackers. Triage that closes a stale issue is a real contribution. |
| No reply after ~7 days | Open the PR anyway. AspNetCore.Docs PRs are reviewed by the docs team, not only YARP maintainers. Link it on #1764. |
| Someone else opens a PR first | Review it, and add your repro link as supporting evidence. Don't compete. |

---

### 7. Process check (honest)

- **What went well:** on 2026-09-26 you went from reading the issue to a running three-process repro in one day,
  and you checked the *current* docs before writing anything (Q2). Both are exactly the habits that make a first
  PR go smoothly.
- **What to watch:** the upstream deliverable is about 120 words of docs plus a comment. You now have three
  concept notes, a 245-line lecture, and a shortlist, and nothing has been posted. That ratio is fine for
  learning, but it's the "too slow / too deep" pattern from `persona.md` if it keeps growing. **Posting is now
  the thing that unblocks everything else**, because the maintainers' answer decides what the PR looks like.
- **Rule for the next session:** evidence (§4) and the comment (§5.2) in one sitting. Anything interesting that
  comes up goes in §9's ledger, not into a new deep dive, until the comment is posted.

---

### 8. Checklist (progress tracker)

- [x] Read the issue; find where the docs live (Q2, 2026-09-26)
- [x] Discover the Timeouts page already covers the core (Q2, 2026-09-26)
- [x] Build the three-process repro; observe abort, fix and gotcha at 8 s (lecture 001, 2026-09-26)
- [x] Restructure repo; public evidence page `README.md` (2026-09-27)
- [x] Re-verify issue and docs state; find #2615; plan evidence (this entry, 2026-09-27)
- [ ] **E1**: defaults on both sides, 100 s abort, client + proxy logs saved
- [ ] **E2**: 30 s keep-alive survives ≥ 300 s, logs saved
- [ ] Update `README.md` results table with evidence links
- [ ] Commit and push, so the link in the comment works
- [ ] Re-check #1764, then post the comment (§5.2)
- [ ] Maintainer response → follow §6
- [ ] PR in `dotnet/AspNetCore.Docs` (§5.3–5.4)
- [ ] PR merged / issue closed; trackers updated (§5.5)

---

### 9. Question Ledger (parked until the comment is posted)

- **A general lecture:** "long-lived connections through middleboxes": every hop (NAT, cloud load balancer,
  YARP, corporate proxy) has its own idle timer, and heartbeats have to beat the *smallest* one. That's the
  concept under #1764, and it transfers to SignalR, gRPC streaming, and MQTT. Lecture 001 is currently about the
  YARP repro specifically. A general version would suit the `lectures/` folder better.
- **Telemetry:** wire `Yarp.Telemetry.Consumption` into `Proxy` and watch `WebSocketCloseReason.ActivityTimeout`
  get reported (the old optional step).
- **Why did #2615 see "around 2 minutes" rather than 100 s?** Unknown. Possibly the browser noticed late, or a
  different hop. Curious, but not needed.
- **Browser heartbeat experiment** from Q3 (client-side fix, watch frames in DevTools).

### Sources (checked 2026-09-27)

- [dotnet/yarp#1764](https://github.com/dotnet/yarp/issues/1764) · [dotnet/yarp#2615](https://github.com/dotnet/yarp/issues/2615)
- [`websockets.md` on main](https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/fundamentals/servers/yarp/websockets.md) · [YARP Timeouts page](https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/timeouts)
- [`ForwarderRequestConfig.cs`](https://github.com/dotnet/yarp/blob/main/src/ReverseProxy/Forwarder/ForwarderRequestConfig.cs) · [`ForwarderError.cs`](https://github.com/dotnet/yarp/blob/main/src/ReverseProxy/Forwarder/ForwarderError.cs)
- [`WebSocketOptions.KeepAliveInterval`](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.builder.websocketoptions.keepaliveinterval) · [Yarp.ReverseProxy on NuGet](https://www.nuget.org/packages/Yarp.ReverseProxy)

---

## Q5. My E1 run stayed open past 300 s with "defaults on both sides". Did I collect it wrong?

**Related:** [YARP #1764](https://github.com/dotnet/yarp/issues/1764), item A · **Asked:** 2026-09-27 ·
**Corrects:** [Q3 §5](#5-fix-1-server-side-keep-alives-your-reading-confirmed) ("with defaults the proxy still cuts idle
sockets") and [Q4 §3–§5.2](#3-what-each-claim-needs-as-evidence) (claim C3, and E1's expected result) · **This entry is
now the progress tracker** (§4).

### The question

I tried to capture E1 (defaults on both sides, expected: aborted at 100 s). The client was still open at 300 s. I don't
think I did it right. What should the evidence actually be?

### The short answer

**You collected it correctly. The prediction was wrong.** .NET's `ClientWebSocket` sends its own keep-alive frame (an
unsolicited Pong) every **30 s** by default, and that resets YARP's 100 s `ActivityTimeout` just like a server keep-alive
does. So with a .NET client, "defaults on both sides" survives. The case that fails at defaults is a **browser** client,
which can't send keep-alives at all. The full evidence session, with a wire tap on both hops, screenshots and nine runs,
is in [`implementations/001-lab-report-idle-websockets-through-yarp.md`](../implementations/001-lab-report-idle-websockets-through-yarp.md).

### The explanation

| Who sends a keep-alive by default | Interval | Beats YARP's 100 s? |
|---|---|---|
| .NET `ClientWebSocket` | 30 s | ✅ yes, so the connection survives (runs 001, 003) |
| ASP.NET Core `UseWebSockets` (server) | 2 min | ❌ no |
| Browser `WebSocket` | none, no API for it | ❌ nothing to send |

The rule: **the connection survives if the smallest keep-alive interval of any endpoint is under `ActivityTimeout`.**
The lecture's 8 s runs *looked* like they proved "defaults die" only because at 8 s the client's 30 s keep-alive was
too slow as well (run 007). The 8 s override wasn't a faithful scale model of the 100 s case.

| Run | Setup (timeout 100 s) | Result |
|---|---|---|
| 003 | .NET client, all defaults | open at 310 s (client Pong every 30 s on the wire) |
| 004 / 006 | .NET client, keep-alive **off**, server default | **aborted at 100.1 s**, `UpgradeActivityTimeout` |
| 005 | .NET client off, server 30 s | open at 310 s |
| 008 | Chrome, server default | **aborted at 100.1 s**, close code 1006 |
| 009 | Chrome, server 30 s | open at 310 s |

### What to take away

- A result that contradicts the prediction is the most useful run in the session. Explain it with an instrument (here,
  a wire tap) before collecting more.
- "Enabled" isn't the question for keep-alives; "shorter than the smallest idle timer on the path, from *some* endpoint" is.
- Test with the client your users actually run. For #1764 that's usually a browser.
- The docs text in Q4 §5.3 still stands. The **comment draft in Q4 §5.2 must not say "defaults on both sides → aborted"**.
  Use the corrected sentence in the lab report §6.3.

### 4. Checklist (progress tracker, continues Q4 §8)

- [x] E1/E2 equivalents captured at real defaults: runs 002–009 in `sample/evidence/` (2026-09-27)
- [x] `README.md` results table updated with evidence links (2026-09-27)
- [x] Lab report written: `implementations/001-lab-report-idle-websockets-through-yarp.md` (2026-09-27)
- [ ] Review, commit and push, so the link in the comment works
- [ ] Re-check #1764, then post the comment (Q4 §5.2 with the lab report §6.3 sentence)
- [ ] Maintainer response → follow Q4 §6
- [ ] PR in `dotnet/AspNetCore.Docs` (Q4 §5.3–5.4)
- [ ] PR merged / issue closed; trackers updated (Q4 §5.5)

---

## Q6. Checking my mental model, are the tests good enough, and how do I close this out?

**Related:** [YARP #1764](https://github.com/dotnet/yarp/issues/1764), item A · **Asked:** 2026-09-27 ·
**Deep dive:** [`yarp/yarp_concepts/001-the-gatekeeper-in-the-middle.md`](../../yarp_concepts/001-the-gatekeeper-in-the-middle.md)
· **This entry is now the progress tracker** (§5).

### The question (summarized)

I explained my understanding: a client connects through YARP, which routes to a server using clusters and routes; a
WebSocket is the four numbers (both IPs and ports); open sockets are expensive, and a vanished client never says
goodbye, so something has to time them out; each party has its own timeout defaults; normally the client sends the
ping, but somehow client pings "aren't valid" with YARP. Then I got stuck: **who actually manages the connection?**
If the socket is four numbers, is it client↔YARP or client↔server? Why does YARP close it and not the server? If
YARP is a router for stateless requests, and a WebSocket is a permanent connection between client and server, why is
YARP involved at all after the handshake? And I'm not sure how my experiment configurations relate to the issue, or
how to move forward.

### The short answer

Most of your model is right. **One wrong picture caused every other confusion:** there is no connection from the
client to the server. There are **two** TCP connections, client↔YARP and YARP↔server, and **YARP owns one end of
each**. YARP copies every byte between them for the whole life of the WebSocket. So YARP is never "out of the
picture"; it's holding two sockets per idle WebSocket, and it hangs up on idle ones to protect its own resources.
Any byte from either side (including invisible keep-alive frames) resets its 100 s timer, so client keep-alives
**are** valid; browsers just can't send them.

**The tests are good enough. Stop experimenting and post.** §3 has the verdict and §4 the close-out steps.

### 1. Your model, checked line by line

| You said | Verdict | The correction (lecture section) |
|---|---|---|
| Client → YARP → server; YARP picks the server from its config (clusters, routes) | ✅ | Small fix: **routes** point at **clusters**, clusters contain **destinations**. `ActivityTimeout` is a *cluster* setting (§3.1) |
| A WebSocket is four values: both IPs and ports | ⚠️ | That's the definition of a **TCP connection** (the 4-tuple). A WebSocket is a protocol spoken *over* one TCP connection (§2.1) |
| Open sockets are expensive at scale | ✅ | For YARP: 2 sockets + 2 × 64 KB buffers + 2 waiting tasks **per WebSocket**, idle or not (§5.4, §6.1) |
| A vanished client sends no stop signal, so the connection could live forever | ✅ | Called a **half-open connection**. TCP only notices when it tries to *send*; TCP's own keep-alive is off by default and waits 2 h (§6.1) |
| Each party has its own timeout defaults and keep-alive methods | ✅ | Exactly: each owner polices only **its own** sockets, and the shortest active timer on the path fires first (§6.2) |
| Normally the client sends the pings | ⚠️ | Either side may. Servers most commonly send protocol pings; browser apps use **app-level heartbeats** because the browser API can't send pings (§8.3) |
| With YARP, client keep-alives aren't valid | ❌ | They're valid. Run **003**: the .NET client's Pong every 30 s kept the connection open. The issue says "client **or** server (not the proxy)": YARP won't generate keep-alives itself (§8.3) |
| Maybe YARP manages the connections | ✅, precisely | YARP manages **its two sockets**; the client and server each manage their own. Nobody manages "the connection" from outside (§2.3) |
| Is it client↔YARP or client↔server? | → | **Both hops exist, as separate connections. Client↔server does not exist** (§2.2) |
| Why doesn't the server close it? | → | It could, with its own policy (`KeepAliveTimeout`, off by default). That protects the *server's* memory, not YARP's. In your lab YARP's 100 s is the shortest active timer, so it fires first (§6.2) |
| YARP routes stateless requests, so why is it involved with a stateful WebSocket? | → | The route/cluster/destination decision is made **once**, on the handshake request, then pinned. YARP stays in the data path because the client's bytes arrive at **YARP's** socket; nobody else can forward them (§5.3–5.4) |

The picture to keep:

```
 Caller ═══ conn #1 ═══ [ YARP: socket A ⇄ Pump ⇄ socket B ] ═══ conn #2 ═══ Echo
                                  one Watchdog, 100 s
            any byte, either direction, either hop → Watchdog reset
            no bytes for 100 s → YARP closes BOTH sockets (no Close frame)
```

### 2. What the issue is, and what your experiments show

**The issue:** it's a **gap**, not a mismatch. Nothing in the docs is wrong. The WebSockets page tells readers that
request timeouts are switched off after the handshake, but not that `ActivityTimeout` still applies. The Timeouts
page does say it, but neither page says that the keep-alive interval has to be *shorter* than the timeout, or that
ASP.NET Core's default (2 min) isn't.

**Your experiments are three knobs and one comparison:**

```
 KNOB 1  Caller's keep-alive     .NET 30 s by default · browser: never
 KNOB 2  Server's keep-alive     ASP.NET Core 2 min by default
 KNOB 3  YARP's ActivityTimeout  100 s by default

 survives  ⇔  min(KNOB 1, KNOB 2)  <  KNOB 3
```

Which runs matter for the PR, and why each exists:

| Run | Role in the argument |
|---|---|
| **008** (browser, server default) → aborted at 100.1 s | **The problem**, in the configuration real users have. The headline. |
| **009** (browser, server 30 s) → open at 310 s | **The fix** the docs will recommend. |
| 004 / 005 | The same problem and fix with a .NET client (keep-alive off), with full frame-level logs |
| 003 (.NET client, all defaults) → survives | Explains why .NET-client tests hide the problem (it brings its own 30 s keep-alive) |
| 006 | Control: the taps don't change the result |
| 007 | Explains why the early 8 s runs looked like "defaults always die" |
| 002 | The framework defaults, printed by the runtime rather than quoted from docs |

For the comment and PR you only need to *cite* 008 and 009, and link the rest.

### 3. Are the tests good enough? Yes.

| Standard | Met? | Notes |
|---|---|---|
| Real defaults, not a scaled model | ✅ | 100 s timeout, framework-default keep-alives |
| The client users actually run | ✅ | Headless Chrome, plus .NET |
| Problem **and** fix both shown | ✅ | 008/009 and 004/005 |
| Mechanism observed, not inferred | ✅ | Frame logs on both hops; YARP's own `UpgradeActivityTimeout` warning |
| Control for the instrument | ✅ | 006 |
| Reproducible | ✅ | Pinned SDK, one-command harness, headers on every file, port checks |
| Honest about scope | ✅ | Loopback, HTTP/1.1, no TLS, one browser, one run each: all stated |

Things that are *not* worth doing before posting: repeating runs, HTTP/2, TLS, other browsers, telemetry. None of
them would change the docs sentence. Two small notes, neither blocking:

- The evidence headers say `repo: 6cbe9d4 (+ uncommitted sample changes, if any)`, so a header can't prove exactly
  which code ran. That's fine for a docs issue; next time, commit the sample before the evidence session.
- The README says Ping *or* Pong resets the timer. Your runs only show Pong. Ping is covered by YARP's source (any
  bytes reset it) and its docs, so the claim holds, but it isn't something you measured.

### 4. Close-out plan

Everything is committed and pushed (`main` = `origin/main` as of 2026-09-27). The remaining work is about an hour.

**Recommended route: open the PR and comment in the same sitting.** AspNetCore.Docs' CONTRIBUTING says small
content changes go straight to a PR via the web editor, no issue first. #1764 is already `help wanted`, and the
maintainer who opened it wrote the content. If the reviewers want the text somewhere else, they'll say so in review,
and that's a two-minute change. Waiting a week for permission to add one paragraph is the "too slow" pattern.
(The alternative, comment first and PR after a reply, is Q4 §6. It's also fine, just slower.)

**Step 1: check the link works publicly (2 min).** Open
`https://github.com/Timothy-Lee-Grant/playground/tree/main/yarp/1764_websocket_idle_timeout` in a private browser
window. If it 404s, the repo is private and the link is useless to maintainers.

**Step 2: open the PR (30 min).** Go to
[`websockets.md`](https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/fundamentals/servers/yarp/websockets.md),
click the pencil, and add this paragraph at the end of the `## Timeout` section (v2 of Q4 §5.3, now saying what the
browser run showed):

```markdown
The cluster's `ActivityTimeout` (100 seconds by default) still applies after the handshake. If no data or WebSocket keep-alive frames are sent in either direction for that long, YARP closes the connection. To keep idle WebSocket connections open, send keep-alives from the client or the destination server at an interval shorter than `ActivityTimeout`, or increase `ActivityTimeout` for the cluster. Browser clients don't send WebSocket keep-alives, so they rely on the destination server: ASP.NET Core's default <xref:Microsoft.AspNetCore.Builder.WebSocketOptions.KeepAliveInterval> is two minutes, which is longer than the default `ActivityTimeout`. For more information, see [Timeouts](xref:fundamentals/servers/yarp/timeouts#websockets).
```

Before committing the edit: preview it; check the `#websockets` anchor exists on the Timeouts page; look at how other
pages write `<xref:...>` API links. Leave `ms.date` and the `ai-usage` metadata alone unless the PR template or a
reviewer says otherwise. Sign the CLA if the bot asks.

PR title: **YARP WebSockets: document ActivityTimeout and keep-alive interval**. Description:

```markdown
Fixes dotnet/yarp#1764.

The WebSockets page's Timeout section says request timeouts are disabled after the handshake, but not that
`ActivityTimeout` (default 100 s) still applies. This adds a short paragraph and links to the Timeouts page, which
covers it in more detail. It also notes that ASP.NET Core's default `KeepAliveInterval` (2 min) is longer than the
default `ActivityTimeout`, which matters for browser clients because they don't send keep-alives themselves.

Verification (.NET 10.0.302, Yarp.ReverseProxy 2.3.0, Chrome 153): with a browser client and the server at its
default keep-alive, an idle connection through YARP was aborted after 100 s (`UpgradeActivityTimeout`, close code
1006); with a 30 s server `KeepAliveInterval` it stayed open. Repro, logs and screenshots:
https://github.com/Timothy-Lee-Grant/playground/tree/main/yarp/1764_websocket_idle_timeout

I used an AI assistant (Claude) to help build the repro and draft this text; I ran the experiments and checked the
results myself.
```

**Step 3: comment on #1764 (5 min)**, after the PR exists:

> I've opened dotnet/AspNetCore.Docs#<PR> for this. The Timeouts page already covers `ActivityTimeout` for
> WebSockets, but the WebSockets page doesn't mention it, so the PR adds a short paragraph there with a link.
>
> One detail neither page mentioned: ASP.NET Core's default `KeepAliveInterval` (2 min) is longer than YARP's default
> `ActivityTimeout` (100 s). I verified on .NET 10 / YARP 2.3.0 that a browser client (which can't send pings) is
> aborted after 100 s with server defaults and stays open with a 30 s interval. (A .NET `ClientWebSocket` survives at
> defaults because it sends its own keep-alive every 30 s.) Repro and logs: <link>

**Step 4: respond to review.** Reviewers may reword it or ask for it on the Timeouts page instead. Say yes and adjust.
Log each round as a short entry here.

**Step 5: after merge.** If #1764 doesn't close automatically (cross-repo "Fixes" may not work), comment on #1764 with
the merged PR link and ask a maintainer to close it. Then update the root `README.md` table, the scouting tracker,
this folder's `CLAUDE.md`, and do the Reflect step (root `CLAUDE.md` §9.3).

### 5. Checklist (progress tracker, continues Q5 §4)

- [x] Evidence at real defaults, both client types, with controls (runs 002–009, 2026-09-27)
- [x] Lab report written (`implementations/001`)
- [x] Committed and pushed (checked 2026-09-27)
- [x] Mental model checked; YARP concepts lecture 001 written (2026-09-27)
- [x] Evidence reviewed: good enough to post (this entry, §3)
- [ ] Confirm the evidence link is publicly visible (§4 step 1)
- [ ] Open the AspNetCore.Docs PR (§4 step 2)
- [ ] Comment on #1764 with the PR link (§4 step 3)
- [ ] Review rounds (§4 step 4)
- [ ] Merged; #1764 closed; trackers updated; Reflect (§4 step 5)
