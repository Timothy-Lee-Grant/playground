# Lecture 001: The MCP C# SDK from the ground up

> **Prompted by:** Timothy's request (2026-10-04) for a full orientation to the MCP C# SDK open-source project, plus
> the concepts he's weakest on that matter in it.
> **Upstream checked:** `modelcontextprotocol/csharp-sdk` `main` @ `c40ee04` (2026-09-18); latest release tag
> `v2.2.0` (2026-08-13). Every type, file and default named here was read in that commit.
> **Companion:** audio version [`audio/001-audio-mcp-csharp-sdk-from-the-ground-up.md`](audio/001-audio-mcp-csharp-sdk-from-the-ground-up.md)
> (same characters, spoken style).
> **Prerequisites:** none required. If you've heard ASP.NET Core A001, the names Stage Manager, Supply Room, Job
> Ticket and Assembly Line mean the same things here.
> **Verified vs. unverified:** code facts are from reading the source. **Nothing here has been built or run on your
> machines yet.** Commands in §0 and §24 are what the repo documents, not something we've captured evidence for.

---

## The one-paragraph version

**MCP (Model Context Protocol) is a contract that lets any AI application call any tool server using JSON-RPC
messages. The C# SDK is a library that does the plumbing on both ends: on the server side it turns your attributed
C# methods into tools, reads JSON-RPC messages off a transport (stdin/stdout or HTTP), routes each one to a handler,
and writes the answer back; on the client side it sends requests and matches each reply to the request that's
waiting for it.** Almost every file in the repo is one of: a protocol data type, a transport, the session engine that
moves and correlates messages, the server or client built on that engine, or the glue that plugs it into .NET hosting
and ASP.NET Core.

You've already used this SDK: Tool_Box (July 2026) is built on it. That makes this lecture riskier, not easier.
You've *used* `AddMcpServer()`, `WithStdioServerTransport()` and `Stateless = true`, so the fuzzy-but-functional
problem applies: it worked, so the gaps never showed. This lecture is about what those calls actually do. Also, the
SDK went from 1.x to **2.0 on 2026-07-28**, eleven days after Tool_Box's Plan 001. The protocol changed underneath
it (§4), so part of what you learned then is now the "legacy" path.

---

## Map of this lecture

```
 PART A  The problem and the protocol         §1 what MCP is for  §2 JSON-RPC  §3 what servers offer
                                              §4 lifecycle (then vs now)  §5 transports
 PART B  The SDK's architecture               §6 packages & where code comes from  §7 cast of characters
                                              §8 layers  §9 startup  §10 ONE TOOL CALL, STEP BY STEP
                                              §11 the Postmaster (correlation, concurrency, cancellation)
                                              §12 the HTTP path  §13 the client side  §14 errors  §15 filters
 PART C  The .NET ideas underneath            §16 library vs framework  §17 attributes & reflection
         (the weak spots, made sharp)         §18 DI lifetimes & ownership  §19 delegates & "functions that
                                              return functions"  §20 async: Task, TCS, Channel, cancellation
                                              §21 compact-syntax decoder  §22 JSON source-gen, AOT, multi-targeting
                                              §23 versioning & diagnostics
 PART D  Working in the repo                  §24 build & test  §25 how the tests are built  §26 contributing
                                              §27 Tool_Box (1.x) vs today (2.x)
 END     mistakes · interview · production · check yourself · TEACH-BACK CHECKLIST
```

If you only have one evening: §1, §7, §9, §10, §11. That's the system. Everything else hangs off it.

---

## 0. Run it first (about 15 minutes, not yet tried)

A running system before a document. These are the smallest things that show the machine working. None of this has
been run on your Mac yet, so treat the expected output as "what should happen".

```bash
# 1. Clone (outside the exercises repo) and build. Needs the .NET 10 SDK (global.json pins 10.0.101, rollForward minor).
git clone https://github.com/modelcontextprotocol/csharp-sdk.git && cd csharp-sdk
dotnet build                      # warnings are errors in this repo

# 2. Run ONE test that does a whole tool call in-process: client → pipe → server → your method → back.
dotnet test tests/ModelContextProtocol.Tests \
  --filter "FullyQualifiedName~McpServerBuilderExtensionsToolsTests.Can_Call_Registered_Tool"

# 3. Watch the wire. Run the quickstart server and talk to it with the MCP Inspector (Node needed).
npx @modelcontextprotocol/inspector dotnet run --project samples/QuickstartWeatherServer
```

What to look at in step 3: the Inspector is an MCP **client**. It launches the server as a child process and shows
you the JSON-RPC messages. Call a tool and find the `"id"` in the request and the same `"id"` in the response. That
matching is the single most important mechanism in this lecture (§11).

> ⚠️ The Inspector command is the commonly documented form; check the sample's README if it differs. The test name
> and path are verified in the source.

---

# PART A: The problem and the protocol

## 1. What problem does MCP solve?

**Rule first: MCP standardizes how an AI application discovers and calls external capabilities, so N applications
and M tool servers need N + M implementations instead of N × M.**

Before MCP, every AI app (Claude Desktop, VS Code Copilot, Cursor, your LLM_Monitor) had its own plugin format, and
every tool provider wrote one adapter per app. MCP is a protocol in the same sense HTTP is: a shared contract for
messages, so either side can be swapped.

```
   BEFORE (N × M adapters)                    AFTER (N + M implementations of one contract)

   Claude ──┬── GitHub adapter               Claude ─┐                 ┌─ GitHub server
            ├── Postgres adapter             VS Code ─┼── MCP (JSON-RPC)┼─ Postgres server
   VS Code ─┼── GitHub adapter               Cursor ──┤                 ├─ Tool_Box (yours)
            └── Postgres adapter             LLM_Monitor┘               └─ NuGet server
```

### The three roles, and the word "host" collision

| MCP role | What it is | Example |
|---|---|---|
| **Host** | The AI application the user talks to. Runs the LLM loop. Owns one or more clients | Claude Desktop, VS Code, LLM_Monitor's LangGraph agent |
| **Client** | One connection from the host to one server. Speaks the protocol | `McpClient` in this SDK |
| **Server** | A program that offers tools, resources and prompts | Tool_Box; anything built with `AddMcpServer()` |

> ⚠️ **Name collision you will trip on.** In MCP, *host* means the AI app. In .NET, *host* means the **Generic Host**
> (`IHost`, `Host.CreateApplicationBuilder`), the object that owns DI, config, logging and background services. An MCP
> *server* written in C# runs **inside** a .NET *host*. In this lecture: "MCP host" = the AI app; "the Stage Manager" =
> the .NET Generic Host.

