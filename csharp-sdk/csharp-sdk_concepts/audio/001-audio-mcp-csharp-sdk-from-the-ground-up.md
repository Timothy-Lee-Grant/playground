# The MCP C# SDK From the Ground Up: The Protocol, the Machinery, and the .NET Ideas Underneath

Audio lecture 001 for the MCP C# SDK project. Written on October 4, 2026, to be listened to as a NotebookLM Audio Overview. It's an orientation to the Model Context Protocol C# SDK, the open-source library in the GitHub repository modelcontextprotocol slash csharp-sdk, plus the .NET ideas you need to read its code and contribute to it.

Facts in this episode were checked by reading the SDK's source on its main branch at commit c40ee04, from September 18, 2026. The latest release at that point was version 2.2.0, from August 13, 2026. None of the build or test commands mentioned here have been run on the listener's own machines yet, so treat those parts as what the repository documents, not as something that's been tried.

## Who this is for and what it covers

This episode is for an engineer who has already built something with this SDK. In July 2026 you built Tool_Box, an MCP server in C#, using exactly the calls this episode explains: Add M-C-P Server, With Stdio Server Transport, With Tools From Assembly, and the stateless flag for HTTP. It worked. And that's the problem this episode is aimed at. You've said yourself that fuzzy understanding is the dangerous kind, because when something works you never notice the gap. So the goal here isn't to show you how to use the SDK. It's to show you what those calls actually do, who calls whom, in what order, and why it's built that way.

There's a second reason. Eleven days after your first Tool_Box plan, the SDK jumped from version 1 to version 2.0, on July 28, 2026. The protocol itself changed underneath it. Part of what you learned in July is now what the SDK calls the legacy path. This episode will say clearly what changed.

And there's a third goal, as important as the first two. A handful of general .NET ideas sit underneath every file in this repository, and so far you've been able to get by with a fuzzy version of them: what an attribute actually does, the difference between a package, an assembly and a namespace, dependency injection lifetimes and who disposes what, delegates and functions that return functions, and how async code really works, including a special kind of task called a task completion source. This episode makes those sharp.

Here's the key idea for the whole episode, in one sentence. **MCP is a contract that lets any AI application call any tool server using JSON-RPC messages, and the C# SDK is the library that does the plumbing on both ends: on the server it turns your labeled C# methods into tools, reads messages off a transport, routes each one to a handler and writes the answer back; on the client it sends requests and matches each reply to the request that's waiting for it.**

The episode has twelve parts. It's long, so it's meant to be heard as three episodes made from this same source. Episode one is parts one to four: what MCP is for, the message format, what servers offer, and how a conversation starts and travels. Episode two is parts five to eight: where the SDK's code comes from, the cast of characters, startup, and one tool call traveling through the whole machine, including the engine at its center. Episode three is parts nine to twelve: the HTTP path, errors and filters, the .NET ideas underneath, and then the repository, its tests, how to contribute, the common misconceptions, and a final recap.

## Part one: what MCP is for

Start with the problem. Before MCP, every AI application had its own way of plugging in outside tools. Claude Desktop had one format, a code editor had another, an agent framework had a third. And every company that wanted to offer a tool, say access to GitHub or to a Postgres database, had to write one adapter for each application. With N applications and M tools, that's N times M adapters, and most of them never got written.

MCP, the Model Context Protocol, fixes that the same way HTTP fixed it for the web. It defines one shared contract for the messages. Each application implements the client side once. Each tool provider implements the server side once. N plus M implementations instead of N times M adapters. The key idea of this part: **MCP standardizes how an AI application discovers and calls outside capabilities, so either side can be swapped without the other knowing.**

The protocol names three roles. The **host** is the AI application the user talks to, like Claude Desktop, VS Code, or the LangGraph agent in your LLM_Monitor project. The host runs the conversation with the language model. The **client** is one connection from that host to one server. A host can hold several clients, one per server. In this SDK, the client is a class called M-C-P Client. And the **server** is a program that offers tools and other capabilities. Tool_Box is a server. Anything you build with Add M-C-P Server is a server.

Now a warning about a word, because it will trip you up in this repository. In MCP, the word "host" means the AI application. In .NET, the word "host" means something completely different: the Generic Host, the object you create with Host dot Create Application Builder, which owns dependency injection, configuration, logging, and background services. An MCP server written in C# runs inside a .NET host. Same word, two meanings, often in the same file. In this episode, when we mean the AI application, we'll say "the MCP host". And we'll give the .NET Generic Host a character name you may have heard before: **the Stage Manager**.

Let's also say what MCP does not do, because it's tempting to give a protocol more credit than it deserves. MCP doesn't run the language model; the MCP host does. A server never sees the conversation unless the host chooses to send it a piece. MCP doesn't decide when to call a tool; the model proposes a call and the host carries it out through a client. And MCP isn't a security system. Over HTTP it borrows OAuth. Over a local process connection, the security boundary is simply who is allowed to launch the process.

Recap of part one. MCP is a shared contract between AI applications and tool servers, which turns N times M adapters into N plus M implementations. The roles are host, client and server. "Host" in MCP means the AI application; in .NET it means the Generic Host, which we'll call the Stage Manager. MCP doesn't run the model, doesn't decide when tools are called, and isn't an auth system.

## Part two: JSON-RPC, the message format underneath

MCP doesn't invent its own message format. It uses an existing one called JSON-RPC, version 2.0. RPC stands for remote procedure call: calling a function in another program as if it were local. JSON-RPC says each message is a small JSON object, and there are exactly four shapes.

The first shape is a **request**. It has a method name, like "tools slash call", some parameters, and an **id**, which is just a number or a string chosen by the sender. A request always expects an answer.

The second shape is a **response**. It has the same id as the request it answers, and a result. No method name. The third shape is an **error response**. It also carries the id of the request it answers, plus an error code and a message, instead of a result. The fourth shape is a **notification**. It has a method name and parameters, but no id at all. Nobody ever replies to a notification.

The key idea of this part: **every MCP message is one of those four shapes, and the only thing that connects a reply to its request is the id.** There's nothing else. No ordering guarantee, no separate channel per call. Just a matching number.

Let's walk through a real tool call, the way it appears in the SDK's own conformance tests. The client sends a request with id 7 and method tools slash call. In the parameters it puts the tool's name, "echo", and the arguments, a message whose value is "Peter". It also puts a small metadata object, written underscore meta, that says which protocol version it's speaking, which client it is, and which client features it supports. We'll come back to that metadata in part four, because it's new. The server answers with a response that also has id 7, and a result containing one piece of text content: "hello Peter".

That design has three consequences, and you'll see all three in the code.

First, messages can be in flight at the same time, in both directions. The client can send request 7, then request 8, and get the answer to 8 first. So each side needs a table of "requests I've sent that are still waiting for an answer", looked up by id. That table is the heart of this SDK, and part eight is about it.

Second, both sides can send requests. It isn't only the client asking and the server answering. A server can send a request to the client, for example to ask the user a question. So the SDK uses one shared engine for both the client and the server.

Third, a notification can't fail visibly, because nobody replies to it. And cancellation, "please stop working on request 7", is sent as a notification. So cancellation in MCP is a polite request, not a guarantee. Hold onto that; it comes back in part eight and part eleven.

