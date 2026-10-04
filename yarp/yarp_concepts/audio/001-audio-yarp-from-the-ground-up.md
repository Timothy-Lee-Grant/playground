# YARP From the Ground Up: What It Is, Every Part That Matters, and the .NET Ideas Underneath

Audio lecture 001 for the YARP project. Written on 2026-10-03 to be listened to as a NotebookLM Audio Overview. It's an orientation to the whole of YARP, the reverse proxy library in the dotnet slash yarp repository, plus the .NET and testing ideas you need to read its code and work on issue 275.

Facts in this episode were checked against the YARP source code on its main branch at commit 0cae8ca, from September 11, 2026, which is the same commit your issue 275 branch starts from. The latest stable release on NuGet is version 2.3.0, from February 2025. The main branch is labeled version 3.0.0, preview 1.

## Who this is for and what it covers

This episode is for an engineer who has already done real work around YARP. You ran experiments with WebSockets passing through it for issue 1764, and you opened a documentation pull request from that work. So you've seen YARP from the outside: you configured it, you watched it hang up on quiet connections, and you measured it. What you haven't had yet is the inside view. What are all the pieces? Who calls whom? Where does each piece live in the repository? And which general .NET ideas do you need so the code stops looking like a wall of interfaces?

That last question matters. Several ideas sit underneath YARP that are worth making sharp, not just familiar: the difference between a library and a framework, how the ASP.NET Core middleware pipeline really works, dependency injection and object lifetimes, async code and cancellation, and how unit tests use fake objects. You can get by with a fuzzy version of each of these. YARP is a good place to make them precise, because it uses every one of them, and your next contribution, removing Autofac from YARP's tests, depends on two of them directly.

Here is the key idea for the whole episode, in one sentence. **YARP is a library that turns an ordinary ASP.NET Core app into a reverse proxy by adding one branch to the app's request pipeline: a rulebook decides where a request should go, a short line of middleware picks one destination, and a courier copies the request there and copies the response back.**

The episode has eleven parts. Part one is the problem YARP solves and what kind of thing it is. Part two is library versus framework, and the three lines of code that switch YARP on. Part three is the ASP.NET Core machinery YARP plugs into. Part four is dependency injection. Part five is the rulebook: routes, clusters and destinations. Part six is the main event: one request traveling through YARP, step by step. Part seven is health checks and what happens in the background. Part eight is the rest of the toolbox. Part nine is a tour of the repository. Part ten is testing, mocks and issue 275. Part eleven is the common misconceptions and a final recap.

## Part one: the problem, and what kind of thing YARP is

Start with the problem. A company runs a service, say an orders service, and runs three copies of it on three machines, because one machine isn't enough and because they want to keep serving while one copy crashes or gets upgraded. Without anything in front, every client needs to know all three addresses, pick one, notice when one dies, and retry. Every copy has to handle HTTPS certificates, authentication and logging on its own. And the machines' private addresses are exposed to the internet.

A **reverse proxy** fixes this by being the single front door. Clients talk only to the proxy. The proxy decides which real server should handle each request, forwards it there, and passes the answer back. That one decision point is where you put routing, load balancing, health checking, HTTPS handling, and policies like authentication and rate limits. The word "reverse" is from the servers' point of view: a forward proxy sits in front of clients, like a company's outbound web proxy, and a reverse proxy sits in front of servers.

You covered that in your first YARP lecture, so let's move to what's special about YARP.

YARP stands for "Yet Another Reverse Proxy". The name is a joke with a real story behind it. Around 2019 and 2020, several teams inside Microsoft were each building their own reverse proxy for their own services. Instead of everyone maintaining a slightly different one, the ASP.NET team built one shared, open-source toolkit on top of ASP.NET Core. It started in a repository called microsoft slash reverse-proxy, shipped version 1.0 in late 2021, and now lives at dotnet slash yarp. Big internal Microsoft services run on it, which is part of why performance and correctness get so much attention in code review.

The key idea of this part: **YARP is a toolkit you build a proxy with, not a finished proxy product you install.** Compare it with nginx or Envoy. Those are programs: you download them, write their config file, and run them. You don't write code. With YARP, you write a normal ASP.NET Core application, add the YARP package to it, and the application becomes a proxy. That means you can mix in any C# you like: your own authentication, your own routing decisions, a database of routes, custom logging.

Now a bound on that statement, because it's no longer the whole story. The repository now also contains a ready-made container application, in the folder called src slash Application. It's an opinionated web server plus proxy that you configure purely with a JSON file, no code required. So YARP is mainly a library, and the repository also ships one prebuilt app made from that library. When people say "YARP", they almost always mean the library, which is the NuGet package called Yarp dot ReverseProxy.

Recap of part one. A reverse proxy is the single front door for a set of servers. YARP is Microsoft's shared, open-source reverse proxy toolkit, built on ASP.NET Core. You use it by writing an ASP.NET Core app and adding the package, which is different from installing and configuring a finished product like nginx.

## Part two: library versus framework, and the three lines that switch YARP on

Before going inside YARP, it's worth being exact about two words that get blurred: library and framework.

The difference is about **who calls whom**. With a **library**, your code is in charge. You call the library when you want something, and it returns. The dotnet slash iot packages are like this: your program creates a GPIO controller and calls its methods. With a **framework**, the framework is in charge. It owns the main loop, and it calls your code at the moments it chooses. ASP.NET Core is a framework: it accepts the connections, runs the loop, and calls your code when a request arrives. This is often called inversion of control: the control flow is inverted compared to a library.

So which is YARP? Here's where precision pays off. **YARP is a library that plugs into a framework.** You call YARP's setup methods from your startup code, which is library-style. But what those methods do is register pieces into ASP.NET Core, and from then on, ASP.NET Core calls YARP's code when requests arrive. YARP doesn't own a loop. It doesn't open a socket to listen for clients. It doesn't start threads to handle requests. ASP.NET Core does all of that, and YARP's code runs when ASP.NET Core calls it.

A common misconception is that YARP is a separate server running next to your app, or a framework of its own. Actually, YARP runs entirely inside your ASP.NET Core process, as code that ASP.NET Core calls. If your app also serves a normal web page on one URL and proxies another URL through YARP, both happen in the same process, through the same pipeline.