**What MCP is not** (F7, saying what the abstraction doesn't do):

- It doesn't run the LLM. The host does. The server never sees the conversation unless the host sends it pieces.
- It doesn't decide *when* to call a tool. The LLM proposes a call; the host executes it through a client.
- It isn't an auth system. Over HTTP it borrows OAuth; over stdio, security is "who can launch the process".

## 2. JSON-RPC 2.0 in ten minutes

**Rule first: every MCP message is one of four JSON shapes, and a reply is matched to its request only by `id`.**

| Shape | Has `id`? | Has `method`? | Expects a reply? | SDK type |
|---|---|---|---|---|
| Request | yes | yes | yes | `JsonRpcRequest` |
| Response (success) | yes (same as request) | no | no | `JsonRpcResponse` |
| Error response | yes (same as request) | no | no | `JsonRpcError` |
| Notification | **no** | yes | **never** | `JsonRpcNotification` |

All four derive from `JsonRpcMessage` (`src/ModelContextProtocol.Core/Protocol/JsonRpcMessage.cs`). Responses and
errors share `JsonRpcMessageWithId`. A custom polymorphic converter looks at which fields exist and picks the type.

A real `tools/call` under the 2026-07-28 revision (copied from the repo's HTTP conformance tests, reformatted):

```json
// request: client → server
{ "jsonrpc": "2.0", "id": 7, "method": "tools/call",
  "params": {
    "name": "echo",
    "arguments": { "message": "Peter" },
    "_meta": {
      "io.modelcontextprotocol/protocolVersion": "2026-07-28",
      "io.modelcontextprotocol/clientInfo": { "name": "TestClient", "version": "1.0" },
      "io.modelcontextprotocol/clientCapabilities": {}
    } } }

// response: server → client (same id)
{ "jsonrpc": "2.0", "id": 7,
  "result": { "content": [ { "type": "text", "text": "hello Peter" } ], "resultType": "complete" } }

// notification: no id, no reply (e.g. "please stop working on request 7")
{ "jsonrpc": "2.0", "method": "notifications/cancelled", "params": { "requestId": 7 } }
```

Three consequences you'll see all over the code:

1. **Messages can be in flight at the same time, in both directions.** Request 8 can be answered before request 7.
   So each side needs a table of "requests I sent that are still waiting" keyed by `id` (§11).
2. **Both sides can send requests.** It's not client-asks/server-answers only. The server can send requests to the
   client (elicitation, the old sampling). So the client and server share the same engine (`McpSessionHandler`).
3. **A notification can't fail visibly.** Nobody replies to it. Cancellation is a notification, so it's a polite
   request, not a guarantee (§20).

## 3. What a server offers (and what a client offers back)

**Rule first: servers offer three primitives that differ by who decides to use them: tools (the model), resources
(the application), prompts (the user).**

| Primitive | Who decides to use it | What it is | Methods | C# attribute |
|---|---|---|---|---|
| **Tool** | The **model** | A function with a JSON Schema for its arguments. Can have side effects | `tools/list`, `tools/call` | `[McpServerTool]` on a method in a `[McpServerToolType]` class |
| **Resource** | The **application** | Readable data addressed by a URI or URI template (`file:///{path}`) | `resources/list`, `resources/read`, `resources/templates/list` | `[McpServerResource]` |
| **Prompt** | The **user** | A named, parameterized message template (think slash command) | `prompts/list`, `prompts/get` | `[McpServerPrompt]` |

Client-side features (the server asks the client for something):

| Feature | What | Status in 2026-07-28 |
|---|---|---|
| **Elicitation** | Server asks the user (through the client) for input or confirmation | Current. Statelessly via MRTR (§5) |
| Sampling | Server asks the client's LLM to generate text | **Deprecated** (SEP-2577, diagnostic `MCP9005`) |
| Roots | Server asks which folders/URIs the client considers in scope | **Deprecated** (same) |
| Logging (`logging/setLevel`) | Server-to-client log messages | **Deprecated** (same) |

Plus utilities: **progress** notifications, **cancellation**, **pagination** (cursors on list calls), **completion**
(argument autocompletion), and **capabilities**: each side advertises which of the above it supports, so neither
calls something the other can't do. Two optional extension packages add **Tasks** (long-running tool calls you poll)
and **Apps** (tools linked to HTML UI the host can render; experimental, `MCPEXP003`).

## 4. Lifecycle: how a conversation starts (then vs now)

**Rule first: up to revision 2025-11-25, a connection began with an `initialize` handshake that fixed the protocol
version for the whole connection; from 2026-07-28, there is no handshake, and every request carries its own
protocol version and client capabilities in `_meta`.**

```
  2025-11-25 and earlier (Tool_Box era)            2026-07-28 (SDK 2.x default)
  ─────────────────────────────────────           ─────────────────────────────────────────
  client ── initialize {version, caps} ──► server  client ── server/discover (optional probe) ──► server
  client ◄── result {version, caps, info} ─ server client ◄── {supportedVersions, caps, info} ── server
  client ── notifications/initialized ───► server
  (connection now "knows" version + caps)          every request: _meta.protocolVersion,
  client ── tools/call ─────────────────► server                  _meta.clientCapabilities,
  over HTTP: server may assign Mcp-Session-Id                     _meta.clientInfo
            and the client must echo it             over HTTP: no Mcp-Session-Id at all
```

Why the change matters to you: a handshake means **state**. Whoever did the handshake must remember the result, so
over HTTP every later request had to reach the same server instance (session affinity). Moving the information into
each request makes every request self-describing, so any instance can serve it. That's the same move as REST's
"stateless" constraint. The source names the two proposals: SEP-2575 (remove `initialize`) and SEP-2567 (remove
`Mcp-Session-Id`) (`src/Common/McpProtocolVersions.cs`).

**Version negotiation in the SDK** (verified in `McpProtocolVersions.cs` and `docs/concepts/stateless/stateless.md`):

| Revision | Handshake | Notes |
|---|---|---|
| 2024-11-05, 2025-03-26, 2025-06-18, 2025-11-25 | `initialize` | `InitializeHandshakeProtocolVersions` |
| 2026-07-28 | none; per-request `_meta` | `PerRequestMetadataProtocolVersions`; the client's preferred version |

- The client **probes** first: on stdio it sends `server/discover` with a **5-second** timeout
  (`McpClientOptions.DiscoverProbeTimeout`); on HTTP it sends its first request with the `MCP-Protocol-Version:
  2026-07-28` header. If the server doesn't understand, the client **falls back** to `initialize` on the same
  connection. The result is cached per transport object.
- Versions are dates, so "is this 2026-07-28 or later?" is a plain **string comparison** (`StringComparer.Ordinal`).
  That works only because the format is fixed-width `YYYY-MM-DD`. A neat trick, and a fragile one if the format ever
  changed.
- That 5-second probe timeout is exactly what made CI flaky in issue #1701: `ClientServerTestBase` now raises it to
  `TestConstants.DefaultTimeout` (60 s). Scouting item E (#1806) is the same class of bug for OAuth.

## 5. Transports: how the bytes travel

**Rule first: a transport only moves JSON-RPC messages; it knows nothing about tools. stdio is one message per line
over a child process's stdin/stdout; Streamable HTTP is one POST per client message, with the reply streamed back in
the POST's response.**

| | stdio | Streamable HTTP, **stateless** (default) | Streamable HTTP, stateful | Legacy SSE |
|---|---|---|---|---|
| Process model | Server is a child process of the client | Remote web server | Remote web server | Remote web server |
| Framing | One JSON object per line (`\n`) | POST body in; JSON or an SSE stream out | same + long-lived GET stream | GET `/sse` stream + POST `/message` |
| Session | Implicit: one per process | **None**: each POST independent | `Mcp-Session-Id` kept in memory | Session id in query string |
| Server → client requests | ✓ | Only via **MRTR** (2026-07-28) | ✓ | ✓ |
| Unsolicited notifications | ✓ | ✗ | ✓ (on the GET stream) | ✓ |
| Backpressure | stdin/stdout flow control | POST held open until the handler finishes | same | **None**: POST returns 202 at once |
| Scale out | n/a | Any instance can serve any request | Needs sticky sessions | Needs sticky sessions |
| SDK types (server) | `StdioServerTransport` (a `StreamServerTransport`) | `StreamableHttpServerTransport` + `StreamableHttpHandler` | same + `StatefulSessionManager` | `SseResponseStreamTransport` (off by default, `[Obsolete]` `MCP9004`) |

Key ideas in that table:

- **stdout is the wire on stdio.** Anything else written to stdout (a `Console.WriteLine`, a default console logger)
  corrupts the protocol. That's why the getting-started sample sets `LogToStandardErrorThreshold = LogLevel.Trace`,
  and why Tool_Box had a "stderr-only logging" rule. You already knew the rule; now you know it's because the
  transport reads stdout line by line and tries to parse each line as JSON.
- **Backpressure** means "the producer is slowed down when the consumer can't keep up." Streamable HTTP gets it for
  free: the client's POST doesn't complete until the handler finishes, so a client can't pile up unlimited work. Legacy
  SSE returns `202 Accepted` before the handler runs, so a client (or attacker) can flood the server. That's the
  stated reason SSE is off by default.
- **MRTR (Multi Round-Trip Requests)**: in stateless mode the server can't send a request to the client, because
  the client's answer might arrive at a different server instance. So instead the tool **throws
  `InputRequiredException`**, which becomes an "incomplete" result listing what it needs. The client gathers the input
  (asks the user) and **retries the same `tools/call`** with the answers attached, plus any opaque `requestState` the
  server handed out. The state travels with the client, not in server memory. Think of it as a form sent back with
  "please fill in box 3 and resubmit".
- **Session modes** (`HttpServerTransportOptions.SessionMode`): `Stateless` (default since 2.0), `Stateful`, and
  `StatefulForInitializeClients` (hybrid: old clients get sessions, 2026-07-28 clients are served statelessly on the
  same endpoint). The old `bool Stateless` property still exists as shorthand, which is why Tool_Box's
  `Stateless = true` still compiles.
- **Why headers duplicate the body** (2026-07-28 requires `Mcp-Method` and `Mcp-Name` headers that repeat
  `method` and `params.name`): so load balancers, proxies and gateways can route without parsing the JSON body
  (`docs/concepts/tools/tools.md`). A YARP route could match on `Mcp-Name` the same way it matches on a path.

---

# PART B: The SDK's architecture

## 6. Packages, projects, and where the code comes from

**Rule first: five NuGet packages, stacked so you only take the dependencies you need. Core has the protocol,
transports, session engine, client and low-level server; the main package adds DI and hosting; AspNetCore adds HTTP.**

```
                         ┌───────────────────────────────┐   ┌──────────────────────────────┐
                         │ ModelContextProtocol.AspNetCore│   │ ...Extensions.Tasks / .Apps  │
                         │ MapMcp, WithHttpTransport,     │   │ optional protocol extensions │
                         │ StreamableHttpHandler, auth    │   └──────────────┬───────────────┘
                         └───────────────┬───────────────┘                  │
                                         ▼                                  ▼
                         ┌───────────────────────────────────────────────────────────┐
                         │ ModelContextProtocol   (main package)                      │
                         │ AddMcpServer, WithTools*, WithStdioServerTransport,        │
                         │ the hosted service that runs the server                    │
                         │ deps: Microsoft.Extensions.Hosting.Abstractions, Caching   │
                         └───────────────────────────┬───────────────────────────────┘
                                                     ▼
   ┌─────────────────────────────────────────────────────────────────────────────────────────────┐
   │ ModelContextProtocol.Core                                                                     │
   │ Protocol/ (DTOs) · transports · McpSessionHandler · McpClient · McpServer(Impl) · tools/prompts│
   │ deps: System.Text.Json, System.Threading.Channels, Microsoft.Extensions.AI.Abstractions,       │
   │       Microsoft.Extensions.Logging.Abstractions · ships the Analyzers DLL inside it             │
   └─────────────────────────────────────────────────────────────────────────────────────────────┘
```

| Folder in `src/` | Package? | What's in it |
|---|---|---|
| `ModelContextProtocol.Core/` | yes | `Protocol/` (≈150 DTO files, one per message type), `Client/`, `Server/`, `Authentication/` (OAuth client), `McpSessionHandler.cs` (the engine), `McpJsonUtilities.cs`, `Diagnostics.cs` (OpenTelemetry) |
| `ModelContextProtocol/` | yes | DI builder extensions, `McpServerOptionsSetup`, `SingleSessionMcpServerHostedService`, a distributed-cache event store |
| `ModelContextProtocol.AspNetCore/` | yes | `MapMcp`, `StreamableHttpHandler`, `StatefulSessionManager`, `SseHandler`, authorization filters, idle-session cleanup |
| `ModelContextProtocol.Extensions.Tasks/`, `.Apps/` | yes | The Tasks and Apps protocol extensions |
| `ModelContextProtocol.Analyzers/` | no (shipped inside Core) | A **source generator** that turns XML doc comments on `partial` tool methods into `[Description]` attributes |
| `Common/` | no | Files compiled into several projects: protocol-version constants, header names, SSE parsing, polyfills for `netstandard2.0` |

### The C6 trap: package ≠ assembly ≠ namespace

You flagged this layer yourself as fuzzy (TB 001). This repo is a perfect specimen, because it deliberately puts
types in **namespaces it doesn't own**:

| You write | Lives in **assembly** (the DLL) | Declared in **namespace** | Why that namespace |
|---|---|---|---|
| `builder.Services.AddMcpServer()` | `ModelContextProtocol.dll` | `Microsoft.Extensions.DependencyInjection` | So it shows up wherever you already have `using Microsoft.Extensions.DependencyInjection` (which ASP.NET Core imports implicitly). Zero extra `using` lines |
| `app.MapMcp()` | `ModelContextProtocol.AspNetCore.dll` | `Microsoft.AspNetCore.Builder` | Same trick, next to `MapGet` |
| `[McpServerTool]` | `ModelContextProtocol.Core.dll` | `ModelContextProtocol.Server` | Normal |
| `AIFunction`, `IChatClient` | `Microsoft.Extensions.AI.Abstractions.dll` | `Microsoft.Extensions.AI` | From another repo (dotnet/extensions) |

The three facts to say back:

- A **package** (`.nupkg`) is a zip used to *deliver* one or more assemblies. NuGet restores it; then it's gone from the
  picture.
- An **assembly** (`.dll`) is compiled IL. A project reference or package reference makes its public types *available*
  to the compiler.
- A **namespace** is only a name prefix. Any assembly can declare types in any namespace. `using X;` is a typing
  shortcut. **It grants no access and loads nothing.** If the assembly isn't referenced, no `using` can make the type
  appear.

**Where else code comes from:** `AIFunction`, `AIFunctionFactory` and `IChatClient` (Microsoft.Extensions.AI, built
in `dotnet/extensions`); DI, options, logging, hosting (`Microsoft.Extensions.*`, built in `dotnet/runtime`);
`MapPost`, `HttpContext` (ASP.NET Core's shared framework, `dotnet/aspnetcore`). If you find a bug in how JSON Schema
is generated from a C# parameter, it may live in `AIFunctionFactory` in dotnet/extensions, not here.

## 7. The cast of characters

Learn these names once; §9–§15 use them. Real type names are in the second column so you can search the source.

| Character | Real type (file) | Job in one line | Talks to |
|---|---|---|---|
| **The MCP Host** | Claude Desktop, VS Code, your agent | Runs the LLM loop; owns clients | Requester |
| **The Requester** | `McpClient` → `McpClientImpl` (`Client/`) | Client side: connects, probes the version, sends requests, exposes tools as Tool Cards | its Messenger, its Postmaster |
| **Tool Card** | `McpClientTool : AIFunction` | A server's tool wrapped so any `IChatClient` can hand it to an LLM | MCP Host's LLM |
| **The Stage Manager** | .NET Generic Host (`IHost`) | Starts and stops background services; owns DI, config, logging | Starter, Supply Room |
| **The Supply Room** | DI container (`IServiceProvider`) | Builds objects from registrations; owns their lifetimes | everyone |
| **The Starter** | `SingleSessionMcpServerHostedService` (`ModelContextProtocol/`) | A hosted service whose whole job is `await server.RunAsync()`, then stop the app | Concierge, Stage Manager |
| **The Messenger** | `ITransport` / `TransportBase`; `StdioServerTransport`, `StreamServerTransport`, `StreamableHttpServerTransport`; client: `StdioClientTransport`, `HttpClientTransport` | Turns bytes into `JsonRpcMessage` objects and back. Knows nothing about tools | Inbox, Postmaster |
| **The Inbox** | `Channel<JsonRpcMessage>`, exposed as `ITransport.MessageReader` | A thread-safe queue between the Messenger's read loop and the Postmaster | Messenger (writes), Postmaster (reads) |
| **The Postmaster** | `McpSessionHandler` (internal, `McpSessionHandler.cs`) | Reads the Inbox; starts one handling job per message; matches replies to waiting requests; handles cancellation. **Shared by client and server** | Inbox, Directory, Claim-Ticket Board, Stop Cords |
| **The Claim-Ticket Board** | `_pendingRequests: ConcurrentDictionary<RequestId, TaskCompletionSource<JsonRpcMessage>>` | One ticket per request *we sent* that's still waiting for its reply | Postmaster |
| **The Stop Cords** | `_handlingRequests: ConcurrentDictionary<RequestId, CancellationTokenSource>` | One cord per request *we're handling*; a cancel notification pulls it | Postmaster |
| **The Directory** | `RequestHandlers` (method name → handler) | `"tools/call"` → the function that handles it | Postmaster, Concierge |
| **The Concierge** | `McpServer` (abstract) → `McpServerImpl` (`Server/McpServerImpl.cs`, 2,560 lines) | Built from the Blueprint; fills the Directory; answers `server/discover`/`initialize`; advertises capabilities | Postmaster, Catalog |
| **The Blueprint** | `McpServerOptions` | Everything the Concierge is built from: catalogs, handlers, filters, capabilities, server info | Concierge |
| **The Catalog Builder** | `McpServerOptionsSetup` | Collects every registered tool/prompt/resource into the Blueprint's catalogs | Supply Room, Blueprint |
| **The Catalog** | `McpServerPrimitiveCollection<McpServerTool>` (`ToolCollection`) | Name → tool lookup; raises `Changed` | Concierge |
| **The Interpreter** | `McpServerTool` → `AIFunctionMcpServerTool` | Wraps one C# method: JSON args → C# parameters, fills special parameters, C# return value → `CallToolResult` | your method, Supply Room |
| **The Work Order** | `RequestContext<TParams>` | Everything about one request: params, the Return Address, scoped services, user | handlers, filters, tools |
| **The Return Address** | `DestinationBoundMcpServer` | A per-request view of the server that sends anything it emits (progress, elicitation) back down **the right** connection | tools |
| **The Checkpoints** | Request filters (`McpRequestFilter<TParams,TResult>` delegates); message filters (`McpMessageFilter`) | Wrap handlers for logging, auth, validation | Concierge builds them |
| **The Intake Desk** | `StreamableHttpHandler` (`AspNetCore/`) | ASP.NET Core endpoint: validates headers, makes (or finds) a server for this POST, streams the reply | ASP.NET Core, Concierge |
| **The Session Ledger** | `StatefulSessionManager` | Stateful HTTP only: session id → live session, idle cleanup | Intake Desk |

The single most important relationship: **the Messenger, the Inbox and the Postmaster are the same for client and
server.** `McpClientImpl` and `McpServerImpl` each create a `McpSessionHandler` with a flag `isServer`. The protocol
is symmetric, so the engine is too.

## 8. The layers

```
   your code:   [McpServerTool] methods ·  McpClient.CallToolAsync(...)  ·  filters
 ──────────────────────────────────────────────────────────────────────────────────────────────
   HOSTING        AddMcpServer / With*  (DI registrations)  ·  Starter (hosted service)  ·  MapMcp
 ──────────────────────────────────────────────────────────────────────────────────────────────
   SERVER/CLIENT  Concierge (McpServerImpl): Directory, Catalog, Interpreter, Checkpoints
                  Requester (McpClientImpl): version probe, typed methods (ListToolsAsync...)
 ──────────────────────────────────────────────────────────────────────────────────────────────
   SESSION        Postmaster (McpSessionHandler): read loop, dispatch, id correlation, cancellation,
                  OpenTelemetry spans and metrics
 ──────────────────────────────────────────────────────────────────────────────────────────────
   TRANSPORT      Messenger (ITransport) → Inbox (Channel)  ·  stdio / stream / Streamable HTTP / SSE
 ──────────────────────────────────────────────────────────────────────────────────────────────
   PROTOCOL       JsonRpcMessage + ~150 DTOs (Tool, CallToolRequestParams, ...) · System.Text.Json
                  source-generated serialization (McpJsonUtilities)
```

The contract between transport and session is tiny (`Protocol/ITransport.cs`, verified, comments removed):

```csharp
public interface ITransport : IAsyncDisposable
{
    string? SessionId { get; }
    ChannelReader<JsonRpcMessage> MessageReader { get; }   // incoming: the Inbox's read end
    Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default); // outgoing
}
```

That's the whole seam. Anything that can produce `JsonRpcMessage` objects into a channel and send them out is a
transport: a pipe, stdin, an HTTP POST, a WebSocket you might write. The session layer never knows which.

## 9. Startup: what the five famous lines actually do

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace); // stdout is the wire
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();
```

**Rule first: the `With*` calls only write entries into the Supply Room's list. Nothing is created until the Stage
Manager starts the Starter, and then one resolution pulls the whole object graph into existence.** Same two-phase
pattern as ASP.NET Core (register, then resolve), which you know from A001.

### Phase 1: registration (nothing exists yet)

| Call | What it adds to the service list (verified in `ModelContextProtocol/`) |
|---|---|
| `AddMcpServer()` | `AddOptions()`; registers `McpServerOptionsSetup` as an `IConfigureOptions<McpServerOptions>`; returns a `DefaultMcpServerBuilder`, which is just a wrapper holding the `IServiceCollection` so the next calls can chain |
| `.WithStdioServerTransport()` | `AddHostedService<SingleSessionMcpServerHostedService>()` (the Starter); a **singleton factory** for `McpServer` (`McpServer.Create(transport, options.Value, loggerFactory, services)`); a **singleton factory** for `ITransport` that builds a `StdioServerTransport` |
| `.WithToolsFromAssembly()` | Reflection: every type in the calling assembly with `[McpServerToolType]` → for every method with `[McpServerTool]` → `AddSingleton<McpServerTool>(factory)` where the factory calls `McpServerTool.Create(method, ...)` |

### Phase 2: `Build()`

Creates the container from the list. Still almost nothing constructed.

### Phase 3: `RunAsync()`: one resolution builds everything

```
 Stage Manager starts hosted services
   └─ needs the Starter ──► Starter's constructor needs McpServer
        └─ McpServer factory needs ITransport ──► new StdioServerTransport(...)
        │     └─ StreamServerTransport constructor: SetConnected(); _readLoopCompleted = Task.Run(ReadMessagesAsync)
        │        (the Messenger is now reading stdin in the background, writing into the Inbox)
        └─ McpServer factory needs IOptions<McpServerOptions>.Value
              └─ options system runs McpServerOptionsSetup.Configure(options)
                    └─ needs IEnumerable<McpServerTool> ──► runs every tool factory
                          └─ McpServerTool.Create(method) ──► AIFunctionFactory.Create(...)
                               builds a JSON Schema from the parameters + [Description]s (the Interpreter)
                    └─ puts them all in options.ToolCollection (the Catalog)
        └─ McpServer.Create(...) ──► new McpServerImpl(transport, options, ...)   (the Concierge)
              ConfigureInitialize · ConfigureDiscover · ConfigureTools · ConfigurePrompts · ConfigureResources ·
              ConfigureLogging · ConfigureCompletion · ConfigureSubscriptions · ConfigureMrtr · custom handlers
              → fills the Directory; builds the Checkpoint pipelines; creates the Postmaster
 Starter.ExecuteAsync ──► await server.RunAsync(stoppingToken) ──► Postmaster.ProcessMessagesAsync loop
```

### Shutdown (the other half, often skipped)

When the MCP Host closes the server's stdin, `ReadLineAsync` returns `null` (end of stream) → the read loop ends →
`SetDisconnected()` completes the Inbox → the Postmaster's `await foreach` finishes → it **waits for in-flight
handlers**, then fails any still-waiting Claim Tickets with `IOException("The server shut down unexpectedly.")` →
`RunAsync`'s `finally` disposes the server → the Starter's `finally` calls `lifetime.StopApplication()` → the Stage
Manager stops everything → the process exits. **That's why a stdio server dies when its client goes away**: the
end-of-stream on stdin is the shutdown signal.

## 10. One tool call, step by step (stdio)

**Rule first: a call is two one-way trips. On the way in, the server's Postmaster starts a handling job and looks up
the method in the Directory; on the way out, the client's Postmaster finds the Claim Ticket with the same id and
completes it.**

Setup: the MCP Host's `McpClient` calls `CallToolAsync("echo", new() { ["message"] = "Peter" })`. On the server,
`echo` is `public static string Echo([Description("the echoes message")] string message) => "hello " + message;`
(this is the real `EchoTool` in `tests/.../McpServerBuilderExtensionsToolsTests.cs`). The method name `Echo` became
the tool name `echo` because `DeriveName` strips an `Async` suffix and converts to `snake_case_lower`.

| # | Who | Does what | Hands what to whom |
|---|---|---|---|
| 1 | **Requester** (client) | Builds a `JsonRpcRequest { Method = "tools/call", Params = {name, arguments, _meta} }` | → client Postmaster |
| 2 | client **Postmaster** `SendRequestAsync` | Assigns id 7 (`Interlocked.Increment(ref _lastRequestId)`). Creates a `TaskCompletionSource` (the **Claim Ticket**) and pins it on the board under 7. Starts an OpenTelemetry span and injects trace context into the request | request → client **Messenger** |
| 3 | client **Messenger** | Serializes to one line of JSON + `\n`, writes it to the child process's **stdin** | bytes → OS pipe |
| 4 | client Postmaster | `await tcs.Task.WaitAsync(ct)`. **No thread is blocked**; the method is parked until someone completes the ticket | — |
| 5 | server **Messenger** read loop | `ReadLineAsync` returns the line → `JsonSerializer.Deserialize` → a `JsonRpcRequest` → `WriteMessageAsync` → `TryWrite` into the **Inbox** | message → Inbox |
| 6 | server **Postmaster** loop | `await foreach` gets it. Increments the in-flight count. Starts `ProcessMessageAsync()` **without awaiting it** (fire-and-forget) and immediately goes back to the Inbox | — |
| 7 | the handling job | Creates a linked `CancellationTokenSource` and hangs it on the **Stop Cords** under id 7. Then **forces a yield** (§11 explains why) | — |
| 8 | `HandleMessageAsync` | Starts a server span (parent = the client's trace context). Runs the **message filters**; the first built-in one reads `_meta` (protocol version, client capabilities) and validates it | → `HandleMessageCoreAsync` |
| 9 | `HandleRequestAsync` | Looks up `"tools/call"` in the **Directory**. Missing → `McpProtocolException(..., MethodNotFound)` | → the Directory entry |
| 10 | Directory entry (`SetHandler` wrapper) | Deserializes `params` into `CallToolRequestParams`. Because `ScopeRequests` is `true`, creates a **DI scope** for this request. Builds the **Work Order** `RequestContext<CallToolRequestParams>` with the **Return Address**, the params and the scoped services | Work Order → tool pipeline |
| 11 | tool pipeline (`ConfigureTools`) | Alternate-result filters (Tasks, ASP.NET auth) → `MatchTool` finds `"echo"` in the **Catalog** and sets `MatchedPrimitive` → ordinary **Checkpoints** (your call-tool filters) → base handler → `tool.InvokeAsync(...)` | Work Order → Interpreter |
| 12 | **Interpreter** `AIFunctionMcpServerTool.InvokeAsync` | Wraps the services in a `RequestServiceProvider` (which adds `McpServer`, `RequestContext`, `IProgress<...>`, `ClaimsPrincipal`). Copies JSON `arguments` into `AIFunctionArguments`. `AIFunction.InvokeAsync` binds `message` from JSON and calls your method | → `Echo("Peter")` |
| 13 | your method | Returns `"hello Peter"` | → Interpreter |
| 14 | Interpreter | `switch` on the return value: `string` → `CallToolResult { Content = [TextContentBlock("hello Peter")] }` | result → back up the pipeline |
| 15 | back out | Checkpoints run their "after" code. `SetHandler` stamps `resultType = "complete"` (2026-07-28 only). The DI scope is disposed | → `HandleRequestAsync` |
| 16 | `HandleRequestAsync` | `SendMessageAsync(new JsonRpcResponse { Id = 7, Result = ... })` → outgoing message filters (stamp server info) → server **Messenger** writes one line to **stdout** under a `SemaphoreSlim` send lock, flushes | bytes → OS pipe |
| 17 | handling job `finally` | Removes Stop Cord 7, disposes the CTS, decrements in-flight count | — |
| 18 | client Messenger → client Inbox → client Postmaster | It's a `JsonRpcResponse`, a "message with id" but not a request → `HandleMessageWithId` → removes ticket 7 from the board → `tcs.TrySetResult(response)` | completes the ticket |
| 19 | Requester | Step 4's `await` resumes; deserializes `CallToolResult`; returns it to the MCP Host | → the LLM |

Three things to notice:

- **The send lock (step 16).** Many handling jobs run at once and all write to one stdout. Without the
  `SemaphoreSlim`, two lines could interleave mid-JSON. One writer at a time per stream.
- **Steps 6–7 make the server concurrent.** The Postmaster never waits for a handler. Request 8 can start while 7 is
  still running. That's a feature (a slow tool doesn't block `tools/list`) and a responsibility (your tool code runs
  concurrently with itself; §12, §20).
- **The trace context rides inside the message** (steps 2 and 8). That's scouting item O (`Diagnostics.cs` +
  SEP-414): one distributed trace spanning client and server processes.

## 11. The Postmaster in depth: correlation, concurrency, cancellation

### 11.1 The Claim-Ticket Board is `TaskCompletionSource`

**Rule first: a `TaskCompletionSource<T>` is a `Task` that nobody runs; it completes when some other code calls
`SetResult` on it. That's exactly what "wait for a reply that will arrive later on another path" needs.**

```csharp
// SendRequestAsync (simplified from McpSessionHandler.cs)
var tcs = new TaskCompletionSource<JsonRpcMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
_pendingRequests[request.Id] = tcs;                 // pin the ticket
await SendToRelatedTransportAsync(request, ct);     // send
response = await tcs.Task.WaitAsync(ct);            // park until someone completes the ticket
...
finally { _pendingRequests.TryRemove(request.Id, out _); }   // always unpin

// HandleMessageWithId: a reply arrived (runs in the Postmaster's read path)
if (_pendingRequests.TryRemove(messageWithId.Id, out var tcs))
    tcs.TrySetResult(message);                      // completes the ticket → the parked await resumes
else
    LogNoRequestFoundForMessageWithId(...);         // a reply nobody's waiting for: log and drop
```

`RunContinuationsAsynchronously` matters: without it, `TrySetResult` would run the waiting method's continuation
**right there, on the Postmaster's thread**, and a slow continuation would stall the reading of every other message.
With it, the continuation is queued to the thread pool.

### 11.2 A concurrency timeline with the state after every step (F8)

Two calls at once: `slow_tool` (id 7, takes 10 s) and `echo` (id 8). Then the user cancels 7.

| t | Event | Client board (waiting) | Server cords (running) | Who can observe what now |
|---|---|---|---|---|
| 0 | Client sends 7 | {7} | {} | Server hasn't read it yet |
| 1 | Client sends 8 | {7, 8} | {} | — |
| 2 | Server reads 7, starts job, hangs cord | {7, 8} | {7} | A cancel for 7 would now find a cord |
| 3 | Server reads 8, starts job (doesn't wait for 7) | {7, 8} | {7, 8} | Both handlers running concurrently |
| 4 | Job 8 finishes, sends reply 8, removes cord | {7, 8} | {7} | Reply 8 in the pipe |
| 5 | Client reads reply 8, completes ticket 8 | {7} | {7} | **8 answered before 7**: order is by completion, not by sending |
| 6 | User cancels: client's `ct` fires → `RegisterCancellation` sends `notifications/cancelled {requestId: 7}` | {7} (the `await` throws `OperationCanceledException`; `finally` unpins) → {} | {7} | Client has stopped waiting; server doesn't know yet |
| 7 | Server reads the notification → finds cord 7 → `CancelAsync()` | {} | {7} (token now cancelled) | `slow_tool`'s token is signalled. **Whether it stops depends on the tool** |
| 8a | Tool checks its token / awaits something that honors it → `OperationCanceledException` | {} | {} | Job sees "user cancellation" and sends **no** error reply (the client isn't listening) |
| 8b | Tool ignores the token and finishes at t=10, sends reply 7 | {} | {} | Client gets a reply with no ticket → "no request found", dropped |

Two details the code gets right that are easy to get wrong:

- **The client registers for cancellation *after* sending** (comment in `SendRequestAsync`): if it sent
  `cancelled` first, the server could receive the cancel before the request and ignore it.
- **The server hangs the cord *before* yielding** (comment in `ProcessMessageAsync`): so a cancel arriving right
  behind the request always finds the cord, "even if the asynchronous processing happens out of order".
- `initialize` gets no cord at all: the spec says it must not be cancelled.

### 11.3 Why the forced yield (the deadlock it prevents)

```csharp
// If we await the handler without yielding first, the transport may not be able to read more messages,
// which could lead to a deadlock if the handler sends a message back.
await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
```

An `async` method runs **synchronously on the caller's thread until its first `await` that actually has to wait**.
`ProcessMessageAsync()` is called from inside the Postmaster's `await foreach` loop. If a handler ran a long way
synchronously, the loop couldn't read the next message until it hit a real wait. Now suppose the handler sends a
request to the other side (elicitation) and waits for the reply: the reply arrives in the Inbox, but the only code
that reads the Inbox is the loop that's stuck inside the handler. Each waits for the other: a deadlock. The forced
yield makes the handler give the thread back immediately and continue on the thread pool, so the loop is always free
to read.

```
 WITHOUT yield                                  WITH yield
 loop ─► handler (sync part) ─► sends request   loop ─► start handler ─► yield ─► loop reads next message
          waits for reply in Inbox ...                   handler continues on a pool thread
 loop can't read the Inbox (it's inside the     reply arrives → loop reads it → completes ticket → handler resumes
 handler) → reply never read → DEADLOCK
```

## 12. The HTTP path

```csharp
builder.Services.AddMcpServer()
    .WithHttpTransport(o => o.SessionMode = HttpServerSessionMode.Stateless)  // the default since 2.0
    .WithToolsFromAssembly();
app.MapMcp("/mcp");
```

**Rule first: in stateless mode, every HTTP POST gets its own brand-new Concierge (`McpServer`), Messenger and
Postmaster, which live exactly as long as that POST.** That one fact explains most HTTP behavior.

What `MapMcp` maps (verified in `McpEndpointRouteBuilderExtensions.cs`):

| Endpoint | Stateless | Stateful | Purpose |
|---|---|---|---|
| `POST {pattern}` | ✓ | ✓ | Every client message |
| `GET {pattern}` | ✗ not mapped | ✓ | Long-lived stream for unsolicited server messages |
| `DELETE {pattern}` | ✗ not mapped | ✓ | End a session |
| `GET /sse`, `POST /message` | ✗ (throws if you try) | only with `EnableLegacySse` | Legacy SSE |

The Intake Desk (`StreamableHttpHandler.HandlePostRequestAsync`), in order:

1. `Accept` must include **both** `application/json` and `text/event-stream`, else **406**.
2. Read and parse the body as one `JsonRpcMessage`; garbage → **400** with a JSON-RPC `InvalidRequest`.
3. Validate the `MCP-Protocol-Version` header, the `_meta` envelope, the `Mcp-Method`/`Mcp-Name` headers, and the
   required per-request `_meta`. Each failure → **400**, echoing the request's `id`.
4. Under 2026-07-28, methods the revision removed (`initialize`, `ping`, `logging/setLevel`,
   `resources/subscribe`/`unsubscribe`) → **404** `MethodNotFound`.
5. `GetOrCreateSessionAsync` → in stateless mode, `StartNewSessionAsync(serveStatelessly: true)`: a new
   `StreamableHttpServerTransport { Stateless = true }`, then `McpServer.Create(transport, options, ...,
   context.RequestServices)` with `ScopeRequests = false` (the HTTP request already has a DI scope, so reuse it), and
   `RunAsync(context.RequestAborted)`.
6. Hand the message to that transport; the reply (and any progress notifications) is written into the POST's
   response body as an SSE stream. Nothing written → **202 Accepted**.

### What "a server per request" means for your tool code

```
   POST #1 ─► Concierge A ─► EchoTool instance? (static: none)  ─► reply ─► A disposed
   POST #2 ─► Concierge B ─► ...                                 ─► reply ─► B disposed
                   both A and B resolve SINGLETONS from the same Supply Room ─► SHARED, CONCURRENT
```

- Anything stored *on the server object* is gone after the POST.
- Anything stored in a **singleton** (or a `static` field) is shared by every concurrent request. Kestrel serves
  POSTs in parallel, so two tool calls can touch it at the same moment. That's exactly the Voxel World finding in your
  Lecture 009: a singleton `Dictionary` mutated by concurrent tool calls is a **data race**, and `Stateless = true`
  promised scalability the state layer couldn't honor. Now you can see the mechanism in the SDK's own code.
- **Stateful mode** keeps one Concierge per session (the Session Ledger maps `Mcp-Session-Id` → session), so
  per-session state survives between POSTs, but every request in a session must reach the same instance (sticky
  sessions), and it's the legacy path for 2026-07-28 clients.

## 13. The client side

```csharp
var transport = new StdioClientTransport(new() { Name = "Everything", Command = "npx",
                                                  Arguments = ["-y", "@modelcontextprotocol/server-everything"] });
await using var client = await McpClient.CreateAsync(transport);   // launch + connect + version probe
IList<McpClientTool> tools = await client.ListToolsAsync();
var response = await chatClient.GetResponseAsync("...", new() { Tools = [.. tools] });  // tools ARE AIFunctions
```

- `IClientTransport` is a **factory**: `ConnectAsync()` returns the `ITransport` (the Messenger). For stdio, that's
  when the child process is launched. `StdioClientTransportOptions.InheritEnvironmentVariables` defaults to `true`,
  which hands every secret in your environment to a third-party server; `GetDefaultEnvironmentVariables()` gives a
  curated safe set.
- `McpClient.CreateAsync` → `McpClientImpl.ConnectAsync`: probe `server/discover` (or the HTTP header), fall back to
  `initialize` on failure (§4), then the Postmaster loop starts.
- **The bridge to LLMs:** `McpClientTool` inherits `AIFunction` from Microsoft.Extensions.AI. An `IChatClient`
  (OpenAI, Anthropic, Ollama...) sees a function with a name, description and JSON Schema; when the model asks to
  call it, `McpClientTool.InvokeAsync` sends `tools/call`. On the server side, the Interpreter is *also* an
  `AIFunction`. Same abstraction on both ends; MCP is the wire between them.
- `HttpClientTransport` with `HttpTransportMode.AutoDetect` (default) tries Streamable HTTP and falls back to SSE.

## 14. Errors: two kinds, and why it matters

**Rule first: a tool that fails returns a normal *result* with `isError: true`, so the model can read it and try
something else. A protocol failure (bad method, bad params) is a JSON-RPC *error* that the client code sees as an
exception.**

| What happened | What the server sends | What the client sees | Where in code |
|---|---|---|---|
| Your tool throws `InvalidOperationException("Test error")` | `result: { isError: true, content: [{text: "An error occurred invoking 'throw_exception'."}] }`. **Your message is hidden** | A `CallToolResult` with `IsError == true` | `McpServerImpl.CreateToolCallErrorResult` |
| Your tool throws `McpException("Quota exceeded")` | Same, but the text **includes** your message | Same | same; `McpException` = "this message is safe to show" |
| Your tool throws `McpProtocolException(..., InvalidParams)` | `error: { code: -32602, message }` | `McpProtocolException` thrown from `CallToolAsync` | rethrown, then `ProcessMessageAsync` builds a `JsonRpcError` |
| Unknown method | `error: { code: -32601 }` | exception | `HandleRequestAsync` |
| Non-MCP exception escaping a non-tool handler | `error: { code: -32603, message: "An error occurred." }` | exception | `ProcessMessageAsync` |
| The tool was cancelled by the client | nothing | (client already stopped waiting) | "user cancellation" check |

Why hide ordinary exception messages? They can leak internals (file paths, connection strings) to a remote client
and into an LLM's context. Throwing `McpException` is the explicit opt-in: "I wrote this message for the caller." The
test `Returns_IsError_Content_And_Logs_Error_When_Tool_Fails` pins this: the client gets "An error occurred", and the
full exception goes to the **log** instead.

## 15. Filters: the Checkpoints

**Rule first: a filter is a function that takes the next handler and returns a new handler that wraps it. The SDK
composes the list once, at Concierge construction, from last to first, so the first filter registered is the
outermost.** Same shape as ASP.NET Core middleware.

```csharp
.WithRequestFilters(f => f.AddCallToolFilter(next => async (context, cancellationToken) =>
{
    var sw = Stopwatch.StartNew();                       // before
    var result = await next(context, cancellationToken); // call the rest of the chain
    log.LogInformation("{Tool} took {Ms} ms", context.Params?.Name, sw.ElapsedMilliseconds);  // after
    return result;
}));
```

```csharp
// McpServerImpl.BuildFilterPipeline (verified): wrap from the inside out
var current = baseHandler;
for (int i = filters.Count - 1; i >= 0; i--)
    current = filters[i](current);       // filter i wraps everything registered after it
return current;                          // filter 0 is now outermost
// call order: f0 → f1 → f2 → handler → f2 → f1 → f0
```

Two levels: **message filters** (`WithMessageFilters`: see every raw JSON-RPC message, before routing) and **request
filters** (`WithRequestFilters`: per operation, with typed params and results). Tool calls have an extra outer ring
of "alternate-result" filters used by Tasks and authorization (`docs/concepts/filters.md`).

> 🔎 **A "verify against the code" moment.** `.github/copilot-instructions.md` shows `McpRequestFilter` as a *class*
> with an `InvokeAsync(RequestContext, Func<ValueTask> next)` method. In the code (`Server/McpRequestFilter.cs`) it's a
> **delegate**: `McpRequestHandler<TParams,TResult> McpRequestFilter<TParams,TResult>(McpRequestHandler<TParams,TResult>
> next)`. Instruction files drift. When docs and code disagree, the code wins, and that drift is itself a possible tiny
> docs contribution (check it's still wrong first).

---

# PART C: The .NET ideas underneath (the weak spots, made sharp)

These sections target what your teach-backs and questions have shown is fuzzy (learner model C6, C7) plus the
async topics you've said you want to own. Each starts with the one-line rule.

## 16. Library, framework, SDK: who calls whom

**Rule: with a library, your code calls it and it returns; with a framework, it owns the loop and calls your code.
The MCP C# SDK is a library that, once you hand it to a host, behaves like a framework for the lifetime of the
server.**

| Situation | Who owns the loop | Library or framework? |
|---|---|---|
| `McpClient.CreateAsync(...)`, then `CallToolAsync(...)` in your own `Main` | **You** | Library. You call, it returns |
| `McpServer.Create(transport, options)` then `await server.RunAsync()` yourself | The SDK's Postmaster loop, while you await it | Library call that *contains* a loop |
| `AddMcpServer().WithStdioServerTransport()` + `host.RunAsync()` | The **Stage Manager**, then the Postmaster. Your tool methods are called when messages arrive | Framework-like: inversion of control |
| `MapMcp()` inside ASP.NET Core | ASP.NET Core (a framework), which calls the Intake Desk per POST | The SDK is a plug-in to a framework |

**Where this generalization stops (F2):** "SDK" isn't a technical category. It's a product word for "the official
library (or libraries) for using a protocol from a language". It doesn't mean the SDK contains a framework or is one.
The inversion of control here comes from the **Generic Host** (and ASP.NET Core), not from anything MCP-specific.
`McpServer.Create` + `RunAsync` without any host works fine (the `InMemoryTransport` sample does that).

## 17. Attributes and reflection: labels don't do anything

**Rule: an attribute is inert metadata compiled into the assembly. It does nothing on its own. Some other code has
to look for it with reflection (at run time) or a source generator (at compile time) and act on it.**

```csharp
[McpServerToolType]                                   // a label on the class
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back.")]   // two labels on the method
    public static string Echo([Description("the message")] string message) => $"hello {message}";
}
```

| Label | Who reads it | When | What happens because of it |
|---|---|---|---|
| `[McpServerToolType]` | `WithToolsFromAssembly()` via `t.GetCustomAttribute<McpServerToolTypeAttribute>()` | Startup (registration) | The class is scanned for tools |
| `[McpServerTool]` | `WithTools(...)` via `GetCustomAttribute<McpServerToolAttribute>()` | Startup | A `McpServerTool` singleton is registered. Its `Name`, `Title`, `ReadOnly`, `Destructive`, `Idempotent`, `OpenWorld` properties feed the tool's metadata |
| `[Description]` | `AIFunctionFactory` (Microsoft.Extensions.AI) | When the tool is created | Becomes the `description` in the JSON Schema the **LLM** reads. **Descriptions are prompts**, which is why Tool_Box had a reflection test enforcing them |
| `[McpServerTool]` on a `partial` method with XML `///` docs | The **source generator** in `ModelContextProtocol.Analyzers` | **Compile time** | Generates the `[Description]` attributes for you |

Common C7 trap: "the attribute registers the tool." It doesn't. Delete `.WithToolsFromAssembly()` and the attribute
is still there, and no tool exists. Second trap: reflection over an assembly can't see types the trimmer removed,
which is why the non-generic `WithTools`/`WithToolsFromAssembly` are marked "might not work in Native AOT" and the
generic `WithTools<T>()` is the AOT-safe form (the `<T>` lets the compiler know which type to keep).

## 18. Dependency injection: lifetimes, scopes, and who disposes what

**Rule: registration writes recipes; resolution builds objects; the lifetime decides how long a built object is
shared; and the container disposes exactly the objects it created.**

| Lifetime | One object per... | MCP SDK examples |
|---|---|---|
| Singleton | application (container) | every `McpServerTool` (the Interpreter), `ITransport` and `McpServer` on stdio, `StreamableHttpHandler`, `StatefulSessionManager` |
| Scoped | scope. The SDK creates **one scope per request** when `ScopeRequests == true` (the default); in stateless HTTP it reuses the HTTP request's scope | your `DbContext`-like services used by tools |
| Transient | every resolution | `McpServerOptionsSetup` |

### The thing almost everyone gets wrong: instance tool methods

```csharp
[McpServerToolType]
public sealed class EchoTool(ObjectWithId objectFromDI)        // primary constructor: DI fills objectFromDI
{
    private readonly string _randomValue = Guid.NewGuid().ToString("N");
    [McpServerTool] public string Who() => _randomValue;       // instance method
}
```

The `McpServerTool` wrapping `Who` is a **singleton**, but **a new `EchoTool` instance is created for every
invocation** (`ActivatorUtilities.CreateInstance(request.Services, typeof(EchoTool))`, in
`McpServerBuilderExtensions.CreateTarget`). So:

- `_randomValue` is different on every call. **Fields on a tool class are not state.** If you want state across
  calls, inject a singleton service that holds it, and then you own its thread safety (§20).
- Constructor parameters are resolved from the **request's scope**, so a scoped service is fresh per call and
  disposed with the scope at the end of the request.

### Special parameters: the request-scoped extras

A tool method's parameters are split into two groups by `AIFunctionMcpServerTool.CreateAIFunctionFactoryOptions`:

```
 parameter type is a DI service, or one of the 4 "augmented" types?
    ├─ yes → ExcludeFromSchema = true; value comes from the Supply Room at call time
    │         augmented (RequestServiceProvider): McpServer · RequestContext<CallToolRequestParams> ·
    │                                             IProgress<ProgressNotificationValue> · ClaimsPrincipal
    └─ no  → appears in the JSON Schema the LLM sees; value comes from the JSON "arguments"
 (CancellationToken is bound specially by AIFunctionFactory and never appears in the schema either)
```

So `public static async Task<string> Fetch(string url, HttpClient http, IProgress<ProgressNotificationValue> p,
CancellationToken ct)` shows the LLM **one** parameter, `url`.

### Ownership and disposal (F10)

"Owns" means "is responsible for releasing". "Leak" means "acquired and never released", the C equivalent of
`malloc` on a path that never reaches `free`. In .NET, `Dispose`/`DisposeAsync` is the `free` for things the garbage
collector can't release on its own (threads, sockets, file handles, child processes).

| Rule | Where you'll see it |
|---|---|
| The container disposes what **it** created, when its scope (or itself) is disposed | request scope disposed in `InvokeScopedAsync`'s `finally` |
| `McpServerImpl` implements **only** `IAsyncDisposable`. A synchronous `using` on a provider that holds it **throws** | CONTRIBUTING: "Always `await using` the `ServiceProvider`" |
| `await using var client = ...` = call `DisposeAsync()` at the end of the block, even on exceptions | everywhere in tests |
| A transport whose read loop can never end leaks a thread pool thread | why tests must **never** use `WithStdioServerTransport()`: the test process's stdin can't be closed, so the loop never ends |

## 19. Delegates, lambdas, and functions that return functions

**Rule: a delegate is a typed reference to a method (plus, for lambdas, the variables it captured). A filter is a
function that *takes* a handler and *returns* a handler.**

The C look-alike (F1): a delegate is like a C function pointer **plus** a hidden context pointer (the target object
or the captured variables). Where it stops: delegates are type-checked, can't be null-called without a
`NullReferenceException`, and a lambda's captured variables are shared, not copied.

```csharp
public delegate ValueTask<TResult> McpRequestHandler<TParams, TResult>(RequestContext<TParams> request, CancellationToken ct);
public delegate McpRequestHandler<TParams, TResult> McpRequestFilter<TParams, TResult>(McpRequestHandler<TParams, TResult> next);
```

Reading `next => async (context, ct) => { ...; await next(context, ct); ... }` slowly:

```
 next => ...                         a function with ONE parameter, next (the rest of the chain)
          async (context, ct) => {}  ...that RETURNS another function: the new handler
 inside the new handler, "next" is a CAPTURED variable: each wrapped handler remembers its own next
```

Expanded into ordinary methods, the same filter is:

```csharp
McpRequestHandler<CallToolRequestParams, CallToolResult> MyFilter(McpRequestHandler<CallToolRequestParams, CallToolResult> next)
{
    return Handler;                                       // return a function...
    async ValueTask<CallToolResult> Handler(RequestContext<CallToolRequestParams> context, CancellationToken ct)
    {
        // before
        var result = await next(context, ct);             // ...that calls the function it was given
        // after
        return result;
    }
}
```

## 20. Async: Task, await, TaskCompletionSource, Channel, cancellation

**Rule: `await` pauses a method without holding a thread; the thread goes back to the pool, and the method resumes
(possibly on a different thread) when the awaited task completes. Nothing here creates a thread per request.**

| Thing | What it is | Where in the SDK | What it does NOT do |
|---|---|---|---|
| `Task` / `Task<T>` | A promise of a result that may complete later | everywhere | Doesn't mean "a thread". Most tasks here are I/O waits with no thread at all |
| `await` | "If not done, register the rest of this method as a continuation and return" | everywhere | Doesn't create threads. Doesn't make code thread-safe |
| `ValueTask<T>` | A cheaper `Task` for results that are often available synchronously | handler delegates (`McpRequestHandler`) | Can't be awaited twice |
| `ConfigureAwait(false)` | "Don't resume on the captured context" | on almost every `await` in `src/` | Doesn't affect correctness in ASP.NET Core / console apps (no context); it's library hygiene for UI apps that *have* one |
| `TaskCompletionSource<T>` | A task you complete by hand | Claim Tickets (§11.1); `allHandlersCompleted` | Doesn't run anything |
| `Channel<T>` | Async producer/consumer queue | the Inbox (`TransportBase`) | **Unbounded** here: it never makes the writer wait, so it gives no backpressure by itself |
| `Task.Run(f)` | Queue `f` to the thread pool | the Messenger's read loop | Not a dedicated thread |
| fire-and-forget `_ = F()` | Start without awaiting | each message handler; list-changed notifications | Exceptions aren't observed unless caught inside `F` (that's why `ProcessMessageAsync` catches everything) |
| `CancellationToken` | A read-only "please stop" flag you pass down | every async method | **Doesn't stop anything.** Code must check it or pass it to something that does |
| `CancellationTokenSource.CreateLinkedTokenSource(a)` | A new source that also fires when `a` fires | Stop Cords: linked to the session's shutdown token | — |
| `Interlocked.Increment` | Atomic `++` | request ids, in-flight count | — |
| `ConcurrentDictionary` | A thread-safe map | Claim-Ticket Board, Stop Cords, Catalog lookups | Doesn't make a read-modify-write of *your* values atomic |