Recap of part two. JSON-RPC has four shapes: request, response, error response, notification. Requests and their replies share an id, and that's the only link between them. Replies can arrive out of order, both sides can send requests, and notifications, including cancellation, get no reply.

## Part three: what a server offers, and what a client offers back

An MCP server can offer three kinds of things, which the specification calls primitives. The neat way to tell them apart is by asking who decides to use each one.

**Tools** are chosen by the model. A tool is a function with a name, a description, and a JSON Schema that describes its arguments. The model reads the description and the schema, decides it wants to call the tool, and the host sends tools slash call. Tools can have side effects, like writing a file or placing a block in your voxel world. In C#, a tool is a method marked with the McpServerTool attribute, inside a class marked with the McpServerToolType attribute.

**Resources** are chosen by the application. A resource is readable data, addressed by a URI, like file colon slash slash slash and a path. The host decides which resources to pull into the conversation. In C#, they're methods marked McpServerResource.

**Prompts** are chosen by the user. A prompt is a named template with parameters, a bit like a slash command the user can pick from a menu. In C#, methods marked McpServerPrompt.

Then there are features that go the other way, where the server asks the client for something. The current one is **elicitation**: the server asks the user, through the client, for some input or a confirmation, like "are you sure you want to delete these files?". Two older ones, **sampling**, where the server asks the client's language model to generate text, and **roots**, where the server asks which folders the client considers in scope, were **deprecated** in the July 2026 revision of the protocol. So was the protocol's built-in logging channel. The SDK still supports all three for older connections, but marks them with a deprecation warning.

Around those, there are utilities: progress notifications for long operations, cancellation, pagination for long lists, argument autocompletion, and **capabilities**. Capabilities are each side's declaration of which features it supports, so neither side calls something the other can't handle. And there are two optional extensions, each its own package in this repository: **Tasks**, for long-running tool calls the client can poll, and **Apps**, which links tools to small web pages the host can display. Apps is marked experimental.

Recap of part three. Tools are chosen by the model, resources by the application, prompts by the user. Elicitation lets a server ask the user for input; sampling, roots and logging are deprecated as of July 2026. Capabilities tell each side what the other supports. Tasks and Apps are optional extension packages.

## Part four: how a conversation starts, and how the bytes travel

### Then and now

This is where the big change in version 2.0 lives, so let's go slowly.

Up to the protocol revision dated November 25, 2025, every connection began with a handshake. The client sent a request called initialize, carrying the protocol version it wanted and its capabilities. The server answered with the version it agreed to, its own capabilities and its name. Then the client sent a notification called initialized, and only after that did normal requests begin. From then on, the connection "knew" the version and the capabilities. Over HTTP, the server could also hand out a session id, in a header called M-C-P Session I-D, and the client then had to send that id on every later request.

The revision dated July 28, 2026, removed both. No initialize handshake, and no session id header. Instead, every single request carries its own protocol version, client information and client capabilities inside that underscore meta object you heard about in part two. Over HTTP, the version also goes in a header called M-C-P Protocol Version. If a client wants to know what a server supports before it starts, it can send an optional request called server slash discover, which answers with the versions and capabilities the server supports. That's a question the client can ask, not a ceremony it must complete.

The key idea of this part: **a handshake creates state, and state ties a client to one particular server instance; moving the handshake's information into every request makes each request self-describing, so any instance can serve it.**

Why does that matter? Remember that whoever performs a handshake has to remember the result. If you run five copies of a server behind a load balancer, and the handshake happened on copy number two, then every later request from that client has to reach copy number two, because only copy two remembers. That's called session affinity, or sticky sessions, and it makes scaling and failover harder. Once every request carries its own context, it doesn't matter which copy answers. This is the same move that REST made famous as "stateless".

How does the SDK cope with both worlds at once? The client is polite: it probes first. On a local process connection, it sends server slash discover and waits up to five seconds, a value you can change with a client option called Discover Probe Timeout. Over HTTP, it simply sends its first request with the July 2026 version header. If the server doesn't understand, the client falls back to the old initialize handshake on the same connection. It remembers which era worked, so the probe only costs anything the first time.

A small detail that's worth noticing: protocol versions are dates written as year, month, day, with fixed widths. So "is this version July 2026 or later?" is answered in the SDK with a plain string comparison. That works only because the format is fixed-width. Clever, and a little fragile.

And a detail that connects to your own scouting list. That five-second probe timeout turned out to be too short on slow continuous-integration machines, which made tests fail randomly. The fix, recorded as issue 1701, was for the test helper to raise the timeout to sixty seconds without changing the product default. One of the issues on your shortlist, number 1806, is the same kind of flaky timeout, in the OAuth code.

### The transports

Now, how do the bytes travel? The SDK calls the piece that moves messages a **transport**. And the first thing to know about a transport is what it doesn't do: a transport knows nothing about tools. It only moves JSON-RPC messages.

The first transport is **standard input and output**, usually called stdio. The MCP host launches your server as a child process and talks to it through the process's standard input and standard output. Each message is one line of JSON, ending in a newline character. That's why a stdio server must never write anything else to standard output: not a startup banner, not a console log line. The transport on the other side reads standard output line by line and tries to parse every line as a JSON-RPC message. A stray print statement corrupts the conversation. You already followed this rule in Tool_Box, logging only to standard error. Now you know exactly why: standard output is the wire.

The second transport is **Streamable HTTP**, the recommended one for remote servers. Every message the client sends is an HTTP POST. The server holds that POST's response open and writes the reply back into it, along with any progress notifications along the way, as a stream of server-sent events. That gives you something valuable called **backpressure**. Backpressure means the producer is slowed down when the consumer can't keep up. Because each POST doesn't complete until its handler finishes, a client can't pile up unlimited work on the server.

Streamable HTTP has modes, set by an option called Session Mode. **Stateless**, the default since version 2.0, keeps no session at all: every POST is independent. **Stateful** keeps a session in memory per client, identified by the session id header, which allows the server to push messages whenever it likes, over a long-lived GET request. And there's a hybrid mode, Stateful For Initialize Clients, where old clients that do the handshake get sessions while July 2026 clients are served statelessly on the same endpoint. The old yes-or-no option called Stateless, the one you set in Tool_Box, still exists as a shorthand for the first two.

The third transport is the **legacy SSE transport**, server-sent events. The client opens a long-lived GET to a path ending in slash sse to receive messages, and sends its own messages as POSTs to a separate path ending in slash message. The trouble is that those POSTs return "202 Accepted" immediately, before the work even starts, so there's no backpressure: a client, or an attacker, can flood the server with calls. That's the stated reason legacy SSE is now switched off by default and marked obsolete.

### A form sent back to be filled in

One last piece of part four. In stateless mode, a server can't send a request to the client, like an elicitation question. Why not? Because the client's answer would arrive as a new POST, and that new POST might land on a different server instance, which knows nothing about the question. So the July 2026 revision added **MRTR**, multi round-trip requests. Instead of asking the client a question and waiting, the tool stops and returns an incomplete result that says, in effect, "I need this input before I can finish." In C#, the tool does that by throwing an exception called Input Required Exception. The client collects the input, usually by asking the user, and then calls the same tool again, with the answers attached, plus any opaque state the server handed out the first time. The state travels with the client instead of sitting in server memory. It works like a paper form that comes back to you with "please fill in box three and resubmit".