Now let's anchor this to the shape you know best: the ASP.NET Core builder, then build, then run. A minimal YARP app has three YARP-specific lines, one in each phase.

In the builder phase, you call builder dot Services dot AddReverseProxy, and on the result you call LoadFromConfig, passing it the section of your appsettings file called ReverseProxy. AddReverseProxy registers YARP's services into ASP.NET Core's dependency injection container, which we'll cover in part four. Nothing runs yet. LoadFromConfig registers one more service: a configuration provider that knows how to read routes and clusters from that JSON section.

In the build phase, you call builder dot Build, which gives you the app object. This is plain ASP.NET Core, nothing YARP-specific.

Then, before running, you call app dot MapReverseProxy. This is the line that wires YARP into request handling. It does two things. It builds YARP's own small internal pipeline, which we'll walk through in part six. And it registers YARP's routes with ASP.NET Core's routing system, so that when a request arrives whose URL matches one of your proxy routes, routing hands that request to YARP.

Finally, app dot Run starts the host. Kestrel, the web server, begins accepting connections, and from that moment ASP.NET Core calls into YARP for every matching request.

Here's what YARP does **not** do in those three lines. It doesn't accept connections, decrypt HTTPS, or parse HTTP. Kestrel does those. It doesn't decide the order of your other middleware. You do, by the order you write your code. And it doesn't run on its own schedule; apart from a few background timers like health checks, YARP's code runs only when called.

Recap of part two. A library is called by your code; a framework calls your code. YARP is a library that registers itself into the ASP.NET Core framework. Three lines switch it on: AddReverseProxy with LoadFromConfig in the builder phase registers services, MapReverseProxy before running wires YARP into request handling, and Run starts the server that calls YARP.

## Part three: the ASP.NET Core machinery YARP plugs into

To understand YARP's internals, you need four pieces of ASP.NET Core to be sharp. Let's give each one a character name, because these names will come back in the step-by-step walkthrough.

The first character is **the Listener**. That's Kestrel, ASP.NET Core's built-in web server. The Listener owns the sockets that clients connect to. It accepts TCP connections, handles HTTPS, reads the raw bytes, and parses them into HTTP requests. When a complete request header has arrived, the Listener creates an object to represent it and hands it to the pipeline.

The second character is **the Job Ticket**. That's the HttpContext object, H-T-T-P context. There's exactly one Job Ticket per request, and it travels with the request from start to finish. It holds the incoming request: the method, like GET, the path, the headers, and a stream you can read the body from. It holds the outgoing response: a status code you can set, headers, and a stream you write the body into. And it has a section called Features, which is a slot where any component can attach extra information for later components to find. YARP uses that Features slot heavily. Keep it in mind.

The third character is **the Assembly Line**. That's the middleware pipeline. A middleware is a component that receives the Job Ticket, can do some work, and then either passes the ticket to the next middleware or stops and produces the response itself. The middlewares are chained in the exact order you register them in your startup code.

Here's the mechanism, because this is where the delegates lecture pays off. Each middleware is given a reference to the next one, in the form of a delegate of type RequestDelegate. A RequestDelegate is simply a delegate that takes an HttpContext and returns a Task. So when a middleware "passes the ticket on", it's invoking a delegate, which calls the next middleware's method. When that call returns, control comes back to the first middleware, which can then do more work on the way out, like logging how long the request took.

So the key idea is: **the pipeline is a chain of method calls, each middleware calling the next through a delegate, and each one gets a chance to act both on the way in and on the way out.** A middleware that doesn't call next is called a terminal middleware: the chain ends there and the calls unwind back up.

A common misconception is that each middleware is a separate service, thread or stage that requests are queued into, like stations on a factory conveyor that run in parallel. Actually, for a single request, the middlewares are nested method calls on that request's own flow of execution. The first middleware calls the second, which calls the third. Many requests are handled at the same time, but each one moves through its own chain of calls. The "assembly line" name is about the order of stations, not about separate workers.

The fourth character is **the Sorter**, which is endpoint routing. Routing has two halves. One middleware near the start of the pipeline looks at the request's path, method and host, and compares them with a table of known endpoints. An **endpoint** is a pairing of "which requests match" with "the delegate that handles them", plus a bag of extra information called metadata. When the Sorter finds a match, it attaches the chosen endpoint to the Job Ticket. Later, at the end of the pipeline, the endpoint's delegate is invoked to actually handle the request.

Now the important YARP fact. When you call MapReverseProxy, YARP adds **endpoints** to the Sorter's table, one for each route in your configuration. And every one of those endpoints has, as its handling delegate, YARP's own small internal pipeline. So YARP doesn't sit in the main Assembly Line as one big middleware that inspects every request. Instead, the Sorter picks a YARP endpoint only when the request matches a proxy route, and then that endpoint runs YARP's mini assembly line. Requests that don't match a proxy route never touch YARP at all.

Also, each YARP endpoint carries metadata containing the route it was made from. That's how YARP, later, knows which route matched: it reads the endpoint off the Job Ticket and pulls the route out of its metadata.

Recap of part three. The Listener, Kestrel, owns client connections and creates one Job Ticket, the HttpContext, per request. The Assembly Line is the middleware pipeline: nested calls, each middleware invoking the next through a RequestDelegate. The Sorter, endpoint routing, matches the request to an endpoint. MapReverseProxy adds one endpoint per proxy route, and each endpoint's handler is YARP's own mini pipeline.

## Part four: dependency injection, the Supply Room

Almost every class in YARP takes its collaborators through its constructor, as interfaces. ForwarderMiddleware, for example, takes four things: the next delegate in the chain, a logger, something called an I-HTTP-Forwarder, and something called an I-Random-Factory. Where do those come from? Nobody in YARP's code writes "new ForwarderMiddleware" with those four arguments. They come from dependency injection.

Let's name the character: **the Supply Room**. That's the dependency injection container, which in ASP.NET Core is called the service provider.

The Supply Room works in two phases, and keeping them separate clears up most of the confusion around it.