The Inbox has a genuine firmware look-alike, so here it is, bounded (F13): it's the classic **ISR-fills-a-ring-buffer,
main-loop-drains-it** shape. The Messenger's read loop is the producer, the Postmaster's `await foreach` is the
consumer, and the channel decouples their speeds. Where it stops: the channel is **unbounded** (it grows on the heap
instead of dropping or overwriting), `SingleWriter = false` because several code paths can write, and the consumer
doesn't poll; it's woken by a continuation.

### Your tool code runs concurrently with itself

Because of the fire-and-forget dispatch (§10 steps 6–7) and Kestrel's parallel request handling (§12), two calls to
the same tool can be executing at the same instant. Any shared mutable state they touch (singleton fields, statics)
needs synchronization (`lock`, `SemaphoreSlim`, `ConcurrentDictionary`, or immutable snapshots). This is the
interview-grade version of the Voxel story: *the SDK guarantees concurrency; it does not guarantee your state's
safety.*

## 21. Compact-syntax decoder (F9)

The repo uses `LangVersion=preview`, so you'll meet the newest C#. Each line: what you'll see → what it means.

| You'll see | Means (expanded) |
|---|---|
| `toolAssembly ??= Assembly.GetCallingAssembly();` | `if (toolAssembly == null) toolAssembly = Assembly.GetCallingAssembly();` |
| `options?.Name ?? DeriveName(method)` | `(options != null ? options.Name : null)`, and if that is null, `DeriveName(method)` |
| `if (x is { } y)` | `if (x != null) { var y = x; ... }` (pattern: "not null, and call it y") |
| `if (message is JsonRpcRequest { Method: "initialize" } r)` | `if (message is JsonRpcRequest r && r.Method == "initialize")` |
| `result switch { string text => A, null => B, _ => C }` | `if (result is string text) A; else if (result == null) B; else C;` as an expression that produces a value |
| `Tools = [new() { Name = "x" }]` | collection expression: `new List<Tool> { new Tool { Name = "x" } }` (the target type decides the collection) |
| `[.. tools]` | spread: a new collection containing all elements of `tools` |
| `public sealed class EchoTool(ObjectWithId objectFromDI)` | primary constructor: the parameter is in scope for the whole class (DI fills it) |
| `get; set { ...; field = value; } = TimeSpan.FromSeconds(5);` | C# 14 `field` keyword: the compiler-generated backing field, so a property can validate without declaring `_x` (see `DiscoverProbeTimeout`) |
| `"\n"u8` | a UTF-8 byte span literal (bytes, not a `string`) |
| `"""{"jsonrpc":"2.0"}"""` | raw string literal: no escaping needed inside |
| `#if NET ... #else ... #endif` | compile one branch for modern .NET, the other for `netstandard2.0` (§22) |
| `static r => CreateTarget(...)` | a lambda that captures nothing (`static` forbids captures, so no hidden allocation) |
| `internal sealed partial class` | `partial`: the class is split across files; here the other half is source-generated logging (`[LoggerMessage]`) |