A common misconception is that stateless mode means a server simply can't ask the user anything. Actually, with clients that speak the July 2026 revision, elicitation works fine statelessly through MRTR. What stateless mode really can't do is push a message the client didn't ask for, like "the list of tools just changed".

Recap of part four. Up to November 2025, connections began with an initialize handshake and, over HTTP, a session id. From July 2026, there's no handshake and no session id; every request carries its own version and capabilities, and server slash discover is an optional probe. The client probes, then falls back. Transports only move messages. Stdio is one JSON line per message, so standard output must stay clean. Streamable HTTP has natural backpressure and is stateless by default. Legacy SSE has no backpressure and is off by default. MRTR lets a stateless tool ask for input by returning an incomplete result and being called again.

## Part five: where the SDK's code comes from

The SDK ships as five NuGet packages, stacked so that you only take on the dependencies you actually need.

At the bottom is **ModelContextProtocol dot Core**. It holds the protocol's data types, about a hundred and fifty small classes, roughly one per kind of message. It holds the transports, the client, the low-level server, and the engine that moves and matches messages. Its dependencies are deliberately few: the JSON library, the channels library for queues, the logging abstractions, and a package called Microsoft dot Extensions dot A-I dot Abstractions, which we'll come back to.

On top of Core sits the main package, simply called **ModelContextProtocol**. It adds the dependency injection and hosting glue: the Add M-C-P Server method, all the With methods like With Tools and With Stdio Server Transport, and a small background service that runs the server. This is the package most projects start with.

On top of that sits **ModelContextProtocol dot AspNetCore**, for HTTP servers. It adds Map M-C-P, With Http Transport, and the code that turns an HTTP POST into an MCP message and back. And beside those are the two extension packages, Tasks and Apps.

The repository has two more folders in its source directory that aren't packages of their own. One is the **Analyzers** project, which holds a source generator, a piece of code that runs inside the compiler. It reads the XML documentation comments you write above a tool method and generates the Description attributes for you. It's shipped inside the Core package. The other is a folder called **Common**, with files compiled into several projects at once: the protocol version constants, the HTTP header names, and some compatibility shims for older .NET.

### Package, assembly, namespace

Now the first of the .NET ideas, and this repository is a perfect specimen for it. You've noticed before that the line between a package, an assembly and a namespace is fuzzy for you. Let's make it sharp, using three facts.

A **package** is a zip file used to deliver code. NuGet downloads it and unpacks it at restore time, and after that, the package itself is out of the picture.

An **assembly** is a compiled file, a D-L-L, containing intermediate language. Referencing an assembly, directly or through a package, is what makes its public types available to the compiler.

A **namespace** is only a name prefix. Any assembly can declare types in any namespace it likes. And the using directive at the top of a file is purely a typing shortcut. **A using directive grants no access and loads nothing.** If the assembly isn't referenced, no using line can make its types appear.

Here's the specimen. When you write builder dot Services dot Add M-C-P Server, that method lives in the assembly called ModelContextProtocol. But it's declared in the namespace Microsoft dot Extensions dot Dependency Injection, a namespace that belongs, by convention, to Microsoft's own DI library. Why would an MCP library put its method in somebody else's namespace? So that it shows up automatically wherever you already have that namespace imported, which in an ASP.NET Core app is everywhere, through implicit usings. No extra using line needed. The same trick puts Map M-C-P in the namespace Microsoft dot AspNetCore dot Builder, right next to Map Get and Map Post. So if you search the repository for where Add M-C-P Server comes from, don't go looking by namespace; go by assembly.

A common misconception is that adding using ModelContextProtocol dot Server "adds the SDK" to your project. Actually, the package reference added the SDK. The using line only saves you from typing the namespace in front of every type name.

And some of the most important types you'll touch don't live in this repository at all. The type called A-I Function, which represents "a function a language model can call", and the type called I Chat Client, which represents "a chat model you can send messages to", both come from Microsoft dot Extensions dot A-I, which is built in a different repository, dotnet slash extensions. Dependency injection, options, logging and the Generic Host come from dotnet slash runtime. So if you ever find a bug in how a JSON Schema gets generated from a C# parameter, it might really live in the A-I Function factory in dotnet slash extensions, not here.

Recap of part five. Five packages: Core at the bottom, the main package with DI and hosting on top, the ASP.NET Core package on top of that, plus Tasks and Apps. A package delivers, an assembly contains compiled code, a namespace is just a name. The SDK puts its setup methods in Microsoft's namespaces on purpose. A using directive grants nothing. A-I Function and I Chat Client come from dotnet slash extensions.

## Part six: the cast of characters

Here are the characters who run this machine. Each gets a name and a job, and the real class name, so you can find them in the source. These names are used for the rest of the episode.

**The Requester** is the client, the class M-C-P Client. It connects, probes the protocol version, sends requests, and turns the server's tools into what we'll call **Tool Cards**: each one is an M-C-P Client Tool object, which is a kind of A-I Function, so it can be handed straight to any chat model.

**The Stage Manager** is the .NET Generic Host. It starts and stops background services, and owns configuration, logging and the container. **The Supply Room** is the dependency injection container. It builds objects from registrations and decides how long each one lives.

**The Starter** is a small background service with a long name: Single Session M-C-P Server Hosted Service. Its whole job is to call Run Async on the server and wait. When that ends, it tells the Stage Manager to stop the application.

**The Messenger** is the transport. Its contract is an interface called I Transport. On the server side, the stdio Messenger is a class called Stdio Server Transport, built on a more general Stream Server Transport. The HTTP Messenger is the Streamable Http Server Transport. The Messenger turns bytes into message objects, and message objects back into bytes. It knows nothing about tools.

**The Inbox** is a queue between the Messenger and the next character. Technically it's a channel of JSON-RPC messages, exposed on the transport as a property called Message Reader. The Messenger writes into it; one reader takes messages out.

**The Postmaster** is the engine at the center: an internal class called M-C-P Session Handler. The Postmaster reads the Inbox, starts a separate handling job for every message, matches every reply to the request waiting for it, and handles cancellation. Here's the key fact about the Postmaster: **the client and the server each have one, and it's the same class.** The protocol is symmetric, so the engine is too.

The Postmaster keeps two boards on the wall. **The Claim-Ticket Board** holds one ticket for every request this side has sent that's still waiting for its reply. Each ticket is filed under the request's id. **The Stop Cords** are the opposite: one cord for every request this side is currently handling, also filed by id. When a cancellation notification arrives, the Postmaster finds the cord with that id and pulls it.

**The Directory** maps a method name, like tools slash call, to the function that handles it. **The Concierge** is the server itself: the public abstract class M-C-P Server, whose real implementation is a large internal class called M-C-P Server Impl, about two thousand five hundred lines long. The Concierge is built from **the Blueprint**, the options object called M-C-P Server Options, and when it's built, it fills the Directory, answers server slash discover and initialize, and advertises capabilities.

**The Catalog Builder**, a class called M-C-P Server Options Setup, gathers every registered tool, prompt and resource into the Blueprint's catalogs. **The Catalog** is the tool collection: look up a tool by name.

**The Interpreter** wraps one of your C# methods and makes it a tool. Its class is A-I Function M-C-P Server Tool. It translates JSON arguments into C# parameters, fills in special parameters from the Supply Room, calls your method, and translates the return value into a tool result.