The first phase is **registration**, which happens in the builder phase of startup. Code says, in effect, "when anyone asks for an I-HTTP-Forwarder, give them an HttpForwarder". That's a line on the Supply Room's list. Nothing is created yet. This is exactly what AddReverseProxy does: it adds dozens of such lines. YARP's registration code lives in a few files in the Management folder, and almost every line uses a method called TryAddSingleton.

The second phase is **resolution**, which happens while the app runs. When ASP.NET Core needs to build ForwarderMiddleware, it asks the Supply Room. The Supply Room looks at the constructor, sees the four parameter types, finds or creates an object for each one using its list, and calls the constructor. And if one of those objects itself needs constructor arguments, the Supply Room builds those first, all the way down. This is called constructor injection.

Why go to this trouble instead of just writing "new"? Two reasons, and they're the same two reasons that matter for issue 275. First, **the class states what it needs and doesn't decide where it comes from.** ForwarderMiddleware says "I need something that can forward HTTP", not "I need this exact HttpForwarder class". That's what the interface is for: it's a contract, a list of methods, with no code. Second, because the class only depends on the contract, **anyone can hand it a different object that fulfills the same contract.** In production, the Supply Room hands it the real forwarder. In a unit test, the test hands it a fake forwarder. Same class, different collaborators.

Now lifetimes. When you register something, you also say how long the object should live. There are three lifetimes. **Singleton** means one object is created the first time it's asked for, and that same object is handed out for the whole life of the app. **Scoped** means one object per request: everyone handling the same request shares one, and the next request gets a fresh one. **Transient** means a new object every time anyone asks.

A common misconception is that dependency injection creates a new object every time something needs one. Actually, that's only true for transient registrations. YARP registers almost all of its services as singletons. And that has a big consequence: **one HttpForwarder object, one load-balancing policy object, one configuration manager, are shared by every request being handled at the same moment**, on many threads at once. So YARP's singletons must be safe to use from many threads simultaneously. That's why YARP's per-request information lives on the Job Ticket, not inside those shared objects, and why the counters YARP keeps, like how many requests are in flight to a destination, use atomic increment and decrement operations.

One more detail about TryAddSingleton. The "Try" means "only add this if nothing is registered for that interface yet". So if you register your own implementation of, say, the HTTP client factory **before** calling AddReverseProxy, YARP's default is skipped and yours is used. That's one of YARP's main extension mechanisms: replace a contract's implementation through the Supply Room.

And here's the link to your issue. **Autofac is also a dependency injection container**, a third-party one, older than ASP.NET Core's built-in one. YARP's product code doesn't use Autofac at all. Only YARP's tests do, through a helper that uses Autofac's container to build classes under test and fill their constructor parameters with fake objects automatically. We'll come back to that in part ten.

Recap of part four. The Supply Room, the DI container, works in two phases: registration at startup, resolution at runtime. Classes depend on interfaces, which are contracts, so tests can swap in fakes. Lifetimes are singleton, scoped and transient, and YARP's services are mostly singletons, which means they're shared across concurrent requests and must be thread-safe. TryAdd lets you replace YARP's defaults. Autofac is another DI container, and YARP uses it only in its tests.

## Part five: the Rulebook, routes, clusters and destinations

Now YARP's own world. Everything YARP does is driven by its configuration, so let's call the configuration **the Rulebook**. The Rulebook has three kinds of entries, and the relationship between them is the single most important structure in YARP.

A **destination** is one real server: an address, like "http colon slash slash ten dot zero dot zero dot five, port five thousand". It's one copy of a backend service.

A **cluster** is a named group of destinations that are interchangeable copies of the same service, plus the rules for how to talk to them. For example, a cluster called orders-cluster with three destinations, one per machine. The cluster is where you put everything about the backend side: which load-balancing policy to use, whether to use session affinity, how to check the destinations' health, how to configure the outgoing HTTP connections, and request settings like the activity timeout you measured in issue 1764.

A **route** is a rule about incoming requests. It says which requests it matches, and which cluster they should go to. The match can look at the path, like "slash api slash orders, followed by anything", and optionally at the host name, the HTTP methods, specific headers, or query parameters. A route can also have an order number, to break ties when two routes could match the same request. And a route is where you put everything about the client side: which authorization policy applies, which CORS policy, which rate limiter, which output cache, a request timeout, and **transforms**, which are edits to make to the request on its way out, like stripping a path prefix.

So the key idea is: **routes describe the front, clusters describe the back, destinations are the actual servers. Many routes can point at one cluster, and a cluster holds one or more destinations.** A request flows from a route, to that route's cluster, to one of that cluster's destinations.

A common misconception is that a route points at a specific server. Actually, a route points at a cluster, by its cluster ID, and the choice of server within the cluster happens later, per request, by load balancing. That indirection is what lets you add or remove servers without touching any route.

Where does the Rulebook come from? Through an interface called I-Proxy-Config-Provider. A config provider is anything that can hand YARP a list of routes and a list of clusters, plus a signal for "this has changed". YARP ships two providers. LoadFromConfig reads the Rulebook out of ASP.NET Core's configuration system, which usually means the ReverseProxy section of the appsettings JSON file. LoadFromMemory takes lists of route and cluster objects you build in C#. And you can write your own provider that reads routes from a database, from a service registry, or from Kubernetes. The repository even contains a Kubernetes controller that does exactly that.

That change signal is called a **change token**. It's an object that flips to "changed" when the source changes, for example when someone edits the JSON file. When YARP sees the token flip, it asks the provider for the new Rulebook and swaps it in, without restarting the app. That's called hot reload.

Now the character who reads the Rulebook: **the Librarian**. In the code, it's a class called ProxyConfigManager, in the Management folder. The Librarian has three jobs. First, it validates the Rulebook: unknown policy names, malformed addresses and similar mistakes get reported. A bad Rulebook at startup stops the app from starting, with an error saying the proxy config is invalid. A bad change later, during hot reload, is logged as an error instead of crashing the running app. Second, it turns the plain configuration records into **live state objects**: a RouteModel for each route, a ClusterState for each cluster, and a DestinationState for each destination. Third, and this is the clever part, the Librarian is itself an endpoint data source. That's the ASP.NET Core type the Sorter reads its endpoint table from. So the Librarian produces one endpoint per route, attaches the RouteModel to that endpoint's metadata, and when the Rulebook changes, it tells the Sorter "my endpoints changed" through another change token.