## 22. JSON source generation, AOT, multi-targeting

- **System.Text.Json source generation.** Instead of discovering properties by reflection at run time, a source
  generator writes serialization code at compile time. That's why you see `McpJsonUtilities.JsonContext.Default.CallToolResult`
  (a `JsonTypeInfo<T>`) passed everywhere. Benefits: faster startup, and it works under **Native AOT**, where the
  trimmer removes code reflection would need. CI runs a `test-aot` step in Release builds. **Rule for contributors:** a
  new protocol type needs a `[JsonSerializable]` entry, or AOT breaks.
- **Multi-targeting.** Core and the main package build for `net10.0; net9.0; net8.0; netstandard2.0`; AspNetCore for
  `net10.0; net9.0; net8.0` only. One project, four compilations. `netstandard2.0` lacks newer APIs, hence
  `src/Common/Polyfills/` and `#if NET` branches (for example, `StreamServerTransport` uses `StreamReader` on .NET and a
  `CancellableStreamReader` on netstandard). **Rule:** a change that compiles on net10.0 can still fail on
  netstandard2.0. Build the whole project, not one target.

## 23. Versioning, compatibility, and the diagnostics you'll see

| Mechanism | What it is | Why you care as a contributor |
|---|---|---|
| **SemVer** (`docs/versioning.md`) | MAJOR = breaking, MINOR = additive, PATCH = fixes | A public-API removal or behavior break needs a MAJOR. Keep first PRs behavior fixes or tests |
| **Package validation** (`PackageValidationBaselineVersion` = 2.0.0) | The build compares the public API with 2.0.0 | Accidentally breaking API fails the build; `CompatibilitySuppressions.xml` records accepted ones |
| `[Experimental("MCPEXP00x")]` | API may change any time; callers must suppress the warning to use it | Experimental APIs can change in MINOR/PATCH |
| `[Obsolete(..., DiagnosticId = "MCP900x")]` | Deprecated but working: `MCP9004` legacy SSE, `MCP9005` sampling/roots/logging, `MCP9006` stateful-HTTP knobs | Inside `src/` you'll see `#pragma warning disable MCP9006` where the SDK still implements the legacy path |
| Protocol-era gates | `McpProtocolVersions.IsJuly2026OrLaterProtocolVersion(...)` and friends | Many bug fixes are "this behavior should only apply to one protocol era" (for example #1721: don't stamp `resultType` for 2025-11-25 clients) |
| `TreatWarningsAsErrors=true` | Every warning fails the build | An unused variable or a missing XML doc on a public member is a red build |

---

# PART D: Working in the repo

## 24. Build and test (documented; not yet run on your machines)

| Need | Detail |
|---|---|
| .NET SDK | 10.0 (`global.json`: `10.0.101`, `rollForward: minor`). CI also installs 9.0 |
| Node.js 22 | `npm ci` installs pinned packages for integration and **conformance** tests (`@modelcontextprotocol/conformance`) |
| Docker | Optional: some tests (the "everything" reference server) skip without it |
| Commands | `dotnet build` · `dotnet test` · `dotnet test tests/ModelContextProtocol.Tests/` · or `make build` / `make test` (what CI runs) · `make serve-docs` → DocFX site on `localhost:8080` |
| CI | GitHub Actions `ci-build-test.yml`: **ubuntu / windows / macos × Debug / Release**, then an AOT test in Release, then pack. A PR's CI only runs if code paths changed |

Folder map (top level):

```
csharp-sdk/
├── src/            5 packages + Analyzers + Common                         (§6)
├── tests/          ModelContextProtocol.Tests (core, in-process) · AspNetCore.Tests (HTTP, in-memory Kestrel)
│                   Analyzers.Tests · Conformance{Client,Server} · Test{,Sse,OAuth}Server (helper processes)
│                   AotCompatibility.TestApp · ExperimentalApiRegressionTest · Common/Utils (shared helpers)
├── samples/        13 runnable apps: QuickstartWeatherServer, QuickstartClient, AspNetCoreMcpServer,
│                   InMemoryTransport, ProtectedMcpServer/Client (OAuth), EverythingServer, TasksExtension ...
├── docs/           DocFX conceptual docs (concepts/*), versioning, roadmap, list of diagnostics
└── .github/        copilot-instructions.md (conventions; read it), workflows/, skills/ + agents/ (the
                    maintainers drive releases and triage with AI agents), release-process.md
```

## 25. How the tests are built (F12: the object graph first)

**Rule: before reading any test, ask "what's real, what's replaced, and which of the three moves (arrange, act,
assert) is this line?"**