**The Work Order** is everything about one request, handed to every handler and filter: the class Request Context. **The Return Address** is a per-request view of the server, a class called Destination Bound M-C-P Server, that makes sure anything a tool sends, like a progress update, goes back down the right connection. **The Checkpoints** are filters that wrap handlers, for logging, authorization and validation.

And for HTTP, two more. **The Intake Desk** is a class called Streamable Http Handler: the ASP.NET Core endpoint that receives each POST. **The Session Ledger**, the Stateful Session Manager, keeps track of sessions in stateful mode.

That's a big cast, so here's the one-sentence version: the Messenger fills the Inbox, the Postmaster empties it and looks things up in the Directory that the Concierge filled, the Interpreter calls your method, and the Postmaster's Claim-Ticket Board is how the other side knows its answer arrived.

## Part seven: startup, and what the famous lines actually do

A minimal stdio server in this SDK is five lines. You create a builder with Host dot Create Application Builder. You tell the console logger to send everything to standard error. You call Add M-C-P Server, then With Stdio Server Transport, then With Tools From Assembly. And you call Build, then Run Async.

The key idea of this part: **the With calls create nothing. They only write recipes into the Supply Room's list. Nothing is built until the Stage Manager starts the Starter, and then a single request for one object pulls the entire graph into existence.** If you heard the ASP.NET Core episode, this is the same two-phase pattern: register, then resolve.

### Phase one: registration

Add M-C-P Server does three small things. It turns on the options system. It registers the Catalog Builder as something that configures the server options. And it returns a builder object, which is nothing more than a wrapper around the list of service registrations, so that the next method calls can chain off it.

With Stdio Server Transport adds three recipes. It registers the Starter as a hosted service. It registers a recipe for the server: when someone asks for an M-C-P Server, call M-C-P Server dot Create with the transport, the options and the logger. And it registers a recipe for the transport: when someone asks for an I Transport, build a Stdio Server Transport.

With Tools From Assembly uses reflection. It looks through every type in your assembly, keeps the ones labeled with the McpServerToolType attribute, and for every method in them labeled McpServerTool, it adds one more recipe: when someone asks for the list of tools, include one built by calling M-C-P Server Tool dot Create on this method. The tool objects are registered as singletons.

### Phase two: Build

Build turns the list into a container. Almost nothing is constructed yet.

### Phase three: Run, and the chain reaction

Now Run Async starts the Stage Manager, and the Stage Manager starts its hosted services. Here's the chain, link by link.

The Stage Manager needs the Starter. The Starter's constructor needs an M-C-P Server. The server's recipe needs a transport, so the Supply Room builds the Stdio Server Transport. And notice what that transport's constructor does: it marks itself connected and immediately starts a background read loop on the thread pool. From this moment the Messenger is reading standard input and dropping messages into the Inbox, even before anything is reading the Inbox.

Next, the server's recipe needs the options. Asking for the options makes the options system run the Catalog Builder. The Catalog Builder needs the list of all registered tools, so the Supply Room runs every tool recipe. Each one calls M-C-P Server Tool dot Create on a method, which hands the method to the A-I Function factory from Microsoft's A-I library. The factory inspects the method's parameters and their Description attributes and builds the JSON Schema the language model will read. That's the Interpreter being born, once per tool. The Catalog Builder puts them all into the Catalog.

Finally, M-C-P Server dot Create builds the Concierge. The Concierge's constructor is a list of configure steps, run in order: configure initialize, configure discover, configure tools, prompts, resources, logging, completion, subscriptions, MRTR, and any custom handlers. Each step adds entries to the Directory and builds that operation's chain of Checkpoints. The last thing the constructor does is create the Postmaster, handing it the transport and the Directory.

Now the Starter runs. It calls Run Async on the server, which starts the Postmaster's loop, and the server is live.

### Shutdown, the half people skip

When the MCP host closes the server's standard input, the Messenger's read-line call returns "end of stream". The read loop ends and marks the transport disconnected, which completes the Inbox: no more messages will ever arrive. The Postmaster's loop finishes. The Postmaster then waits for any handlers still running, and fails every ticket still on its Claim-Ticket Board with an error that says the server shut down unexpectedly. Run Async disposes the server. And the Starter, in its finally block, tells the Stage Manager to stop the application. The process exits.

That's why a stdio server dies when its client goes away. The end of standard input is the shutdown signal, and the Starter is the piece that turns "the conversation ended" into "the program ends".

Recap of part seven. Registration writes recipes; Build makes the container; Run starts the Stage Manager, which needs the Starter, which needs the server, which needs the transport, whose constructor starts reading standard input, and the options, whose Catalog Builder runs every tool recipe and builds every Interpreter. The Concierge's constructor fills the Directory and creates the Postmaster. End of standard input shuts the whole chain down in reverse.

## Part eight: one tool call, step by step, and the Postmaster's engine

This is the main event. The MCP host's client calls Call Tool Async with the tool name "echo" and one argument, message equals "Peter". On the server, echo is a static method that returns the word "hello" followed by the message. It's the real echo tool from the SDK's own test suite. Its method name is Echo with a capital E; the tool name is echo in lowercase because the SDK derives tool names by dropping any "Async" suffix and converting to snake case.

The key idea of this part: **a tool call is two one-way trips; on the way in, the server's Postmaster starts a handling job and looks the method up in the Directory; on the way out, the client's Postmaster finds the Claim Ticket with the same id and completes it.**

### The way in

Step one. The Requester builds a request: method tools slash call, with the name, the arguments and the metadata. It hands it to the client's Postmaster.

Step two. The client's Postmaster gives the request a fresh id, say 7, by atomically adding one to a counter. Then it creates a Claim Ticket and pins it on the board under number 7. The Claim Ticket is a real .NET type called Task Completion Source, and it deserves a careful explanation. A normal task represents some work that's running and will finish. A task completion source is a task that nobody runs. It just sits there, unfinished, until some other piece of code calls Set Result on it. That's exactly what "wait for a reply that will arrive later, by a different path" needs. The Postmaster also starts a tracing span, for OpenTelemetry, and tucks the trace context into the request so the server can continue the same trace.

Step three. The client's Messenger turns the request into one line of JSON and writes it to the child process's standard input.

Step four. The client's Postmaster awaits the Claim Ticket's task. And here's a fact to say out loud: **awaiting does not block a thread.** The method is parked, and the thread goes back to the pool to do other work. When the ticket is completed later, the method resumes, possibly on a different thread.

Step five. On the server, the Messenger's read loop gets the line, deserializes it into a JSON-RPC request object, and drops it into the Inbox.

Step six. The server's Postmaster takes it out of the Inbox. It adds one to its count of messages in flight. Then it starts a handling job for this message, and here's the important part: **it does not wait for that job to finish.** It goes straight back to the Inbox for the next message. This is called fire and forget. It's what makes the server concurrent: if request 8 arrives while request 7 is still working, request 8 starts too.

Step seven. The handling job creates a cancellation token source for this request, linked to the server's overall shutdown token, and hangs it on the Stop Cords under id 7. Then it does something that looks odd: it deliberately yields, giving up the thread right away. We'll come back to why in a moment.