The difference between configuration and state is worth stating as a rule. **Configuration is what you wrote; state is what YARP knows right now.** Configuration records, like RouteConfig and ClusterConfig, are immutable: once built, they don't change, and a new Rulebook means new records. State objects, like DestinationState, carry live information that changes every second: the destination's current health, and a counter of how many requests are in flight to it right now.

A common misconception is that YARP reads the JSON file on every request. Actually, the Librarian builds everything once, at startup and again on each change, and requests only read the already-built objects. That's part of why YARP is fast.

One more thing the Librarian sets up, which shows the design. A route's authorization policy, CORS policy and rate limiter aren't enforced by YARP code at all. The Librarian simply attaches them to the route's endpoint as metadata, using ASP.NET Core's own standard attributes. Then ASP.NET Core's own authorization middleware, rate-limiting middleware and so on, sitting in the main Assembly Line, see that metadata and enforce it, **before** the request ever reaches YARP's endpoint. YARP reuses the framework's features instead of reinventing them.

Recap of part five. The Rulebook has routes, which match incoming requests, clusters, which group interchangeable servers with backend settings, and destinations, the actual servers. Routes point at clusters, never at servers. Config providers supply the Rulebook and a change token for hot reload. The Librarian, ProxyConfigManager, validates it, builds live state, and publishes one endpoint per route to the Sorter. Configuration is what you wrote and never changes; state is what YARP knows right now.

## Part six: one request's journey through YARP, step by step

Now the main event. We'll follow one request all the way through, and meet the rest of the cast as the request reaches them.

The setup. Your Rulebook has a route called orders-route. It matches the path "slash api slash orders, followed by anything", and points at orders-cluster. Orders-cluster has three destinations, called d1, d2 and d3, all healthy. Load balancing is left at the default. Session affinity is off. Passive health checks are on. A browser sends a GET request for "slash api slash orders slash forty-two".

**Step one: the Listener.** Kestrel accepts the browser's connection, reads the request, and creates a Job Ticket, an HttpContext, for it. The Job Ticket enters the main Assembly Line.

**Step two: the main Assembly Line, and the Sorter.** The request passes through whatever middleware you registered, say HTTPS redirection and authentication. The Sorter compares the path against its endpoint table, finds the endpoint the Librarian created for orders-route, and attaches it to the Job Ticket. If the route had an authorization policy, the authorization middleware would enforce it now. Then the endpoint's delegate runs, and that delegate is YARP's mini pipeline. We've just crossed from ASP.NET Core into YARP.

YARP's mini pipeline, when you call MapReverseProxy with no arguments, has six stations in this order: the Clerk, the Loyalty Desk, the Dispatcher, the Inspector, the Limits station, and the Courier's handoff. The first and the last two are always there; the middle three are the defaults, and you can replace them with your own list by passing a function to MapReverseProxy. Let's take them in turn.

**Step three: the Clerk.** The Clerk is ProxyPipelineInitializerMiddleware, in the Model folder. Its job is to prepare paperwork for everyone downstream. It reads the endpoint off the Job Ticket, pulls the RouteModel out of the endpoint's metadata, and from the route it finds the cluster. If the route's cluster doesn't exist, the Clerk stops right here with a 503, Service Unavailable. Otherwise, it creates a note and attaches it to the Job Ticket's Features slot. Let's call it **the Proxy Note**. In code it's the I-Reverse-Proxy-Feature. The Proxy Note holds the route, the cluster, the list of all the cluster's destinations, and the list of **available** destinations, meaning the ones currently considered healthy enough to use. Here, all three: d1, d2 and d3.

The key idea of the next three stations is this: **each station narrows the list of available destinations on the Proxy Note.** They don't pass the destination to each other as an argument. They edit the shared note on the Job Ticket, and the next station reads it.

**Step four: the Loyalty Desk.** That's session affinity. Some applications keep per-user data in the memory of one server, so a user's requests must keep going to the same server. When affinity is on, the Loyalty Desk looks for a marker on the request, usually a cookie, that names the destination this user was sent to last time. If it finds one and that destination is still available, it narrows the Proxy Note's list to just that destination. If the destination is gone, a failure policy decides whether to quietly pick another one or return an error. In our example, affinity is off, so the Loyalty Desk checks the cluster's settings and immediately calls next. The list is unchanged.

**Step five: the Dispatcher.** That's load balancing. The Dispatcher asks the cluster which policy to use. If none is configured, it uses **Power of Two Choices**. That policy picks two destinations at random, compares how many requests each one has in flight right now, and chooses the less busy one. Why not just pick the least busy of all? Because checking every destination costs time on every request, and if every proxy instance always picks the single least busy server, they can all stampede onto it at once. Two random choices gets most of the benefit with very little cost. The other policies YARP ships are round robin, random, least requests, and first alphabetical. Say the Dispatcher picks d2. It narrows the Proxy Note's list to exactly one destination: d2. Then it calls next.

**Step six: the Inspector.** That's the passive health check middleware. Here's a neat use of the "way in and way out" idea from part three. On the way in, the Inspector does nothing at all. It immediately calls next. Only **after** the rest of the pipeline returns, which means after the request has been forwarded and the response sent, does the Inspector look at the Proxy Note to see which destination was used and how it went. If the forward failed at the transport level, for example the connection was refused or timed out, the Inspector reports that to a health policy, which can mark d2 unhealthy if failures pile up. So the Inspector learns from real traffic, after the fact.

**Step seven: Limits.** A small station. If the route sets its own maximum request body size, this station applies it to the request, replacing Kestrel's default limit for this one request. If the route doesn't set one, it does nothing. Then it calls next.