`Can_Call_Registered_Tool` (`tests/ModelContextProtocol.Tests/Configuration/McpServerBuilderExtensionsToolsTests.cs`):

```
   TEST PROCESS (everything in one process, no network, no child process)
   ┌──────────────────────────────────────────────────────────────────────────────────────────────┐
   │  McpClient (REAL)                                                    McpServer (REAL)          │
   │   └ StreamClientTransport (REAL) ─ writes ─► Pipe A (in-memory) ─► StreamServerTransport (REAL)│
   │                                 ◄─ reads ── Pipe B (in-memory) ◄─                             │
   │  REPLACED: the OS pipes/stdin/stdout → System.IO.Pipelines.Pipe                                │
   │  REAL:     Postmasters, Directory, Catalog, Interpreter, EchoTool.Echo, JSON serialization     │
   │  LOGGING:  XunitLoggerProvider (to test output) + MockLoggerProvider (captures for asserts)    │
   └──────────────────────────────────────────────────────────────────────────────────────────────┘
```

| Move | Code | Where |
|---|---|---|
| **Arrange** | `ConfigureServices(...)` registers `.WithTools<EchoTool>()`; the base class wires `WithStreamServerTransport(pipeA.Reader, pipeB.Writer)`, builds the provider with `validateScopes: true`, resolves `McpServer` and starts `RunAsync` | `ClientServerTestBase` constructor |
| **Arrange** | `await using McpClient client = await CreateMcpClientForServer();` (raises the discover-probe timeout to 60 s to stop CI flakes, #1701) | test body |
| **Act** | `client.CallToolAsync("echo", { ["message"] = "Peter" }, ct)` | test body |
| **Assert** | result not empty; first block is `TextContentBlock` with `"hello Peter"` and `Type == "text"` | test body |

This is a **fake, not a mock**: the pipe is a working replacement for stdin/stdout. Nothing is scripted or verified.
(A *mock* would be told "when called with X, return Y" and then checked "was I called?". The repo uses Moq for that in
a few places.)

Which test transport to pick (from CONTRIBUTING, verified):

| Testing... | Use | Why |
|---|---|---|
| DI registration only | `WithStreamServerTransport(Stream.Null, Stream.Null)` | No threads blocked |
| Client and server together | inherit `ClientServerTestBase` | Full protocol in-process |
| Client logic only | `TestServerTransport` | Auto-answers standard requests |
| HTTP | inherit `KestrelInMemoryTest` | Real ASP.NET Core stack, no ports |
| Real process lifecycle | `StdioClientTransport` | Only then |

House rules that will be checked in review: `TestConstants.DefaultTimeout` (60 s), never hard-coded short timeouts;
**never `Task.Delay` to wait for something**, use `TaskCompletionSource`/`SemaphoreSlim`/`Channel` signals; pass
`TestContext.Current.CancellationToken` (xUnit v3); `[Collection(nameof(DisableParallelization))]` for tests that
touch global state like `ActivitySource` listeners. And the copilot-instructions rule: build and test before calling
anything done.

## 26. Contributing to this repo

From `CONTRIBUTING.md` and `.github/copilot-instructions.md` (read 2026-10-04):

- First-timer issues are labeled `good first issue`; maintainers also use `help wanted` and `ready for work`.
- **No issue, no PR**: open an issue for anything not already tracked, and for all but the smallest changes, post your
  approach on the issue before coding. **Assign yourself** so others know.
- A PR needs: style conformance (`.editorconfig`), **tests for every fix or feature**, all tests passing locally,
  error handling, docs updated.
- **AI disclosure:** the repo's own AI instructions say content posted to GitHub with AI help must carry a visible
  note (for example a `> [!NOTE]` at the bottom). That matches your own rule to disclose.
- License: Apache 2.0; contributions are under it.
- Releases are cut by maintainers using AI agent skills (`prepare-release`, `publish-release`), with human gates.

The stage ladder for any contribution here (C5):

```
 noticed ─► reproduced (a failing test) ─► approach agreed on the issue ─► fixed ─► red→green proven ─►
 all 3 OSes × 2 configs green in CI ─► reviewed ─► merged ─► shipped in a release
```

Scouting items from September, **status not re-checked today** (GitHub API access wasn't available in this
session; check the threads before acting): **E** #1806 (flaky OAuth metadata timeout on Windows; the fix pattern is
the 60-second override you saw in `ClientServerTestBase`), **H** #1781 (doc/sample for integration-testing a Streamable
HTTP server; `KestrelInMemoryTest` is the in-repo pattern to mirror), **O** (study `Diagnostics.cs` + SEP-414
distributed tracing).