Step eight. The handling job starts the server's side of the trace, using the context the client tucked in. It runs the message filters. The first one is built into the SDK: it reads the underscore meta object, the protocol version and the client's capabilities, and checks that they're valid.

Step nine. The job looks up tools slash call in the Directory. If the method weren't there, it would throw a protocol exception with the code "method not found".

Step ten. The Directory's entry deserializes the parameters into a typed object, the Call Tool Request Params. Because a server option called Scope Requests is true by default, it asks the Supply Room for a fresh scope for this request. Then it builds the Work Order, with the Return Address, the parameters, and the scoped services.

Step eleven. The tool pipeline runs. First come some special outer filters, used by the Tasks extension and by authorization. Then a step called Match Tool looks up "echo" in the Catalog and notes which tool matched. Then your own Checkpoints, if you registered any. Then the base handler, which calls the matched tool's Invoke Async.

Step twelve. The Interpreter takes over. It wraps the scoped services in a special provider that can also hand out four request-specific things: the server, the Work Order, a progress reporter, and the user's identity. It copies the JSON arguments into an arguments object, and calls the A-I Function, which binds the argument called message to the parameter called message and calls your method.

Step thirteen. Your method returns "hello Peter".

Step fourteen. The Interpreter looks at the return value's type. It's a string, so it wraps it in a tool result with one piece of text content.

### The way out

Step fifteen. The result travels back up through your Checkpoints, which get to run their "after" code. The SDK stamps a field called result type with the value "complete", but only for July 2026 clients, because older clients would reject the unknown field. The request's scope is disposed.

Step sixteen. The handling job sends a response with id 7. It passes through the outgoing filters, which stamp the server's name, and reaches the Messenger, which writes one line to standard output. It does that while holding a lock, a semaphore that lets only one writer through at a time, because many handling jobs might be finishing at once, and two lines written at the same moment could interleave halfway through the JSON.

Step seventeen. The handling job's finally block takes down Stop Cord 7, disposes the token source, and subtracts one from the in-flight count.

Step eighteen. On the client, the Messenger reads the line and drops it in the client's Inbox. The client's Postmaster sees that it's a response, not a request, so it doesn't look in the Directory. Instead it removes ticket 7 from the Claim-Ticket Board and calls Set Result on it.

Step nineteen. That completes the task that step four was awaiting. The Requester's method resumes, reads the tool result, and returns it to the MCP host, which shows it to the language model.

### Two calls at once, with the state after every step

Now let's run two calls at the same time and track exactly what's on each board after every event. Request 7 is a slow tool that takes ten seconds. Request 8 is echo.

The client sends 7. The client's board holds ticket 7; the server's cords are empty, because it hasn't read anything yet. The client sends 8. The board now holds tickets 7 and 8. The server reads 7 and hangs cord 7. The server reads 8 and hangs cord 8, without waiting for 7 to finish. Now both handlers are running at the same time, and the server holds cords 7 and 8. Echo finishes first: the server sends reply 8 and takes down cord 8. Cords: just 7. The client reads reply 8 and completes ticket 8. Board: just 7. Notice what just happened: **request 8 was answered before request 7.** Order is by completion, not by sending. That's the whole reason the board exists.

Now the user cancels request 7. The client's cancellation token fires. The client's Postmaster had registered a small callback for exactly this: it sends a notification called notifications slash cancelled, carrying request id 7. At the same moment, the client's await of ticket 7 throws an "operation cancelled" exception, and its finally block unpins ticket 7. The client's board is now empty: it has stopped waiting. But the server's cord 7 is still up, because the server hasn't read the notification yet.

The server reads the cancellation notification, finds cord 7, and pulls it, which signals the slow tool's cancellation token. And now the result depends entirely on the tool. If the tool checks its token, or is awaiting something that honors the token, it throws "operation cancelled", the handling job recognizes it as a user cancellation, and sends no reply at all, because nobody's listening for one. If the tool ignores its token, it keeps running to the end, sends reply 7 at the ten-second mark, and the client's Postmaster finds no ticket for 7, logs "no request found", and drops it.

The key idea of that walk: **cancellation is cooperative. Pulling the cord sets a flag; it doesn't stop anything by itself.**

Two details in the code are worth admiring. First, the client registers its cancellation callback only after the request has been sent. If it registered before, a cancellation could be sent before the request itself, and the server would ignore a cancel for an id it had never heard of. Second, the server hangs the Stop Cord before its handling job yields, so a cancellation arriving right behind the request always finds the cord. And one exception: the old initialize request never gets a cord, because the specification says it must not be cancelled.

### Why the forced yield

Back to that odd moment in step seven, where the handling job deliberately gives up its thread. Here's the problem it prevents.

An async method runs synchronously, on whatever thread called it, until it reaches its first await that actually has to wait. The handling job is started from inside the Postmaster's loop. So without a yield, the handler's first stretch of code runs on the loop's own thread, and while it runs, the loop can't read the next message.

Now suppose the handler, still on that stretch, sends a request to the other side, say an elicitation question, and waits for the answer. The answer arrives in the Inbox. But the only code that reads the Inbox is the loop, and the loop is stuck inside the handler, waiting for the answer. Each is waiting for the other. That's a deadlock. The comment in the source says it directly: if we await the handler without yielding first, the transport may not be able to read more messages, which could lead to a deadlock if the handler sends a message back. The forced yield makes the handler hand the thread back immediately and continue on the thread pool, so the loop is always free to keep reading.

There's a cousin of this protection on the Claim Tickets. They're created with an option called Run Continuations Asynchronously. Without it, completing a ticket would run the waiting method's continuation right there, on the Postmaster's thread, and a slow continuation would stall the reading of every other message. With it, the continuation is queued to the thread pool instead.

Recap of part eight. The client assigns an id and pins a Claim Ticket, a task completion source, then awaits it without blocking a thread. The server's Messenger fills the Inbox; the Postmaster starts a fire-and-forget handling job per message and hangs a Stop Cord; the job looks up the Directory, creates a scope and a Work Order, runs the Checkpoints, and the Interpreter calls your method and wraps the result. The reply is written under a lock, and the client's Postmaster completes the ticket with the matching id. Replies can arrive out of order. Cancellation is a notification that pulls a cord, and the tool must cooperate. The forced yield and asynchronous continuations keep the Postmaster's loop free, which prevents deadlocks.

## Part nine: the HTTP path, and a server for every request

For an HTTP server, the setup changes in two places. Instead of With Stdio Server Transport, you call With Http Transport, optionally setting the Session Mode. And after building the app, you call app dot Map M-C-P with a route, like slash mcp.

The key idea of this part: **in stateless mode, every HTTP POST gets its own brand-new Concierge, Messenger and Postmaster, and they live exactly as long as that one POST.** Once you hold onto that, most HTTP behavior explains itself.

Map M-C-P maps a POST endpoint at your route in every mode. In stateful mode it also maps a GET endpoint, for the long-lived stream the server uses to push messages, and a DELETE endpoint, for ending a session. In stateless mode, GET and DELETE aren't mapped at all, because there's no session to stream on or to delete. And if you try to enable legacy SSE in stateless mode, Map M-C-P throws an error at startup, because SSE needs a session shared between its GET and its POSTs.