**Step eight: the Courier's handoff.** That's ForwarderMiddleware, in the Forwarder folder, and it's the terminal station. It reads the Proxy Note's available destinations. If the list is empty, for example because every destination is unhealthy, it sets a 503 and records the error on the Job Ticket. If the list has exactly one destination, it uses it: d2. And here's a detail that matters for your issue. **If the list still has more than one destination**, which happens when someone builds a custom pipeline without a Dispatcher, ForwarderMiddleware picks one at random itself. That's why its constructor asks for an I-Random-Factory: so tests can supply a predictable random number source. Then it records on the Proxy Note which destination was chosen, increments two in-flight counters, one for the cluster and one for d2, and calls the Courier. When the Courier finishes, it decrements both counters, in a finally block, so they're decremented even if something throws. Those counters are what Power of Two Choices compares on the next request.

**Step nine: the Courier.** That's the I-HTTP-Forwarder, whose real implementation is the HttpForwarder class. The Courier's method is called SendAsync, and it receives five things: the Job Ticket, the destination's address, the cluster's outgoing HTTP client, the cluster's request settings, and the route's transformer. The Courier's own source code has a numbered comment listing its steps, and it's one of the best things to read in the whole repository. Here they are in words.

First, it builds a brand-new outgoing request, an HttpRequestMessage, aimed at d2. A proxy never sends the incoming request object itself; it builds a separate outgoing one and copies things across.

Second, it sets up copying of the request body, if there is one, in the background, so a large upload streams through instead of being loaded into memory first.

Third, it copies the request headers, and runs the route's transformer, a character we'll call **the Editor**. By default, the Editor copies most headers as they are, drops the ones that only describe the incoming connection itself, and adds the X-Forwarded headers: X-Forwarded-For, which carries the client's IP address, plus the original scheme, host and path prefix. Why? Because d2 only sees a connection from the proxy, so without those headers it would think every request came from the proxy. Also by default, the Host header on the outgoing request is d2's own host name, not the one the browser used. Your route's transforms run here too, such as removing the "slash api" prefix from the path.

Fourth, it sends the outgoing request using **the Outbound Line**: the cluster's HTTP message invoker, created once per cluster by the ForwarderHttpClientFactory, on top of a SocketsHttpHandler. That handler is set up with redirects off, cookies off, automatic decompression off, and any system proxy ignored. Each of those settings has the same reason behind it: **a proxy must pass the conversation through unchanged.** If the proxy followed redirects or unzipped responses by itself, the browser would get something different from what d2 sent.

Fifth and sixth, when d2 answers, the Courier copies the status code and the response headers onto the Job Ticket's response.

Seventh, there's a fork. If d2 answered with status 101, Switching Protocols, this is an upgrade, which is how a WebSocket starts. The Courier upgrades the browser's side as well, and then runs two copy loops at once, one in each direction, until either side closes. That's exactly the path your issue 1764 experiments went through. Otherwise, the normal case, it copies the response body from d2 back to the browser.

Eighth, it copies any trailers, which are headers that come after the body, as used by gRPC. Ninth, it waits for the background request-body copy from step two to finish. Then it returns a result: either "no error" or a specific error type.

Let's name the two helpers doing the copying, because you've met them before. **The Pump** is the StreamCopier: a loop that reads a chunk from one stream and writes it to another, over and over, without buffering the whole thing. **The Watchdog** is the activity timeout, 100 seconds by default, set per cluster. Every time the Pump moves bytes, it resets the Watchdog. If no bytes move for the whole timeout, the Watchdog cancels the operation. That's the behavior you measured for idle WebSockets.

Now, two .NET ideas the Courier depends on, briefly, because they appear on almost every line.

The first is **async and await**. Nearly every method on this path returns a Task and is awaited. The key idea is: **await means "pause this method until the operation finishes, without holding a thread while waiting."** While the Courier is waiting for d2's response, which may take seconds, no thread sits blocked. The thread goes off to handle other requests, and when the response arrives, the method continues where it left off, possibly on a different thread. That's how one YARP process can have tens of thousands of requests in flight at once. A common misconception is that async code runs on a separate background thread. Actually, async code is about not holding a thread while waiting; it doesn't create threads by itself.

The second is **cancellation tokens**. A CancellationToken is an object that some code can watch to learn "stop, nobody needs this any more". The Courier combines three reasons to stop into one token: the browser disconnected, which ASP.NET Core reports through the Job Ticket; the Watchdog timed out; or the caller asked to cancel. Every awaited operation is given that token, so any of the three stops all the work. Cancellation is cooperative: the token doesn't kill anything by force. Each operation checks it and stops itself.

**Step ten: the way back out.** The Courier returns to ForwarderMiddleware, which decrements the counters. That returns to Limits, which returns to the Inspector, which now does its after-the-fact check on d2. Then the Dispatcher, the Loyalty Desk and the Clerk return in turn, and control unwinds back through your main Assembly Line to the Listener, which finishes the response on the browser's connection.

What if something goes wrong? If d2 refuses the connection, the Courier doesn't throw an exception up the pipeline. It records a ForwarderError on the Job Ticket, with a value saying which stage failed, sets a status code, typically 502 Bad Gateway, or 504 Gateway Timeout for a timeout, and returns. Your own middleware can read that error feature afterwards to log it or customize the response.

Recap of part six. The Listener creates the Job Ticket; the Sorter picks the YARP endpoint for the route. YARP's mini pipeline runs: the Clerk attaches the Proxy Note with the available destinations; the Loyalty Desk, then the Dispatcher, narrow that list, down to one; the Inspector waits and checks the outcome on the way out; ForwarderMiddleware counts in-flight requests and calls the Courier. The Courier builds a new outgoing request, lets the Editor apply transforms and X-Forwarded headers, sends it on the Outbound Line, and the Pump copies bodies while the Watchdog watches for silence. A 101 response turns it into a two-way copy. Await frees threads while waiting, and one cancellation token carries every reason to stop.

## Part seven: health checks, and what happens in the background

In part six, the Clerk put a list of "available" destinations on the Proxy Note. Who decided which destinations were available? That happens outside the request path, and it's worth understanding separately.

Every DestinationState carries two health readings: an **active** one and a **passive** one. Each reading is one of three values: Unknown, Healthy or Unhealthy. Every destination starts as Unknown.

The active reading comes from a character we'll call **the Patrol**. That's the ActiveHealthCheckMonitor. If a cluster enables active health checks, the Patrol runs on a timer, every 15 seconds by default, and sends a probe request to each destination, usually to a health path such as "slash health" on that server. It never touches real client traffic. The probe results go to an active health policy. The default one, called ConsecutiveFailures, marks a destination Unhealthy after a run of failed probes in a row, two by default, and Healthy again when probes succeed.