## 27. Tool_Box (SDK 1.x, July 2026) vs the SDK today (2.x)

| You did / learned in Tool_Box | What it actually was | What's different in 2.x |
|---|---|---|
| `AddMcpServer().WithStdioServerTransport()` | §9: registrations; the Starter runs the server | Same API |
| stderr-only logging, "stdout = protocol" | §5: the Messenger parses every stdout line as JSON | Same |
| Inspector handshake failure (missing .NET 10 runtime) | The `initialize` handshake never completed because the process couldn't start | Clients now probe `server/discover` first, then fall back to `initialize` |
| `Stateless = true` on HTTP | §12: a new server per POST | Stateless is now the **default**; `SessionMode` is the real setting; `Stateless` is shorthand |
| `AllowedHosts` DNS-rebinding pin (ADR-008) | Host-header validation | The SDK docs now say the same thing prominently (getting-started, transports) |
| Integration tests with the SDK's client on ephemeral ports | Real Kestrel + real HTTP client | The repo itself uses `KestrelInMemoryTest`: in-memory, no ports |
| Server-to-client features over HTTP needed sessions | Stateful sessions | MRTR lets elicitation work statelessly with 2026-07-28 clients; sampling/roots/logging are deprecated |
| "Three-round API-drift saga → read the docs first" | 1.x pre-release churn | 2.0 is stable SemVer with package validation, so drift now happens only via `[Experimental]` APIs |