Here's what the Intake Desk does with each POST, in order. First, it checks the Accept header: the client must accept both plain JSON and an event stream, otherwise the answer is 406, "not acceptable". Second, it reads the body and parses it as one JSON-RPC message; garbage gets a 400, "bad request". Third, it validates the protocol version header, the metadata in the message, and the two new headers the July 2026 revision requires, M-C-P Method and M-C-P Name, which repeat the method and the tool name from the body. Any mismatch is another 400, and every error carries the request's id so the client can match it. Fourth, if the client speaks the July 2026 revision and asks for a method that revision removed, like initialize or ping, the answer is 404, "method not found".

Why repeat the method and tool name in headers when they're already in the body? So that load balancers, proxies and gateways can route a request without parsing its JSON body. The SDK's tools documentation says this directly. A gateway like YARP could route on the M-C-P Name header the same way it routes on a path.

Fifth, the Intake Desk gets a session for this request. In stateless mode, that means making a new one: a new Streamable Http Server Transport marked stateless, then M-C-P Server dot Create with that transport. And here's a detail that ties back to dependency injection: the new server is given the HTTP request's own service scope, and its Scope Requests option is switched off, because the HTTP request already has a scope and there's no point making another. Then the server's Run Async starts, tied to the HTTP request's abort token. Sixth and last, the Intake Desk hands the message to the new transport, and the reply, along with any progress notifications, is written into the POST's response body as a stream. If nothing at all gets written, for example because the message was a notification, the answer is 202, "accepted".

### What this means for your tool code

Two POSTs arrive at the same moment. Each gets its own Concierge. Each Concierge resolves its tools from the same Supply Room. Anything stored on a Concierge is thrown away after its POST. But anything stored in a singleton service, or in a static field, is shared by every request, and ASP.NET Core's web server handles POSTs in parallel. So two tool calls can touch that shared state at exactly the same instant.

You've met this before. In your capstone review of Tool_Box, the sharpest finding was that the voxel world's singleton dictionary was a genuine data race, because ASP.NET Core serves requests concurrently and stateless mode advertises horizontal scaling that the state layer couldn't honor. Now you can see the mechanism inside the SDK itself: a new server per POST, all sharing the same singletons.

Stateful mode is the other trade. One Concierge lives for the whole session, and the Session Ledger maps each session id to its live session, so per-session state survives between POSTs. The price is that every request in a session must reach the same server instance, which means sticky sessions behind a load balancer, and that it's now the legacy path for July 2026 clients.

A common misconception is that "stateless" makes a server safe for concurrent use. Actually, stateless only means the SDK keeps no session between requests. It says nothing about your own singletons and statics. Those are shared and concurrent, and their safety is your job.

Recap of part nine. Map M-C-P maps POST always, and GET and DELETE only in stateful mode. The Intake Desk checks Accept, parses, validates versions and headers, rejects removed methods, then in stateless mode creates a new server per POST, using the request's scope. Singletons and statics are shared across concurrent requests. Stateful mode keeps a server per session but needs sticky sessions.

## Part ten: the client side, errors, and filters

### The client side

On the client, you create a transport, for example a Stdio Client Transport with a command to run, and call M-C-P Client dot Create Async. The client transport is really a factory: its Connect Async method is what launches the child process and returns the Messenger. Then the Requester runs the version probe you heard about in part four, falls back if it needs to, and starts its own Postmaster loop.

One safety detail on the client's stdio transport: by default, the child process inherits every environment variable of the parent. That includes any secrets sitting in your environment, like API keys and tokens, handed to a third-party server you may not trust. The transport options have a setting to turn inheritance off, and a helper that returns a curated, safe set of variables, such as the path and the home directory.

And here's the bridge to language models. When you call List Tools Async, you get Tool Cards, M-C-P Client Tool objects. Each one is an A-I Function, the same abstraction from Microsoft's A-I library. So you can hand the list straight to any I Chat Client, whether it talks to OpenAI, Anthropic or a local Ollama model. The model sees a function with a name, a description and a schema. When it asks to call one, the Tool Card sends tools slash call. Notice the symmetry: on the server, the Interpreter is also built on an A-I Function. MCP is the wire between two A-I Functions in two different processes.

### Two kinds of errors

The key idea here: **a tool that fails returns a normal result marked as an error, so the language model can read it and try something else; a protocol failure is a JSON-RPC error, which the client program sees as an exception.**

If your tool throws an ordinary exception, say an invalid operation exception with the message "Test error", the SDK catches it and sends back a normal tool result with a flag called is error set to true, and a text that says only: an error occurred invoking, and the tool's name. Your message is hidden. The full exception goes to the server's log instead. The SDK's own test for this checks exactly that: the client sees "an error occurred", and the log holds the real exception.

Why hide it? Because exception messages can leak internals, like file paths or connection strings, across a trust boundary to a remote client and into a language model's context. If you want the caller to see your message, you throw an M-C-P Exception instead. That's the explicit opt-in that says "I wrote this message for the caller." It still arrives as a result with is error set, but now it includes your text.

Protocol failures are different. If a tool throws an M-C-P Protocol Exception with the code "invalid params", or if a client asks for a method that doesn't exist, the SDK sends a real JSON-RPC error response, with a numeric code. The client's Postmaster finds the ticket, sees an error instead of a result, and throws an exception in the caller's code.

### Filters

The Checkpoints are filters, and the rule for them is this: **a filter is a function that takes the next handler and returns a new handler that wraps it.** The SDK puts them together once, when the Concierge is built, starting from the last one and working toward the first, so that the first filter you registered ends up outermost. If you register three filters, a call passes through filter one, then two, then three, then the handler, and then back out through three, two and one. That's the same shape as ASP.NET Core's middleware pipeline.

There are two levels. Message filters see every raw JSON-RPC message before it's routed. Request filters are per operation, like "every call-tool" or "every list-tools", with typed parameters and results.

A small lesson hides here. The repository's instructions file for AI assistants shows a request filter as a class with an Invoke Async method. In the actual code, it's a delegate: a function type that takes the next handler and returns a handler. Instruction files drift away from the code. When the docs and the code disagree, the code wins, and that kind of drift can itself be a tiny contribution, after you've checked it's still wrong.

Recap of part ten. The client transport is a factory that launches the process; the client probes the version; environment inheritance is on by default and leaks secrets. Tool Cards are A-I Functions, so any chat client can use them. Tool failures become results with is error set, with messages hidden unless you throw an M-C-P Exception. Protocol failures are JSON-RPC errors. Filters are functions that wrap handlers, assembled backwards so the first registered runs outermost.

## Part eleven: the .NET ideas underneath

These are the ideas that, in your teach-backs, have been the most fuzzy, plus the async topics you've said you want to own. Each one starts with its rule.

### Library or framework

The rule: **with a library, your code calls it and it returns; with a framework, the framework owns the main loop and calls your code.** The MCP SDK is a library. If you create a client in your own Main method and call Call Tool Async, you're in charge: you call, it returns. But once you register the server with the Generic Host and call Run, the Stage Manager owns the loop, the Starter runs the Postmaster, and your tool methods get called whenever messages arrive. That's inversion of control.

Let's bound that. The inversion comes from the Generic Host and from ASP.NET Core, not from anything special about MCP. "SDK" isn't a technical category at all. It's a product word for "the official library for using a protocol from a language". And you can run an MCP server without any host: the SDK's in-memory transport sample creates a server and calls Run Async itself.