The passive reading comes from the Inspector you met in step six, the one that checks outcomes on the way out. Its default policy, called TransportFailureRate, watches the rate of transport failures to each destination over a sliding window, 60 seconds by default. If the failure rate crosses a threshold, it marks the destination Unhealthy. Because the passive check has no way to test a destination without sending real traffic, it can't tell when a destination recovers. So after a reactivation period, 60 seconds by default, it sets the destination back to Unknown, which lets traffic flow to it again and gives it another chance.

So the key contrast is: **the Patrol probes on a timer and never touches real requests; the Inspector learns only from real requests, after the fact.** You can use either, both or neither.

Whenever a health reading changes, a component recomputes the cluster's list of available destinations, using an available-destinations policy. The default, HealthyAndUnknown, includes every destination that isn't Unhealthy. The alternative, HealthyOrPanic, does the same, but if that leaves nobody, it "panics" and uses all destinations anyway, on the theory that trying a possibly-broken server is better than refusing everyone.

There's a concurrency design here that connects to the delegates lecture. The cluster's lists of all destinations and available destinations live together in one small object, ClusterDestinationsState, and that object is never modified after it's created. When health changes, YARP builds a **new** object with new lists and swaps the cluster's reference to point at it. A request that already read the old object keeps a complete, consistent list, even if health changes a microsecond later. It's the same rule you learned for delegates: **the object never changes; the reference gets re-pointed.** Be careful how far that comparison goes, though. It's the same technique, immutable snapshots plus a re-pointed reference, used for the same reason, safe reading from many threads. It isn't the same mechanism: delegates are a language feature, and this is just a design choice in YARP's own classes.

Recap of part seven. Each destination has an active and a passive health reading: Unknown, Healthy or Unhealthy. The Patrol probes on a timer; the Inspector learns from real traffic and later resets destinations to Unknown. A policy turns health into the available list, which the Clerk reads per request. The list lives in an immutable snapshot that gets swapped, never edited, so concurrent requests always see a consistent list.

## Part eight: the rest of the toolbox

YARP has more parts than one request uses. Here are the ones you'll meet in the code and in issues, each in a sentence or two.

**Transforms**, which we called the Editor. Beyond the defaults, routes can declare transforms in the Rulebook: path prefix changes, path patterns, adding, removing or rewriting request and response headers, query parameters, and the X-Forwarded or the standard Forwarded headers. In C#, you can register transform providers and factories to add your own, and they all get compiled into one transformer object per route when the Librarian builds the route. They live in the Transforms folder, which is one of the biggest in the project.

**Direct forwarding.** You can skip the Rulebook, routing, load balancing and health checks entirely, and just use the Courier. There's a method called MapForwarder, and you can also ask the Supply Room for the I-HTTP-Forwarder and call SendAsync yourself, with any address you choose. That's for people who want YARP's careful HTTP copying but their own decision logic. The "Direct" sample shows it.

**HTTP.sys delegation.** That's the Delegation folder. HTTP.sys is the web server built into Windows, an alternative to Kestrel. On Windows, YARP can, instead of copying a request's bytes to another server, hand the whole request over to another process's HTTP.sys queue, so that process answers the client directly. It's Windows-only. Two of the seven test files in your issue 275 are in this area, HttpSysDelegatorTests and HttpSysDelegatorMiddlewareTests, and whether they run on your Mac hasn't been checked yet. That's on the plan's Step 2.

**Service discovery.** The ServiceDiscovery folder. By default, destination addresses are used exactly as written. You can add a DNS destination resolver, which looks up a host name and turns one configured destination into one destination per IP address it resolves to, and refreshes that periodically.

**Telemetry.** YARP reports what it's doing in three ways: logs through the standard ILogger, counters and events through an EventSource named Yarp dot ReverseProxy, and distributed tracing through Activities. The Clerk creates an Activity for each proxied request, and the Courier's outgoing request carries trace headers, so a trace can follow a request through the proxy into the backend. A separate package, TelemetryConsumption, lets your code subscribe to those events in a typed way.

**Configuration filters.** An I-Proxy-Config-Filter can inspect and rewrite every route and cluster as the Librarian loads them. That's useful for things like filling in addresses from environment variables.

**The Kubernetes controller** and **the container application.** Two things built on top of the library in the same repository. The first watches Kubernetes Ingress resources and turns them into a Rulebook. The second is the prebuilt, JSON-configured server from part one. Neither is part of the core library.

Recap of part eight. Transforms edit requests and responses per route. Direct forwarding uses the Courier without the rest. HTTP.sys delegation is a Windows-only handoff instead of a copy. Service discovery can expand destinations by DNS. Telemetry comes as logs, EventSource events and traces. Config filters rewrite the Rulebook as it loads.

## Part nine: a walk through the repository

Now let's map all of this onto the folders, so the repository stops being a maze. When you open the dotnet slash yarp repository, there are five top-level folders that matter, plus a handful of files.

**src** holds the shipped code. Inside it, **src slash ReverseProxy** is the main library, the Yarp dot ReverseProxy package. Its subfolders map almost one to one onto the cast. Configuration holds the Rulebook types, like RouteConfig, ClusterConfig and DestinationConfig, plus the config providers and validation. Management holds the Librarian, ProxyConfigManager, and the registration code that AddReverseProxy runs, which is where you can read the Supply Room's list. Routing holds MapReverseProxy and the factory that turns routes into endpoints. Model holds the live state objects, RouteModel, ClusterState, DestinationState, plus the Proxy Note's type and the Clerk. SessionAffinity holds the Loyalty Desk. LoadBalancing holds the Dispatcher and its five policies. Health holds the Patrol, the Inspector and the health policies. Limits holds the Limits station. Forwarder holds the Courier's handoff middleware, the Courier itself, the Pump, the outbound HTTP client factory and the error types. Transforms holds the Editor's pieces. Delegation holds HTTP.sys delegation, ServiceDiscovery holds the DNS resolver, and WebSocketsTelemetry holds extra tracking for WebSocket connections. Next to ReverseProxy, src also has TelemetryConsumption, Kubernetes dot Controller, and Application, the container app.