---

## Common mistakes

| Mistake | What actually happens | Fix |
|---|---|---|
| Writing to stdout in a stdio server (`Console.WriteLine`, default console logger) | The client tries to parse it as JSON-RPC; the connection breaks or messages get lost | Log to stderr |
| Assuming fields on a tool class persist between calls | A new instance is made per invocation | Inject a singleton service for state |
| Assuming a singleton used by tools is safe because "each request is independent" | Concurrent requests share it | Synchronize, or make it immutable |
| Throwing plain exceptions with useful messages from tools | The client sees "An error occurred invoking 'x'." | Throw `McpException` for messages meant for the caller |
| Expecting cancellation to stop a tool | It sets a flag; the tool decides | Pass `CancellationToken` to every async call; check it in loops |
| `using` (sync) on a provider holding the server | Throws: `McpServerImpl` is async-only disposable | `await using` |
| `WithStdioServerTransport()` in a unit test | Leaks a thread per test | `WithStreamServerTransport(Stream.Null, Stream.Null)` or `ClientServerTestBase` |
| `Task.Delay(100)` to "wait for" something in a test | Flaky on slow CI | Signal with `TaskCompletionSource` |
| Hard-coded 5 s timeouts in tests | Flaky on Windows CI (the #1701 / #1806 family) | `TestConstants.DefaultTimeout` |
| Calling `ElicitAsync` in a stateless HTTP server | `InvalidOperationException` ("not supported in stateless mode") | MRTR: throw `InputRequiredException`, check `server.IsMrtrSupported` |
| Believing `using ModelContextProtocol.Server;` "adds" the SDK | It adds nothing; the package reference does | Know package vs assembly vs namespace (§6) |
| Changing code that compiles on net10.0 only | netstandard2.0 build fails | Build all targets; use `#if NET` / polyfills |

## Interview relevance

- **"Design a request/response protocol over a single bidirectional stream."** §11 is the textbook answer: request ids,
  a pending-request map of promises (`TaskCompletionSource`), a single reader loop, per-message dispatch, a send lock,
  cancellation by id, and failing pending requests on disconnect. Same design as gRPC streams, LSP, Chrome DevTools
  Protocol and database wire protocols.
- **Stateless vs stateful services**: why removing the handshake and session id enables horizontal scaling, what
  you give up (unsolicited push, server-initiated requests), and how MRTR moves state to the client (like a
  continuation token).
- **Backpressure**: why Streamable HTTP has it and legacy SSE doesn't.
- **Error design**: results-with-`isError` for the model vs protocol errors for the program; not leaking exception
  messages across a trust boundary.
- **Async internals**: why the forced yield prevents a deadlock; `RunContinuationsAsynchronously`; why
  `CancellationToken` is cooperative.
- **AI engineering**: how tool schemas are generated from code, why descriptions are prompts, how `AIFunction`
  unifies tools across LLM providers.

## Real-world production usage

- The SDK is co-maintained by Microsoft and the MCP project; it's what .NET MCP servers are built on, including
  Microsoft's own (the transports doc's examples use the NuGet MCP Server, launched with `dnx NuGet.Mcp.Server`), and
  there's an official `dotnet new` MCP server template.