### Attributes do nothing on their own

The rule: **an attribute is inert metadata compiled into the assembly; some other code must look for it and act on it.** The McpServerTool attribute doesn't register a tool. With Tools From Assembly registers the tool, by using reflection to look for the attribute at startup. Delete that one call and the attribute is still there, and no tool exists. The Description attribute doesn't do anything either, until the A-I Function factory reads it and copies it into the JSON Schema. And that's worth saying as its own idea: **descriptions are prompts.** The language model reads them to decide what to call. That's why Tool_Box had a test that refused any tool without a description.

There's a second reader of attributes in this repository: the source generator in the Analyzers project. It runs at compile time, not at run time. When you mark a partial method as a tool and write XML documentation above it, the generator writes the Description attributes for you.

And one consequence for performance. Reflection that scans a whole assembly can't see types that the trimmer removed when compiling ahead of time, which is called Native AOT. That's why the non-generic With Tools From Assembly is marked as possibly not working with Native AOT, and the generic form, With Tools with the tool class in angle brackets, is the safe one. The angle brackets tell the compiler exactly which class to keep.

### Dependency injection: lifetimes and the instance surprise

The rule: **registration writes recipes, resolution builds objects, the lifetime decides how long an object is shared, and the container disposes exactly the objects it created.**

A singleton is one object for the whole application. Every tool wrapper, every Interpreter, is a singleton. On stdio, the transport and the server are singletons too. A scoped object is one per scope, and the SDK makes one scope per request by default. A transient object is new every time it's asked for.

Now the surprise, which almost everyone gets wrong the first time. Suppose your tool is an instance method, not a static one, on a class whose constructor takes some services. The Interpreter wrapping that method is a singleton. But a brand-new instance of your tool class is created for every single call, using the request's scope to fill the constructor. So **fields on a tool class are not state.** If you store a counter in a field and increment it in the tool, every caller sees one. If you want state that survives between calls, put it in a singleton service, and then its thread safety is your job, for the reasons in part nine.

Then there are special parameters. When the Interpreter is built, it sorts your method's parameters into two groups. A parameter whose type is a registered service, or one of four special types, is hidden from the schema and filled in at call time from the Supply Room. The four special types are the server itself, the Work Order, a progress reporter, and the user's claims principal, their identity. A cancellation token is also filled in automatically and never appears in the schema. Every other parameter appears in the schema the model sees and is filled from the JSON arguments. So a tool method with a URL, an HTTP client, a progress reporter and a cancellation token shows the model exactly one parameter: the URL.

And ownership. "Owns" means "is responsible for releasing". A leak is something acquired and never released, the same idea as allocating memory in C on a path that never reaches the free call. In .NET, Dispose and Dispose Async are how you release things the garbage collector can't release by itself: threads, sockets, file handles, child processes. The server implementation in this SDK can only be disposed asynchronously, which is why the contributing guide insists on await using for any service provider that holds a server. A plain using statement would throw. And it's why tests must never use the stdio server transport: the test process's standard input can never be closed, so the transport's read loop never ends, and a thread pool thread is leaked for every test.

### Delegates and functions that return functions

The rule: **a delegate is a typed reference to a method, plus, for a lambda, the variables it captured.** The comparison to C helps here, and here's where it stops. A delegate is like a C function pointer with a hidden context pointer attached, carrying either the target object or the captured variables. Unlike a C function pointer, it's type-checked, and a lambda's captured variables are shared, not copied.

A request filter is written as next, arrow, async, open parenthesis, context and cancellation token, close parenthesis, arrow, and then a body. Read slowly, that's a function with one parameter, next, which returns another function, the new handler. Inside the new handler, next is a captured variable, so each wrapped handler remembers its own next. Written out as ordinary methods, it's a method called My Filter that takes next and returns a local function called Handler, and Handler does its "before" work, awaits next, does its "after" work, and returns the result.

### Async: what's real and what isn't

The rule: **await pauses a method without holding a thread; nothing in this SDK creates a thread per request.**

A task is a promise of a result. It doesn't mean a thread. Most tasks in this SDK are waits on input and output, with no thread involved at all while waiting. Await doesn't create threads, and it doesn't make code thread-safe. A task completion source is a task you complete by hand, and it's the Claim Ticket. A channel is an asynchronous producer-consumer queue, and it's the Inbox. Configure Await false, which appears on almost every await in this repository, means "don't try to resume on the captured context"; it matters for libraries used inside desktop UI apps, and makes no difference in a console app or ASP.NET Core. A cancellation token is a read-only "please stop" flag; it stops nothing unless code checks it or passes it to something that does.

There's one genuine firmware look-alike here, so let's use it and bound it. The Messenger and the Inbox form the classic shape of an interrupt routine filling a ring buffer while a main loop drains it. The Messenger's read loop is the producer, the Postmaster's loop is the consumer, and the queue lets them run at different speeds. Where it stops: this queue is unbounded, so it grows on the heap instead of dropping or overwriting data; several code paths may write to it; and the consumer doesn't poll, it's woken up by a continuation.

And the conclusion that matters most for your own code: because of fire-and-forget dispatch and parallel HTTP handling, **two calls to the same tool can be running at the same instant. The SDK guarantees concurrency; it does not guarantee your state's safety.**

### Reading the compact syntax

This repository uses preview C#, so a few compact forms appear constantly. Here they are, expanded into if-and-else form.

Question-mark question-mark equals, as in "tool assembly question-mark question-mark equals get calling assembly", means: if tool assembly is null, set it to the calling assembly. Question-mark dot followed by question-mark question-mark, as in "options question-mark dot name, question-mark question-mark, derive name", means: if options isn't null, take its name; and if that result is null, use derive name instead. "Is, open brace, close brace, y" means: if this value is not null, call it y. A switch expression, the word switch followed by cases separated by arrows, is an if, else-if, else chain that produces a value; the Interpreter uses one to decide how to wrap your return value. Square brackets around a list of items is a collection expression, a short way to create a list or an array. Two dots before a name inside those brackets spreads another collection's items in. A class name followed directly by parentheses with parameters is a primary constructor: those parameters are available throughout the class, and dependency injection fills them. And the word field, inside a property's setter, is a brand-new C# feature: it refers to the hidden storage behind the property, so a property can validate its value without declaring a separate private variable. You'll see it in the client option for the discover probe timeout.

Two last build facts. The serializer uses source generation: instead of discovering properties by reflection at run time, generated code does it at compile time, which is faster and works with Native AOT. So any new protocol type needs to be registered with the generator, or the AOT build breaks. And most packages are compiled four times, for .NET 10, 9 and 8, and for the much older .NET Standard 2.0. A change that compiles for .NET 10 can still fail for .NET Standard, which is why you'll see compatibility shims and "if NET" blocks around the code.

Recap of part eleven. The SDK is a library that acts framework-like once the Generic Host runs it. Attributes are inert until reflection or a source generator reads them, and descriptions are prompts. Tool wrappers are singletons, but tool classes are created fresh for every call, so fields aren't state; special parameters come from the Supply Room; the container disposes what it created, and the server only disposes asynchronously. Filters are functions returning functions. Await holds no thread, a cancellation token stops nothing by itself, and your tools run concurrently with themselves.

## Part twelve: the repository, its tests, contributing, misconceptions, and the recap