A useful habit when you open any unfamiliar repository: **find the registration code first.** In YARP, that's the files in the Management folder that AddReverseProxy calls. They list every service, which interface it fulfills, and its lifetime. It's the closest thing to a table of contents for the running system.

**test** holds the tests, as separate projects. ReverseProxy dot Tests is the unit test project: each test builds one class with fake collaborators and checks one behavior. Its folders mirror the source folders, so the tests for ForwarderMiddleware are in test slash ReverseProxy dot Tests slash Forwarder. ReverseProxy dot FunctionalTests is the end-to-end project: those tests start real servers inside the test process, a real proxy and a real backend, and send real HTTP traffic through them, for WebSockets, headers, cancellation, passive health and so on. Tests dot Common holds helpers shared by the test projects, and that's where TestAutoMockBase lives, the helper your issue removes. There are also test projects for the Kubernetes controller and the container application.

**samples** holds small runnable apps, one per feature: a basic sample, a config-file sample, a code-based config sample, transforms, authentication, metrics, direct forwarding, HTTP.sys delegation, Kubernetes, and a sample backend server. When you want to see a feature run, start here.

**docs** holds design notes, operations notes and the roadmap. A common misconception, and one you already ran into on issue 1764, is that this is where YARP's user documentation lives. Actually, the user-facing documentation lives in the dotnet slash AspNetCore dot Docs repository, which is why your documentation pull request went there.

**eng** holds the build infrastructure. YARP builds with Arcade, the shared build system used across the dotnet organization. The file eng slash Versions dot props sets the versions of every package the repository depends on, which is why issue 275 has to remove two lines from it: the Autofac and Autofac Extras Moq versions.

And the files at the root. global dot json pins the exact .NET SDK, currently a .NET 11 release candidate. The scripts restore dot sh, build dot sh and test dot sh install that SDK into a dot-dotnet folder inside the clone, build everything, and run the tests. The azure-pipelines files define continuous integration on Azure DevOps, which is what runs on your pull request; your CI lecture covers how that works. And CONTRIBUTING dot md holds the contribution rules, which the plan's Step 2 has the CLI summarize before your PR.

Recap of part nine. src slash ReverseProxy is the library, with one folder per character. test has the unit tests, mirroring the source folders, the functional tests with real servers, and the shared test helpers. samples shows each feature running. docs is design notes; user docs live in AspNetCore dot Docs. eng and Versions dot props hold the build setup and package versions. Start reading any repository at its registration code.

## Part ten: testing, mocks, and what issue 275 is about

This part connects everything to the work in front of you. It covers what a unit test is made of, what a mock is, what Autofac's AutoMock adds, and why the issue wants it gone.

Start with purpose. A unit test checks one class's behavior in isolation, quickly, without a network or real servers. But classes in YARP depend on other things through their constructors. To test ForwarderMiddleware on its own, without actually sending HTTP, you need a stand-in for the Courier: an object that fulfills the I-HTTP-Forwarder contract, but whose behavior the test controls. That stand-in is called a **mock**, or more generally a fake. This only works because of part four: ForwarderMiddleware depends on an interface, not on the concrete HttpForwarder class.

Every unit test makes three moves, usually called **arrange, act, assert**. Arrange: build the real object under test, build the fakes, and script the fakes' answers. Act: call the real code. Assert: check the outcome, either a result or a state change, or that the real code made a particular call on a fake.

YARP's tests build fakes with a library called **Moq**. Here's what its pieces do, described in words. You create a mock by writing new Mock, with the interface in angle brackets, for example a Mock of I-HTTP-Forwarder. That Mock object is a controller for the fake, not the fake itself. Its property called Object is the actual fake, the thing you pass into a constructor. You script it with Setup: "when SendAsync is called with these arguments, return this". And afterwards you can check it with Verify: "was SendAsync called, with arguments matching these conditions?"

One more Moq idea matters for your issue: **loose versus strict**. A loose mock answers any call nobody scripted with a default value: zero, null, false, or an empty result. A strict mock throws an exception on any unscripted call. New Mock, with nothing else, is loose by default.

Now let's walk one real test, in words, from the ForwarderMiddleware test file. It's called Invoke underscore Works, and here's its object graph, meaning which objects are real and which are fake. Real: the ForwarderMiddleware under test. Real: a DefaultHttpContext, which is a Job Ticket built by hand, with method GET, scheme https, host example dot com and path slash api slash test. Real: a ClusterState and one DestinationState, d1 in effect, with the address https colon slash slash localhost port 123. Real: a Proxy Note, attached to the Job Ticket's Features by the test itself, with d1 as the only available destination. Notice that the test is doing the Clerk's job by hand, because the Clerk isn't part of this test. Fake: the I-HTTP-Forwarder, the Courier.

The arrange step scripts the fake Courier: when SendAsync is called with this Job Ticket, this address, this HTTP client and request settings whose timeout and version match, then signal "I've started", wait until the test says "go on", and return "no error". The act step calls Invoke on the real middleware, without waiting for it to finish. The assert steps are the interesting part. Before the call, the cluster's and d1's in-flight counters are zero. While the fake Courier is paused mid-request, both counters are one. That proves the middleware counts requests in flight around the forward. After the test lets the fake finish, the counters go back to zero. And Verify confirms SendAsync was called with exactly the expected arguments.

So the key idea is: **the test controls time and answers through the fake, and checks the real middleware's behavior around it.**

Now, how did that test get its ForwarderMiddleware and its fake Courier? Not with "new". The test class inherits from **TestAutoMockBase**, and calls two helper methods. Create, with ForwarderMiddleware in angle brackets, builds the middleware. And Mock, with I-HTTP-Forwarder in angle brackets, returns the mock for the Courier. Here's what happens underneath. TestAutoMockBase holds an **AutoMock**, from the package Autofac dot Extras dot Moq, created in loose mode. AutoMock contains an Autofac container, a Supply Room just for this test. When you call Create, the container looks at ForwarderMiddleware's constructor, sees its four parameters, and for every interface parameter it doesn't already have, it makes a loose Moq mock on the spot and passes it in. When you call Mock for an interface, it returns **the same mock object** it already injected, or makes one and remembers it for the next Create. And Provide lets a test say "for this type, use this exact object instead".