- Production HTTP servers run **stateless** behind load balancers, with OAuth (`ProtectedMcpServer` sample),
  `AllowedHosts`, restrictive CORS only if browsers must call it, and OpenTelemetry (ActivitySource and Meter named
  `Experimental.ModelContextProtocol`).
- Clients embed `McpClient` in agents: list tools once, pass them to an `IChatClient`, and let the model call them.

## Check yourself

1. A stdio server prints a startup banner with `Console.WriteLine`. What breaks, and in which component?
2. Request 9 is sent, then request 10. Reply 10 arrives first. Walk through what each Postmaster does.
3. Why does the client register its cancellation callback *after* sending the request?
4. Your tool class has a field `int _count` incremented per call. What values will callers see, and why?
5. In stateless HTTP mode, how many `McpServer` objects exist during 50 concurrent POSTs?
6. A tool throws `IOException("disk full at /var/secret/db")`. What does the client receive? How would you make
   "disk full" visible but not the path?
7. Which line of code makes a `[McpServerTool]` method become a tool? What happens if you remove it?
8. What does `next => async (ctx, ct) => await next(ctx, ct)` do, and in what order do three such filters run?
9. Why is `server/discover` a probe with a timeout, and what does the client do if it times out?
10. Name what's real and what's replaced in `Can_Call_Registered_Tool`.

## Teach-back checklist

Say these back in your own words (start with what each thing is *for*):

1. **What MCP is for:** a JSON-RPC contract so any AI app (MCP host, via a client) can discover and call any tool
   server; the SDK does the plumbing on both ends. "Host" in MCP ≠ the .NET Generic Host.
2. **The four message shapes**, and that replies are matched to requests only by `id`.
3. **Then vs now:** `initialize` handshake + sessions (≤ 2025-11-25) vs `server/discover` + per-request `_meta`, no
   sessions (2026-07-28), and the client's probe-then-fall-back.
4. **The layers and the seam:** protocol DTOs → transport (`ITransport`: a channel in, `SendMessageAsync` out) →
   session engine (the Postmaster, shared by client and server) → server/client → hosting.
5. **Startup:** `With*` only registers; `RunAsync` resolves the Starter, which pulls in the transport, the options (the
   Catalog Builder runs every tool factory), and the Concierge, which fills the Directory; stdin EOF shuts it all down.
6. **One tool call:** id + Claim Ticket → line on stdin → Inbox → fire-and-forget job + Stop Cord → Directory → scope +
   Work Order → Checkpoints → Interpreter binds JSON args and DI params → your method → `CallToolResult` → line on
   stdout → ticket completed.
7. **Concurrency and cancellation:** handlers run concurrently; replies can come back out of order; cancellation is a
   notification that pulls a cord, and the tool must cooperate; the forced yield prevents a deadlock.
8. **HTTP stateless:** a new server per POST, scoped to the request; singletons and statics are shared across
   concurrent requests (the Voxel race).
9. **Errors:** tool failures are `isError` results (messages hidden unless `McpException`); protocol failures are
   JSON-RPC errors.
10. **The .NET underneath:** attributes are inert until reflection reads them; package ≠ assembly ≠ namespace and
    `using` grants nothing; tool classes are instantiated per call; `await` holds no thread; `CancellationToken`
    stops nothing by itself.

## References (all read 2026-10-04 at `c40ee04` unless noted)

- `src/ModelContextProtocol.Core/McpSessionHandler.cs` (the Postmaster), `Protocol/ITransport.cs`,
  `Protocol/TransportBase.cs`, `Server/StreamServerTransport.cs`, `Server/StdioServerTransport.cs`
- `src/ModelContextProtocol.Core/Server/McpServerImpl.cs` (constructor, `ConfigureTools`, `SetHandler`,
  `InvokeHandlerAsync`, `BuildFilterPipeline`, `CreateToolCallErrorResult`), `Server/AIFunctionMcpServerTool.cs`,
  `Server/RequestServiceProvider.cs`
- `src/ModelContextProtocol/McpServerServiceCollectionExtensions.cs`, `McpServerBuilderExtensions.cs`,
  `McpServerOptionsSetup.cs`, `SingleSessionMcpServerHostedService.cs`
- `src/ModelContextProtocol.AspNetCore/McpEndpointRouteBuilderExtensions.cs`, `StreamableHttpHandler.cs`
- `src/Common/McpProtocolVersions.cs`, `src/Common/McpHttpHeaders.cs`
- `docs/concepts/`: getting-started, transports, stateless, mrtr, filters, tools; `docs/versioning.md`
- `CONTRIBUTING.md`, `.github/copilot-instructions.md`, `.github/workflows/ci-build-test.yml`
- Tests: `tests/ModelContextProtocol.Tests/ClientServerTestBase.cs`,
  `Configuration/McpServerBuilderExtensionsToolsTests.cs`
- MCP specification: https://modelcontextprotocol.io/specification/