### The repository and its build

The repository has five top-level folders that matter. The source folder holds the five packages, the Analyzers project and the Common folder. The tests folder holds the main test project, which runs clients and servers inside one process; the ASP.NET Core tests, which run a real HTTP stack in memory; conformance test client and server, which check this SDK against the official MCP conformance suite; and several small helper servers. The samples folder holds thirteen runnable apps, including a quickstart weather server, a quickstart client, an ASP.NET Core server, an in-memory transport example, and a pair showing OAuth protection. The docs folder holds the conceptual documentation, built into a website with a tool called DocFX. And the dot github folder holds the continuous-integration workflows, the instructions file for AI assistants, and a set of AI agent skills that the maintainers use to triage issues and prepare releases.

To build, you need the .NET 10 SDK. Then the commands are simply dotnet build and dotnet test, run from the root. Node.js is needed for the conformance tests, and Docker is optional, because the few tests that need it skip themselves without it. Warnings are treated as errors, so an unused variable fails the build. Continuous integration runs on GitHub Actions, on Ubuntu, Windows and macOS, each in both Debug and Release, plus an ahead-of-time compilation test. Again: none of this has been run on your Mac yet.

### How one test is built

Before reading any test, ask three questions: what's real, what's been replaced, and which of the three moves, arrange, act or assert, is this line doing.

Take the test called Can Call Registered Tool. Everything happens inside the test process, with no network and no child process. The client is real. The server is real. The Postmasters, the Directory, the Catalog, the Interpreter, the echo method and the JSON serialization are all real. The only thing replaced is the operating system's pipes: instead of standard input and output, the client and server are connected by two in-memory pipes, one in each direction, from a library called System dot I-O dot Pipelines. That replacement is a fake, not a mock. A fake is a working substitute. A mock is something you script, "when called with this, return that", and then check, "were you called?". The repository uses a mocking library called Moq in a few places, but not here.

Arrange happens in two places. A shared base class called Client Server Test Base creates the two pipes, registers the server with a stream transport over them, builds the container with scope validation turned on, gets the server and starts it running. The test class adds its tools by overriding a method called Configure Services. Then the test body creates a client connected to the other ends of the pipes, through a helper that, as you heard in part four, raises the discover probe timeout to sixty seconds. Act is one line: call the echo tool with message "Peter". Assert checks that the result has content, that the first piece is text, and that it says "hello Peter".

The house rules for tests, which reviewers will check: use the shared sixty-second default timeout, never a short hard-coded one; never use a fixed delay to wait for something to happen, use a signal like a task completion source instead; pass the test framework's cancellation token to every async call; and dispose clients and servers with await using.

### Contributing

From the contributing guide: first-timer issues are labeled good first issue, and maintainers also use help wanted and ready for work. If no issue exists for your change, open one first. For anything but the smallest change, post your approach on the issue before writing code, and assign yourself so others know. A pull request needs tests for every fix or feature, all tests passing, error handling, and updated documentation. The repository's own AI instructions say that anything posted to GitHub with AI help should carry a visible note saying so, which matches your own rule. Contributions are under the Apache 2.0 license.

Two issues from your September shortlist live here. Issue 1806 is a flaky OAuth timeout on Windows, the same family as the sixty-second probe fix. Issue 1781 asks for a guide to integration-testing a Streamable HTTP server, and the repository's in-memory Kestrel test base is the pattern to follow. Their current status wasn't checked for this episode, so look at the threads before doing anything.

### Then and now, for Tool_Box

The calls you used in July still work. Logging to standard error is still required, for the same reason. Your Inspector handshake failure, caused by a missing .NET 10 runtime, was an initialize handshake that never completed; today the client would try server slash discover first and then fall back. Stateless HTTP, which you switched on by hand, is now the default, and Session Mode is the real setting behind the old flag. Your rule about pinning allowed hosts to prevent DNS rebinding is now front and center in the SDK's own getting-started guide. And the API drift that caught you three times in July came from pre-release churn; version 2.0 is stable, follows semantic versioning, and checks its public surface against version 2.0.0 on every build.

### The common misconceptions, one more time

A common misconception is that MCP runs the model or decides when tools are called. Actually, the MCP host does both; MCP only carries the messages.

A common misconception is that "host" means the same thing everywhere in this repository. Actually, in MCP it means the AI application, and in .NET it means the Generic Host.

A common misconception is that replies come back in the order requests were sent. Actually, they come back in the order they finish, and only the id connects them.

A common misconception is that cancelling a request stops the tool. Actually, cancellation is a notification that sets a flag; the tool has to check it.

A common misconception is that the McpServerTool attribute registers a tool. Actually, With Tools or With Tools From Assembly reads the attribute and does the registering.

A common misconception is that fields on a tool class keep their values between calls. Actually, a new instance is created for every call.

A common misconception is that stateless mode makes a server safe for concurrent use. Actually, it only means the SDK keeps no sessions; your singletons and statics are shared and concurrent.

A common misconception is that a using directive adds a library to your project. Actually, the package reference does; using only shortens names.

A common misconception is that await creates a thread. Actually, it releases one while waiting.

A common misconception is that a tool's exception message reaches the caller. Actually, it's hidden unless you throw an M-C-P Exception.

### The final recap

Here are the ten ideas to take away. They're also what to say back in a teach-back.

One. MCP is a JSON-RPC contract that lets any AI application, through a client, discover and call any tool server. The SDK does the plumbing on both ends. "Host" in MCP is the AI app; in .NET it's the Generic Host.

Two. There are four message shapes, request, response, error and notification, and a reply is matched to its request only by its id.

Three. Up to November 2025, connections began with an initialize handshake and, over HTTP, sessions. From July 2026, every request carries its own version and capabilities, there are no sessions, and the client probes with server slash discover, then falls back if it must.

Four. The layers are protocol types, the transport, the Postmaster engine shared by client and server, the server and client, and the hosting glue. The seam between transport and engine is tiny: a channel of messages in, and a send method out.

Five. At startup, the With calls only write recipes. Run starts the Stage Manager, which needs the Starter, which pulls in the transport, the options with every tool's Interpreter, and the Concierge, which fills the Directory. End of standard input shuts it all down.

Six. One tool call: an id and a Claim Ticket, a line on standard input, the Inbox, a fire-and-forget handling job with a Stop Cord, the Directory, a scope and a Work Order, the Checkpoints, the Interpreter, your method, a tool result, a line on standard output, and the ticket completed.

Seven. Handlers run concurrently and replies can arrive out of order. Cancellation pulls a cord and the tool must cooperate. The forced yield keeps the Postmaster's loop free and prevents a deadlock.

Eight. In stateless HTTP, every POST gets a new server, using the request's scope. Singletons and statics are shared across concurrent requests, which is exactly the voxel world data race.

Nine. Tool failures come back as results marked is error, with messages hidden unless you throw an M-C-P Exception. Protocol failures are JSON-RPC errors.

Ten. Underneath it all: attributes are inert until something reads them; a package delivers, an assembly contains, a namespace names, and using grants nothing; tool classes are created per call; await holds no thread; and a cancellation token stops nothing by itself.

That's the MCP C# SDK from the ground up. When you next open a file in this repository, ask the two questions this whole series has been built on: where does this code come from, and who calls whom?