This is convenient. The test never mentions the logger or the random factory; AutoMock fills them in silently. And that convenience is precisely the issue's complaint. **AutoMock hides what the class needs.** Reading the test, you can't see that ForwarderMiddleware has four dependencies, or which ones the test cares about. It adds two packages to the test projects for that convenience, and the tests aren't really shorter, because each test still scripts the mocks it cares about.

The replacement is explicit construction. Each test class creates its mocks as ordinary fields, and has one small private method that calls the real constructor directly: the next delegate, a no-op logger, the Courier mock's Object, and a random factory. Every dependency is now visible on one line.

And there are two traps, both of which follow from what you just heard.

The first is **the shared-mock trap**. In the old style, the mock a test scripts with Setup, and checks with Verify, is the same object that was injected into the class, because AutoMock returns the same one. In the new style, if a test creates one mock for the constructor and a different mock to Setup and Verify, the class under test talks to the first mock while the test checks the second one. Verify can then fail, or worse, a test that expects no call can pass for the wrong reason, because nobody ever looks at the right mock. The rule: **the mock you script and check must be the very object you passed into the constructor.**

The second is **the loose-default trap**. AutoMock's mocks are loose. If a hand-written replacement uses a strict mock, or a different kind of stand-in, calls that used to quietly return defaults would now throw, and tests would change behavior. So the replacements must stay loose: plain new Mock, and for loggers, the standard NullLogger, which simply discards log messages.

And one rule over both: **this is a refactor, so no test may change what it proves.** That's why the plan has a proof step for every rewritten file: temporarily break the product code that a test guards, confirm the test goes red, then put the code back. A test that stays green when the code it guards is broken isn't proving anything.

A common misconception to clear up: AutoMock is not how YARP's product code gets its dependencies. In production, ASP.NET Core's own Supply Room builds ForwarderMiddleware. Autofac exists only inside the tests, as a convenience for building objects with fakes. Removing it changes nothing about how YARP runs.

Recap of part ten. Unit tests isolate one class by giving it fakes through its constructor, which works because it depends on interfaces. Every test arranges, acts and asserts. Moq's Mock is a controller; its Object is the fake; Setup scripts it; Verify checks calls; new mocks are loose. AutoMock uses an Autofac container to build the class and auto-fill every constructor parameter with a loose mock, and returns the same mock later. Issue 275 replaces that with explicit construction, minding the shared-mock trap and the loose-default trap, and proving every rewritten test still goes red when its code is broken.

## Part eleven: common misconceptions, and the final recap

Let's collect the misconceptions from the whole episode in one place, each as the wrong version and then the right one.

A common misconception is that YARP is a separate server or a framework of its own. Actually, YARP is a library running inside your ASP.NET Core process, and ASP.NET Core calls it.

A common misconception is that YARP inspects every request as one big middleware. Actually, YARP adds one endpoint per route, and its mini pipeline runs only for requests the Sorter matched to a proxy route.

A common misconception is that middlewares are separate workers that requests are queued between. Actually, for one request they are nested method calls, each invoking the next through a RequestDelegate.

A common misconception is that a route points at a server. Actually, a route points at a cluster, and the server is chosen per request.

A common misconception is that YARP reads its configuration on every request. Actually, the Librarian builds live state once, and again only when the change token says the source changed.

A common misconception is that dependency injection makes a new object for every use. Actually, that depends on the lifetime, and YARP's services are mostly singletons shared by all concurrent requests, which is why they must be thread-safe.

A common misconception is that the browser has one connection to the backend through YARP. Actually, there are two separate connections, and YARP owns one end of each. The Courier builds a new outgoing request rather than passing the original along.

A common misconception is that async code runs on background threads. Actually, await releases the thread while waiting and continues later, possibly on another thread.

A common misconception is that Autofac is part of how YARP runs. Actually, it's only in the tests, as AutoMock, and issue 275 removes it without touching product code.

And here is the final recap, the ten ideas to be able to say back in your own words.

One. YARP is a reverse proxy toolkit: a library you add to an ASP.NET Core app, not a finished product you install.

Two. It's a library that plugs into a framework: AddReverseProxy registers services, MapReverseProxy wires routes into routing, and from then on ASP.NET Core calls YARP.

Three. The middleware pipeline is a chain of nested calls through RequestDelegates; each middleware acts on the way in and the way out, and a terminal one ends the chain.

Four. MapReverseProxy adds one endpoint per route, and each endpoint runs YARP's mini pipeline: the Clerk, the Loyalty Desk, the Dispatcher, the Inspector, Limits, and the Courier's handoff.

Five. Routes describe the front and point at clusters; clusters group interchangeable destinations and describe the back. The Librarian turns configuration, which never changes, into live state, and reloads it on a change token.

Six. The middle stations narrow the list of available destinations on the Proxy Note, which is attached to the Job Ticket; ForwarderMiddleware counts in-flight requests and calls the Courier.

Seven. The Courier builds a new outgoing request, applies transforms and X-Forwarded headers, sends it on an outbound client that changes nothing, and the Pump copies bodies while the Watchdog cancels after silence. A 101 response becomes a two-way copy.

Eight. Dependency injection works in two phases, registration and resolution; classes depend on interfaces; YARP's services are mostly singletons, so per-request data lives on the Job Ticket.

Nine. Health comes from the Patrol, which probes on a timer, and the Inspector, which learns from real traffic; the available list is an immutable snapshot that gets swapped, never edited.

Ten. YARP's unit tests give each class fakes made with Moq; AutoMock fills in constructor parameters through an Autofac container and hides the dependencies. Issue 275 replaces it with explicit construction, and the two traps are using a different mock than the one injected, and losing the loose defaults.

That's the whole of YARP, from the outside in. The next time you open the repository, start in the Management folder to see the cast, read the numbered comment at the top of the Courier's SendAsync method, and then open one test in the Forwarder test folder and name, out loud, which objects are real and which are fakes.
