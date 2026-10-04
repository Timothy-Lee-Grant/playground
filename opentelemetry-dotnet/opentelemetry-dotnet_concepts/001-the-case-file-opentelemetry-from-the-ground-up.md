# Lecture 001: The Case File

## OpenTelemetry from the ground up: what it is, every part that matters, and how a span travels from one line of C# to a dashboard

> **Prompted by:** Timothy's request (2026-10-04) for an in-depth orientation to OpenTelemetry, plus the concepts he's
> weakest on that this project leans on. Feeds the 📡 Telemetry track in
> [`scouting/001-issue_shortlist_sept_2026.md`](../../scouting/001-issue_shortlist_sept_2026.md) (items L, N, O, Q, R).
> · **Date:** 2026-10-04
> · **Source checked against:** `open-telemetry/opentelemetry-dotnet` `main` @ `f9dd754` (2026-10-02; latest stable
> release **1.19.1**) and `open-telemetry/opentelemetry-dotnet-contrib` `main` @ `d3588e5` (2026-10-03). File paths
> below are from those commits. Both repos' `global.json` pin the **.NET 10.0.401 SDK**.
> · **Prerequisites:** none required. Helpful: ASP.NET Core audio A001 (builder → app → run, the pipeline, DI
> lifetimes), iot concepts 002 (delegates and events).
> · **Companion:** audio lecture [`audio/001-audio-opentelemetry-from-the-ground-up.md`](audio/001-audio-opentelemetry-from-the-ground-up.md)
> covers the same ground, with the same character names, for listening.
>
> **Evidence status:** every file path, class name, default value and code excerpt was read from the source at the
> commits above. **Nothing in this lecture was run** (no .NET SDK was available in this session). The "try it"
> programs in §0.5 and §9.5 are **unverified** until you run them; their expected output is a prediction.

---

## How to read this (it's long on purpose, so here's the short path)

You asked for "very detailed and in-depth". It is. So it's built to be read in layers, and you can stop after any of
them with a complete (if shallow) picture:

| Path | Sections | Time | You'll be able to... |
|---|---|---|---|
| **Core** | §0, §0.5 (run it), §1, §3, §6, §8 | ~45 min | Explain what OTel is, name every character, and trace one span from `StartActivity` to the wire |
| **Distributed** | + §9, §10 | +30 min | Explain how one trace crosses a process boundary, and why the Baggage bug (#7449) happens |
| **Signals** | + §11, §12, §13, §14, §15 | +45 min | Explain metrics, logs, sampling, the Collector and semantic conventions |
| **Contributor** | + §16 to §21 | +45 min | Find your way around the repo, read a test, and know the contribution rules |

Each section opens with a **one-line rule** in bold. If you only read those lines, you get the skeleton.

---

## 0. The whole lecture on one screen

```
   YOUR PROCESS (an ASP.NET Core app)                                      ANOTHER PROCESS
 ┌──────────────────────────────────────────────────────────────────┐    (the Sorting Office =
 │  your code / a library                                           │     OpenTelemetry Collector,
 │     │  source.StartActivity("PlaceOrder")                        │     or the Archive directly)
 │     ▼                                                            │
 │  REPORTER (ActivitySource) ──"anyone listening?"──► BUREAU CHIEF │
 │     │                                    (TracerProvider SDK)    │
 │     │◄── SELECTOR (Sampler): "record this one" ──────┘           │
 │     ▼                                                            │
 │  TIMECARD (Activity) clipped to the CLIPBOARD (Activity.Current) │
 │     │  tags, events, status ... then Dispose() = stop            │
 │     ▼                                                            │
 │  MAILROOM (BatchExportProcessor): drops the card in the          │
 │  OUT-TRAY (lock-free ring buffer, 2048 slots)                    │
 │     │  MAIL CARRIER thread wakes at 512 cards or every 5 s       │
 │     ▼                                                            │
 │  PACKER (OtlpTraceExporter): serialize to OTLP protobuf ─────────┼──► :4317 (gRPC) or :4318 (HTTP)
 └──────────────────────────────────────────────────────────────────┘
          │ outgoing HTTP call: the STAMPER writes a `traceparent` header
          ▼
   NEXT SERVICE: its Stamper reads the header, so its Timecards join the same CASE FILE (trace)
```

1. **OpenTelemetry is a standard plus implementations, not a product.** It defines *what* telemetry looks like
   (the spec, the semantic conventions, the OTLP wire format) and ships libraries and a Collector that produce and
   move it. Storing and displaying it is someone else's job (Jaeger, Prometheus, Grafana, Azure Monitor, the Aspire
   dashboard...).
2. **In .NET, the "API" is mostly built into the runtime.** `Activity` *is* a span, `ActivitySource` *is* a tracer,
   `Meter` *is* a meter, `ILogger` is the log API. OpenTelemetry .NET supplies the **SDK** that listens to them.
3. **Nothing is recorded unless someone is listening.** `StartActivity` returns `null` when no listener wants that
   source, or when the sampler says no. That's why every call site writes `activity?.SetTag(...)`.
4. **A trace crosses processes only because a header carries it.** `traceparent` = trace ID + parent span ID + a
   sampled flag. No header, no connection between the two services' spans.
5. **Export is asynchronous and lossy by design.** Ended spans go into a bounded queue; a background thread ships
   them in batches. Full queue → the span is dropped and counted, never blocking your request.
6. **The SDK must be disposed to flush.** Whatever is still in the queue at shutdown is lost unless the provider is
   disposed (or `ForceFlush` is called).

---

## 0.5 Run it first: a five-minute walking skeleton (unverified)

You learn from contact with a running thing, so do this before §1. It's a console app, no web server, no Docker.

```bash
mkdir otel-skeleton && cd otel-skeleton
dotnet new console
dotnet add package OpenTelemetry.Exporter.Console     # pulls in OpenTelemetry (the SDK) too
```

```csharp
// Program.cs
using System.Diagnostics;          // Activity, ActivitySource: from the .NET runtime, not from OpenTelemetry
using OpenTelemetry;               // Sdk
using OpenTelemetry.Trace;         // TracerProviderBuilder extension methods

var source = new ActivitySource("Demo.Shop");                 // the Reporter, named "Demo.Shop"

using var tracerProvider = Sdk.CreateTracerProviderBuilder()  // the Bureau Chief
    .AddSource("Demo.Shop")                                   // "listen to the Demo.Shop reporter"
    .AddConsoleExporter()                                     // a Packer that writes to stdout
    .Build();

using (var order = source.StartActivity("PlaceOrder"))        // Timecard 1 (the root span)
{
    order?.SetTag("order.items", 3);
    using (var pay = source.StartActivity("ChargeCard"))      // Timecard 2: parent is Timecard 1, automatically
    {
        pay?.SetTag("payment.method", "card");
        Thread.Sleep(50);
    }                                                         // Dispose() stops ChargeCard here
}                                                             // ...and PlaceOrder here
```

`dotnet run`. **Predicted output:** two blocks printed by the console exporter, **`ChargeCard` first** (it ends first;
spans are exported when they *end*, not when they start). Both show the **same `Activity.TraceId`**; `ChargeCard`'s
`Activity.ParentSpanId` equals `PlaceOrder`'s `Activity.SpanId`; `PlaceOrder` has no parent.

**Then break it on purpose** (this is the part that makes it stick):

| Break | Change | Predicted result | The lesson |
|---|---|---|---|
| B1 | `.AddSource("Demo.Other")` | Nothing printed. `order` is `null` | No listener for that source → no Activity at all (rule 3) |
| B2 | Delete `.AddSource(...)` and the `?` in `order?.SetTag` | `NullReferenceException` | Why the `?.` is everywhere |
| B3 | Restore; move `Thread.Sleep(50)` *outside* the inner `using` block | `ChargeCard`'s duration drops to ~0 ms | A span measures exactly the code between start and stop |

Save the output as evidence if you run it (`sample/evidence/` convention, root `CLAUDE.md` §4.2).

---

## 1. What problem does this solve?

**Rule: observability is the ability to answer a question about a running system that you didn't know you'd need to
ask, from the data it already emits.**

### 1.1 The situation without it

A user reports: "checkout took 9 seconds at 14:02." Your system is five services: a gateway (YARP), an orders
service, a payments service, an inventory service, a Postgres database, plus a Kafka topic. Each one writes its own
log file. To answer "where did the 9 seconds go?" you would have to:

- find the gateway's log line for that request (by time and URL, hoping the clocks agree),
- guess which orders-service log line is the same request (there's no shared ID),
- repeat for every hop, including the asynchronous one through Kafka,
- and you still wouldn't know *how long* each hop took unless someone happened to log start and end times.

A single-process program doesn't have this problem: a debugger, a stack trace, or a profiler shows the whole story.
**The moment a request crosses a process boundary, the story is scattered across machines.**

### 1.2 The three questions, and the three signals that answer them

| Question an operator asks | Signal | What one data point looks like |
|---|---|---|
| "Is something wrong, and how much?" (rates, error %, latency percentiles) | **Metrics** | `http.server.request.duration` histogram, for `GET /orders/{id}`, status 200: count 1,204, buckets... |
| "Where did *this* request spend its time, across all services?" | **Traces** (made of **spans**) | Span `GET /orders/{id}`, 9.1 s, child of the gateway's span, trace ID `4bf9...` |
| "What exactly happened at that point?" | **Logs** | `"Card declined: insufficient funds"` with trace ID `4bf9...` attached |

The power comes from **correlation**: a metric alert links to example traces (via *exemplars*, §11.6), and a trace
links to the logs written while each of its spans was active (via the trace ID stamped on every log record, §12). One
ID ties the three together.

### 1.3 Why a *standard* was needed

Before OpenTelemetry, every vendor (Datadog, New Relic, Dynatrace, Application Insights, Jaeger, Zipkin...) shipped
its own agent, its own API, and its own wire format. Two consequences:

1. **Library authors couldn't instrument their libraries.** If the Kafka client wanted to emit spans, whose API should
   it call? Picking one vendor's SDK would force it on every user.
2. **Switching vendors meant re-instrumenting every service.** Lock-in by code.

OpenTelemetry fixes both by separating **producing** telemetry (a vendor-neutral API that libraries can call with
zero cost when nobody is listening) from **shipping and storing** it (pluggable exporters and a standard wire format,
OTLP, that every vendor now accepts).

### 1.4 History, in one table

| Year | Event |
|---|---|
| 2016–2017 | **OpenTracing** (a tracing API standard, CNCF) and **OpenCensus** (Google's tracing + metrics libraries) compete |
| 2019 | They merge into **OpenTelemetry** under the CNCF |
| 2021–2023 | Tracing, then metrics, then logs reach stable status in the spec and in major languages |
| May 2026 | OpenTelemetry becomes a **CNCF graduated** project (the same tier as Kubernetes and Prometheus) |
| 2026 | **Profiles** (a fourth signal: CPU/memory profiling) is in public alpha; declarative (YAML) configuration became stable in the spec |

---

## 2. What OpenTelemetry is, and what it is not

**Rule: OpenTelemetry produces, describes and transports telemetry. It does not store it, query it, or draw it.**

| OpenTelemetry **is** | OpenTelemetry **is not** |
|---|---|
| A **specification**: the language-neutral rules every implementation follows | A database. Spans and metrics have to go *somewhere* else |
| **Semantic conventions**: the agreed names for attributes and metrics (`http.request.method`, `db.system.name`...) | A dashboard or UI |
| **OTLP**: the OpenTelemetry Protocol, a protobuf wire format over gRPC or HTTP | An alerting system |
| **APIs and SDKs** in ~12 languages (C#, Java, Go, Python, JS, ...) | A vendor. It's run by a foundation (CNCF) and contributors from many companies |
| **Instrumentation libraries** that make popular frameworks emit telemetry | Automatic. Your *own* business logic emits nothing until you write `StartActivity` (or a library does it for you) |
| **The Collector**: a stand-alone program that receives, processes and forwards telemetry | Required. Apps can export straight to a backend |

> **Common misconception:** "OpenTelemetry is a monitoring tool, like Grafana." Actually, OpenTelemetry stops at the
> wire. Grafana (or Jaeger, Prometheus, Azure Monitor, the Aspire dashboard) is the **Archive** at the other end.
> When you see a pretty trace waterfall, OTel made the data; something else drew it.

---

## 3. The project map: dozens of repos, and which ones matter to you

**Rule: there's one specification, one set of conventions, one wire format and one Collector, and then one
implementation repo (or a few) per language. You'll live in the .NET ones.**

```
                         open-telemetry (GitHub org, CNCF)
 ┌────────────────────────────────────────────────────────────────────────────────────────────┐
 │  LANGUAGE-NEUTRAL (the rules)                                                              │
 │    opentelemetry-specification ── the CHARTER: what an API/SDK/exporter must do            │
 │    semantic-conventions        ── the DICTIONARY: attribute & metric names, span names     │
 │    opentelemetry-proto         ── the CRATE design: OTLP protobuf definitions              │
 │                                                                                            │
 │  THE PIPELINE PROGRAM (Go)                                                                 │
 │    opentelemetry-collector        ── core Collector                                        │
 │    opentelemetry-collector-contrib── 100s of receivers/processors/exporters                │
 │                                                                                            │
 │  PER LANGUAGE (one SIG = "special interest group" each)                                    │
 │    .NET:  opentelemetry-dotnet              ◄── core: API, SDK, OTLP/Console/InMemory/...  │
 │           opentelemetry-dotnet-contrib      ◄── everything else: instrumentations,         │
 │                                                 resource detectors, extra exporters        │
 │           opentelemetry-dotnet-instrumentation ── "zero-code" auto-instrumentation agent   │
 │    Java, Go, Python, JS, ... : same shape                                                  │
 └────────────────────────────────────────────────────────────────────────────────────────────┘
```

| Repo | What lives there | Who changes it | Relevance to you |
|---|---|---|---|
| `opentelemetry-specification` | The rules, written in prose with MUST/SHOULD/MAY | Spec approvers, after cross-language discussion | You read it to know what the .NET SDK is *supposed* to do. Reviewers cite it |
| `semantic-conventions` | Names and meanings: `http.route`, `server.address`, `error.type`... and their stability | Semconv working groups | Most instrumentation PRs are about matching these (§15) |
| `opentelemetry-dotnet` | API extensions, SDK, core exporters, hosting integration | .NET SIG maintainers/approvers | Scouted item N (#7449 Baggage) |
| `opentelemetry-dotnet-contrib` | ~50 components (`src/` has 53 entries), each with named **component owners** | Owners per component, listed in `.github/component_owners.yml` | Items Q (#4473 InfluxDB backpressure) and R (#4516 GC metrics) |
| `opentelemetry-dotnet-instrumentation` | An agent that instruments an app with **no code changes** (via the .NET CLR profiling API + startup hooks) | Separate SIG | Out of scope for now; know it exists |
| `opentelemetry-collector(-contrib)` | The Collector, in Go | Collector SIG | You'll *run* it, not change it |

**Why core vs. contrib?** Core holds what the spec *requires* every language to have (API, SDK, OTLP exporter,
console/in-memory exporters) and is released as one versioned unit. Contrib holds everything optional: instrumentation
for specific libraries (ASP.NET Core, HttpClient, SqlClient, Redis, Kafka...), cloud resource detectors, and
vendor/third-party exporters. Each contrib component ships on its own schedule with its own owners. That's the same
"required vs. optional, slow vs. fast" split you saw between the ASP.NET Core shared framework and its separately
shipped NuGet packages. **Where the analogy stops:** contrib components are *not* reviewed only by the core
maintainers; the component owners are the first reviewers, and core maintainers merge.

---

## 4. The signals, precisely

**Rule: a trace is a tree of spans sharing one trace ID; a metric is a number aggregated over time per combination of
attributes; a log is a timestamped record that can carry the current trace ID. All three carry the same Resource.**

### 4.1 Traces and spans: anatomy of a span

A **span** is one timed operation. A **trace** is all the spans for one end-to-end request, connected by parent
links.

| Field | Example | What it's for |
|---|---|---|
| Trace ID | `4bf92f3577b34da6a3ce929d0e0e4736` (16 bytes, 32 hex chars) | Shared by every span in the trace. The "case number" |
| Span ID | `00f067aa0ba902b7` (8 bytes, 16 hex chars) | This span's own ID |
| Parent span ID | (the caller's span ID, or empty for the root) | Builds the tree |
| Name | `GET /orders/{id}` | Low-cardinality description of the *kind* of operation (§15) |
| Kind | `Server`, `Client`, `Internal`, `Producer`, `Consumer` | Which side of a call this is. A client span in service A is usually the parent of a server span in service B |
| Start / end time | timestamps | Duration = end − start |
| Attributes (in .NET: **tags**) | `http.request.method=GET`, `http.route=/orders/{id}` | Searchable key/values |
| Events | `exception` at +120 ms with a stack trace | Timestamped points inside the span |
| Links | another trace's span context | "Related to, but not my parent" (e.g. a batch job processing 50 queued messages) |
| Status | `Unset`, `Ok`, `Error` + description | Did it fail? |

```
 trace 4bf9...                                    time ──────────────────────────────────►
 ├─ [gateway]   SERVER  GET /checkout             ████████████████████████████████████  9.1 s
 │  └─ [gateway] CLIENT POST                       ███████████████████████████████████
 │     └─ [orders] SERVER POST /orders              ██████████████████████████████████
 │        ├─ [orders] CLIENT  postgres INSERT        ██                                 0.04 s
 │        └─ [orders] CLIENT  POST (payments)          ████████████████████████████████
 │           └─ [payments] SERVER POST /charge           ███████████████████████████   8.7 s  ◄ found it
```

That picture (a "waterfall") is what a trace backend draws from the parent links. **Notice the pairs**: every
`CLIENT` span in one service has a matching `SERVER` child in the next. That pairing is propagation (§9).

### 4.2 Metrics

A **metric** is not a list of events. The SDK **aggregates** measurements in memory and periodically exports the
*aggregate*: a running sum, a current value, or a histogram (counts per bucket + sum + min/max). One
`http.server.request.duration` histogram might have one row per (method, route, status code) combination. Each row is
a **metric point** (in .NET: `MetricPoint`, §11). Metrics are cheap no matter how much traffic there is; spans cost
per request.

### 4.3 Logs

OpenTelemetry did **not** invent a new logging API for .NET. You keep using `ILogger`. The OTel SDK plugs in as a
logging provider (the same way the console logger does), turns each log call into a `LogRecord`, stamps it with the
current trace ID and span ID, and exports it like everything else.

### 4.4 Baggage

**Baggage** is a set of key/value pairs (e.g. `tenant=contoso`) that travels *with* a request, across services, so
downstream code can read it. It's not telemetry by itself; nothing records it unless code copies it onto a span or log.
It rides in a separate `baggage` HTTP header. (Bug #7449, §10.4, is about baggage.)

### 4.5 Resource

A **Resource** describes *who produced* the telemetry: `service.name=orders`, `service.version=2.3.1`,
`host.name=...`, `k8s.pod.name=...`. It's attached once per export batch, not per span. If you set nothing, the .NET
SDK uses `service.name = unknown_service:<process name>` (`src/OpenTelemetry/Resources/ResourceBuilder.cs`,
`PrepareDefaultResource`). Setting `service.name` is the first thing every real app should do.

### 4.6 Profiles (just so you know the word)

A fourth signal, continuous profiling (which functions use CPU/memory), entered **public alpha** in 2026. Not in the
.NET SDK in any stable form; ignore it for now.

---

## 5. The .NET twist: the API is (mostly) already in the runtime

**Rule: in .NET, libraries instrument with `System.Diagnostics` types that ship inside the runtime. OpenTelemetry .NET
is mostly the *listener* side: the SDK that subscribes to those types, plus exporters.**

### 5.1 Why .NET is different from every other language

In Java or Python, a library that wants to emit spans calls `io.opentelemetry.api.trace.Tracer` (Java) or
`opentelemetry.trace` (Python): the OpenTelemetry API package. In .NET it calls `System.Diagnostics.ActivitySource`,
which ships with the runtime itself.

History explains it. .NET had a class called `Activity` (in the `System.Diagnostics.DiagnosticSource` assembly) years
before OpenTelemetry existed; ASP.NET Core and `HttpClient` already used it for correlation IDs. When OpenTelemetry
arrived, the .NET runtime team and the OTel .NET SIG agreed (around .NET 5, 2020) to **extend `Activity` until it
matched the OTel span model**, add `ActivitySource` and `ActivityListener`, and later add `Meter`/`MeterListener`
(.NET 6). The benefit is big: the runtime and every Microsoft library (ASP.NET Core, HttpClient, SqlClient, gRPC,
SignalR, Blazor...) can emit OTel-compatible telemetry **with no dependency on any OpenTelemetry package**.

### 5.2 The translation table (memorize this one)

| OpenTelemetry concept (spec) | .NET type you'll see in code | Where the type comes from |
|---|---|---|
| Tracer | `ActivitySource` | `System.Diagnostics.DiagnosticSource` (in the .NET runtime) |
| Span | `Activity` | same |
| Span attributes | `Activity.SetTag` / `Activity.TagObjects` | same |
| Span context (trace ID, span ID, flags, state) | `ActivityContext` | same |
| Span kind | `ActivityKind` | same |
| Span status | `Activity.SetStatus(ActivityStatusCode...)` | same |
| "current span" | `Activity.Current` (an `AsyncLocal`, §10) | same |
| Span processor hook | `ActivityListener` (callbacks) | same; the SDK *creates* one |
| Meter, instruments | `Meter`, `Counter<T>`, `Histogram<T>`, `Gauge<T>`, `ObservableGauge<T>`... | same (`System.Diagnostics.Metrics`) |
| Logs API | `ILogger` | `Microsoft.Extensions.Logging.Abstractions` (dotnet/runtime) |
| Baggage | `OpenTelemetry.Baggage` | **OpenTelemetry.Api** NuGet package ⚠ (`Activity.Baggage` also exists, and is a *different* thing, §10.5) |
| Propagators | `OpenTelemetry.Context.Propagation.TraceContextPropagator`, `BaggagePropagator` | **OpenTelemetry.Api** |
| TracerProvider / MeterProvider / LoggerProvider (SDK) | `TracerProviderSdk`, `MeterProviderSdk`, `LoggerProviderSdk` (internal) | **OpenTelemetry** NuGet package (the SDK) |
| Sampler, processor, exporter | `Sampler`, `BaseProcessor<T>`, `BaseExporter<T>` | **OpenTelemetry** |

> **Common misconception:** "OpenTelemetry's `Tracer` and `TelemetrySpan` classes are what you're supposed to use in
> .NET." They exist (`src/OpenTelemetry.Api/Trace/Tracer.cs`, `TelemetrySpan.cs`), but they're a thin **shim** over
> `Activity` for people porting code from other languages. The API README says the SDK "will be operating entirely
> with `Activity` only". Use `ActivitySource`/`Activity`.

### 5.3 Where the code comes from (the C6 layer, made explicit)

You've flagged "package vs. assembly vs. namespace vs. what's in the runtime" as fuzzy. Here's the OTel version, the
same four-layer question you met in the ASP.NET Core audio lecture:

```
 ┌──────────────────────────── your app's process ─────────────────────────────┐
 │                                                                             │
 │  FROM THE .NET RUNTIME (shared framework Microsoft.NETCore.App, no NuGet)   │
 │    System.Diagnostics.DiagnosticSource.dll                                  │
 │       namespace System.Diagnostics          → Activity, ActivitySource,     │
 │                                               ActivityListener,             │
 │                                               DistributedContextPropagator  │
 │       namespace System.Diagnostics.Metrics  → Meter, Counter<T>, Histogram, │
 │                                               MeterListener                 │
 │                                                                             │
 │  FROM THE ASP.NET CORE SHARED FRAMEWORK (Microsoft.AspNetCore.App)          │
 │    Microsoft.AspNetCore.Hosting.dll → creates the "Microsoft.AspNetCore"    │
 │                                       server Activity for every request     │
 │                                                                             │
 │  FROM NuGet (downloaded at build, copied next to your app)                  │
 │    OpenTelemetry.Api.dll                       → Baggage, propagators       │
 │    OpenTelemetry.dll   (the SDK)               → providers, samplers,       │
 │                                                  processors, Sdk class      │
 │    OpenTelemetry.Extensions.Hosting.dll        → services.AddOpenTelemetry()│
 │    OpenTelemetry.Exporter.OpenTelemetryProtocol.dll → UseOtlpExporter()     │
 │    OpenTelemetry.Instrumentation.AspNetCore.dll (contrib) → AddAspNetCore...│
 └─────────────────────────────────────────────────────────────────────────────┘
```

Three things worth making sharp:

1. **`using System.Diagnostics;` grants nothing.** It only lets you write `Activity` instead of
   `System.Diagnostics.Activity`. The type is reachable because your project targets .NET, whose shared framework
   includes that assembly. (On old targets like `net462` or `netstandard2.0`, the same assembly arrives as the
   `System.Diagnostics.DiagnosticSource` NuGet package instead. That's why OTel's `Directory.Packages.props` lists it.)
2. **API package vs. SDK package is the "abstractions split" again.** A library author references only
   `OpenTelemetry.Api` (or nothing at all, if `ActivitySource` is enough). Only the *application* references the SDK
   and exporters. **Why:** a library can't know which exporter, sampler, or vendor the app will choose, and must cost
   nothing when telemetry is off. The repo's `AGENTS.md` describes it as a strict three-layer design: no-op API
   layer → SDK → exporters.
3. **Version compatibility rule** (`VERSIONING.md`): an app/library built against `OpenTelemetry.Api` 1.x works with
   any SDK version with the same major and an equal or greater minor. "Core components" (API, SDK, OTLP, Console,
   InMemory, Zipkin (deprecated), Extensions.Hosting) are always released together with the same version number,
   even if one of them didn't change.

---

## 6. The cast of characters

**Rule: every box in the §0 picture is a real type with a job. Learn the job first; the class name is a label.**

| Character | Real type | Where | Job, in one line |
|---|---|---|---|
| **The Reporter** | `ActivitySource` | runtime | A named source of spans ("Demo.Shop", "Microsoft.AspNetCore"). Asks "is anyone listening?" before creating anything |
| **The Timecard** | `Activity` | runtime | One span: name, IDs, start/stop time, tags, events, status |
| **The Case File** | (no type; a trace is just "all Activities with the same `TraceId`") | concept | The whole story of one request, across services |
| **The Clipboard** | `Activity.Current` (backed by `AsyncLocal<Activity>`) | runtime | Holds "the Timecard currently in progress" for *this* async flow, so new Timecards know their parent |
| **The Bureau Chief** | `TracerProviderSdk` (built by `Sdk.CreateTracerProviderBuilder()` or `WithTracing`) | `src/OpenTelemetry/Trace/TracerProviderSdk.cs` | Owns the listener, the Selector, the processors. Decides which Reporters to listen to |
| **The Bureau Chief's phone line** | `ActivityListener` | runtime type, created in `TracerProviderSdk` | The callbacks the runtime calls: `ShouldListenTo`, `Sample`, `ActivityStarted`, `ActivityStopped` |
| **The Selector** | `Sampler` (default `ParentBasedSampler(AlwaysOnSampler)`) | `src/OpenTelemetry/Trace/Sampler/` | Decides per span: record and export / record only / drop |
| **The Desk Clerk** | `BaseProcessor<Activity>` | `src/OpenTelemetry/BaseProcessor.cs` | Gets `OnStart` and `OnEnd` for every recorded span. Can enrich, filter, or hand off |
| **The Mailroom** | `BatchActivityExportProcessor` (a `BatchExportProcessor<Activity>`) | `src/OpenTelemetry/BatchExportProcessor.cs` | A Desk Clerk whose `OnEnd` drops the Timecard into the Out-Tray |
| **The Out-Tray** | `CircularBuffer<T>` | `src/OpenTelemetry/Internal/CircularBuffer.cs` | Bounded, lock-free ring buffer. Default 2048 slots |
| **The Mail Carrier** | `BatchExportThreadWorker<T>` (background thread) | `src/OpenTelemetry/Internal/` | Sleeps until 512 cards are waiting or 5 s pass, then empties the tray into the Packer |
| **The Packer** | `BaseExporter<T>`, e.g. `OtlpTraceExporter`, `ConsoleActivityExporter` | `src/OpenTelemetry.Exporter.*` | Turns a batch of Timecards into bytes and sends them somewhere |
| **The Return Address** | `Resource` | `src/OpenTelemetry/Resources/` | "Who sent this": `service.name` etc. Attached to every shipment |
| **The Stamper** | `TextMapPropagator` (`TraceContextPropagator`, `BaggagePropagator`); in the runtime, `DistributedContextPropagator` | `src/OpenTelemetry.Api/Context/Propagation/` | Writes the Case File number onto outgoing requests (**inject**) and reads it off incoming ones (**extract**) |
| **The Stamp** | the `traceparent` header (+ `tracestate`) | W3C Trace Context standard | `00-<trace id>-<parent span id>-<flags>` |
| **The Sticky Note** | `Baggage` | `src/OpenTelemetry.Api/Baggage.cs` | Key/values that travel with the request (`baggage` header) |
| **The Annotator** | instrumentation libraries, e.g. `HttpInListener` | contrib `src/OpenTelemetry.Instrumentation.AspNetCore/Implementation/HttpInListener.cs` | Adds the standard (semantic-convention) tags to a Timecard that a framework created |
| **The Meter Panel** | `Meter` + instruments (`Counter<T>`, `Histogram<T>`...) | runtime | Where measurements are reported |
| **The Ledger** | `AggregatorStore` + `MetricPoint` | `src/OpenTelemetry/Metrics/` | One running total / histogram per unique attribute combination |
| **The Auditor** | `PeriodicExportingMetricReader` | `src/OpenTelemetry/Metrics/Reader/` | Every 60 s (default), snapshots the Ledger and hands it to a Packer |
| **The Scribe** | `ILogger` | Microsoft.Extensions.Logging | Your logging API, unchanged |
| **The Scribe's Copier** | `OpenTelemetryLoggerProvider` → `LogRecord` | `src/OpenTelemetry/Logs/` | Copies each log call into a `LogRecord`, stamped with the current trace/span ID |
| **The Sorting Office** | the OpenTelemetry **Collector** (separate Go program) | `opentelemetry-collector` repo | Receives, filters, samples, batches and forwards telemetry for many services |
| **The Archive** | a backend: Jaeger, Prometheus, Grafana Tempo/Loki, Azure Monitor, Aspire dashboard | not OTel | Stores and displays |
| **The Dictionary** | semantic conventions | `semantic-conventions` repo; mirrored in `src/Shared/SemanticConventions.cs` | The agreed names: `http.route`, `error.type`... |

How they connect:

```
                         configured at startup                          per span, at run time
  ┌──────────────────────────────────────────────────┐
  │ BUREAU CHIEF (TracerProviderSdk)                 │      REPORTER ──► phone line ──► SELECTOR
  │   ├─ phone line (ActivityListener) registered    │         │            (Sample callback)
  │   │   with the runtime's static list             │         ▼ (if recorded)
  │   ├─ SELECTOR (Sampler)                          │      TIMECARD on the CLIPBOARD
  │   ├─ DESK CLERKS (processors, in order)          │         │ Stop()
  │   │    └─ MAILROOM ─ OUT-TRAY ─ MAIL CARRIER ─┐  │         ▼
  │   │                                           ▼  │      phone line: ActivityStopped
  │   └─ RETURN ADDRESS (Resource)          PACKER   │         ▼
  └──────────────────────────────────────────────────┘      DESK CLERKS.OnEnd ──► MAILROOM ... ──► PACKER ──► wire
```

---

## 7. Startup: what `AddOpenTelemetry()` actually does

**Rule: setup only records *intentions* in the DI container. The Bureau Chief is constructed (and starts listening)
when the host starts, because a tiny hosted service asks for it.**

### 7.1 The code you'll write in a real app

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()                           // OpenTelemetry.Extensions.Hosting
    .ConfigureResource(r => r.AddService("orders"))           // Return Address: service.name=orders
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()                       // contrib: listen to "Microsoft.AspNetCore" + Annotator
        .AddHttpClientInstrumentation()                       // contrib: outgoing HTTP spans
        .AddSource("Orders.Domain"))                          // your own Reporter(s)
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()                       // subscribes to ASP.NET Core's built-in meters
        .AddMeter("Orders.Domain"))
    .WithLogging()                                            // ILogger → OTel LogRecords
    .UseOtlpExporter();                                       // one OTLP Packer for all three signals

var app = builder.Build();
// ... middleware, endpoints ...
app.Run();
```

(Since 1.19.0 there is also `builder.AddOpenTelemetry()` directly on the host builder, which additionally seeds
`service.name` from the app name and `deployment.environment.name` from the environment name, per
`RELEASENOTES.md`.)

### 7.2 Anchored to builder → app → run (and what's different)

| Phase | What ASP.NET Core does (you know this) | What OpenTelemetry does in that phase |
|---|---|---|
| **builder** (`CreateBuilder`, `Services.Add...`) | Collects service registrations; builds nothing | `AddOpenTelemetry()` and every `With...`/`Add...` call **register** configuration callbacks and singletons. No provider exists yet. No listener exists yet |
| **build** (`builder.Build()`) | Builds the `IServiceProvider` (the Supply Room) | Still nothing constructed: singletons are created lazily, on first request for them |
| **run** (`app.Run()` → host `StartAsync`) | Starts hosted services, then Kestrel | `TelemetryHostedService` (registered by `AddOpenTelemetry`) runs first and asks DI for the providers. **This** constructs the Bureau Chief, which builds its sampler/processors/exporter and calls `ActivitySource.AddActivityListener(...)`. From this moment, Reporters have a listener |
| **shutdown** | Host stops; DI container disposed | Disposing the container disposes the providers → `Shutdown()` → Mailroom flushes the Out-Tray → Packer sends the last batch |

The hosted service is tiny; this is the whole thing
(`src/OpenTelemetry.Extensions.Hosting/Implementation/TelemetryHostedService.cs`):

```csharp
internal sealed class TelemetryHostedService(ITelemetryHostInitializer initializer) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // The sole purpose of this HostedService is to ensure all
        // instrumentations, exporters, etc., are created and started.
        initializer.Initialize();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
```

**What's different from the middleware model:** OpenTelemetry is **not middleware**. It doesn't sit in the request
pipeline and nothing calls it per request. It's a **subscriber**: it registers callbacks with the runtime's static
`ActivitySource` and `MeterListener` machinery, and the runtime calls those callbacks whenever *any* code (ASP.NET
Core, HttpClient, your code) starts or stops an Activity from a source it subscribed to. If you've read the events
lecture: the Bureau Chief is the subscriber; `ActivitySource` is the publisher; the "event" is a span starting or
stopping.

> **What this does NOT do (C7 check):** `AddAspNetCoreInstrumentation()` does not create spans for your business
> logic, your database calls (that's `SqlClient`/`EntityFrameworkCore` instrumentation), or your background
> services. It listens to the Activity that ASP.NET Core *itself* creates for each request, and decorates it. Every
> other span exists only because some library or your code called `StartActivity`, **and** you added that source.

### 7.3 Configuration without code

Almost every option can also be set by environment variable, defined by the spec, e.g. `OTEL_SERVICE_NAME`,
`OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL`, `OTEL_TRACES_SAMPLER`, `OTEL_BSP_MAX_QUEUE_SIZE`. In
.NET they're read through `IConfiguration`, so they can also come from `appsettings.json`. A newer, experimental
package, `OpenTelemetry.Configuration.Declarative`, implements the spec's YAML "declarative configuration"
(`OTEL_CONFIG_FILE=otel-config.yaml`); the repo's most recent commits are on it.

---

## 8. Journey 1: one span's life inside one process

**Rule: a span is created only if (1) a listener subscribed to its source and (2) the sampler says record. It's
exported only after it stops, and only by a background thread.**

### 8.1 The code

```csharp
private static readonly ActivitySource Source = new("Orders.Domain", "1.0.0");   // one per library, static

public async Task<Order> PlaceOrderAsync(Cart cart)
{
    using var activity = Source.StartActivity("PlaceOrder");   // ActivityKind.Internal by default
    activity?.SetTag("order.item_count", cart.Items.Count);
    var order = await _repo.SaveAsync(cart);                  // may create child spans (DB instrumentation)
    activity?.SetTag("order.id", order.Id);
    return order;
}                                                              // end of scope: activity.Dispose() → Stop()
```

**Two compact pieces of syntax, expanded (F9):**

- `activity?.SetTag("order.id", order.Id);` means
  ```csharp
  if (activity is not null)
  {
      activity.SetTag("order.id", order.Id);
  }
  // else: do nothing at all, and don't even evaluate the arguments
  ```
  C has no equivalent operator; you'd write the `if` by hand.
- `using var activity = ...;` means "call `activity.Dispose()` when the enclosing block ends, on every exit path,
  including exceptions". Written out:
  ```csharp
  var activity = Source.StartActivity("PlaceOrder");
  try
  {
      // ... rest of the method ...
  }
  finally
  {
      if (activity is not null) { activity.Dispose(); }   // Dispose() stops the Activity
  }
  ```
  The C comparison is `goto cleanup;` on every error path; the compiler writes the cleanup for you. **Where it
  differs:** `using` is not "free memory". `Activity.Dispose()` *stops the span* (records end time, fires
  `ActivityStopped`). The garbage collector still owns the memory.

### 8.2 Step by step, with the state after every step (F8)

| # | Who acts | What happens | `activity` | `Activity.Current` | Out-Tray |
|---|---|---|---|---|---|
| 0 | (startup, once) | Bureau Chief registered its phone line; `ShouldListenTo` says yes for `"Orders.Domain"` (it was added with `AddSource`) | — | the ASP.NET Core server span `POST /orders` (call it **S**) | n items |
| 1 | Reporter | `StartActivity("PlaceOrder")`: is any listener interested in this source? Yes | — | S | n |
| 2 | Selector (via phone line `Sample`) | Gets the would-be parent (S's context), trace ID, name, kind, tags. Default sampler is **ParentBased(AlwaysOn)**: S is sampled → `RecordAndSample` | — | S | n |
| 3 | Reporter | Creates the Activity: **same trace ID as S**, new span ID, `ParentSpanId = S.SpanId`, `Recorded` flag set. Starts it | **P** (not null) | **P** (Start sets Current) | n |
| 4 | phone line `ActivityStarted` | `if (activity.IsAllDataRequested)` → `processor.OnStart(P)` (Desk Clerks see it start) | P | P | n |
| 5 | your code | `SetTag(...)`, `await` the repository. Child spans (DB) see P on the Clipboard and become P's children | P | P (flows across the `await`, §10) | n |
| 6 | `using` ends | `P.Dispose()` → `Stop()`: end time recorded; `Activity.Current` restored to **S** | P (stopped) | **S** | n |
| 7 | phone line `ActivityStopped` | `processor.OnEnd(P)` → Mailroom: `circularBuffer.TryAdd(P)` | P | S | **n + 1** |
| 8 | Mailroom | If the count just reached **512** (`MaxExportBatchSize`), wake the Mail Carrier now; otherwise it wakes on its 5 s timer | P | S | n + 1 |
| 9 | Mail Carrier (background thread `OpenTelemetry-BatchExportProcessor-OtlpTraceExporter`) | Takes up to 512 items as a `Batch<Activity>`, calls `exporter.Export(batch)` | — | (different thread) | n + 1 − batch |
| 10 | Packer (`OtlpTraceExporter`) | Serializes to OTLP protobuf (hand-written serializer in `Implementation/Serializer/`), sends over gRPC to `http://localhost:4317` by default on .NET (HTTP/protobuf to `:4318` on .NET Framework/netstandard) | — | — | — |

The phone-line wiring for steps 4 and 7 is this, from `TracerProviderSdk.cs` (simplified; the real code also handles
"legacy" Activities and suppression):

```csharp
var activityListener = new ActivityListener();
activityListener.ActivityStarted = activity =>
{
    if (activity.IsAllDataRequested && SuppressInstrumentationScope.IncrementIfTriggered() == 0)
    {
        this.Processor?.OnStart(activity);
    }
};
activityListener.ActivityStopped = activity =>
{
    if (!activity.IsAllDataRequested) { return; }
    if (SuppressInstrumentationScope.DecrementIfTriggered() == 0)
    {
        this.Processor?.OnEnd(activity);
    }
};
activityListener.Sample = (ref options) =>
    !Sdk.SuppressInstrumentation ? ComputeActivitySamplingResult(ref options, this.Sampler) : ActivitySamplingResult.None;
activityListener.ShouldListenTo = filter;            // built from your AddSource(...) names, wildcards allowed
ActivitySource.AddActivityListener(activityListener); // register with the runtime's static list
```

Notice the **lambdas** (`activity => { ... }`): each one is a delegate stored in a property of the listener object.
That's the same "store a callback, someone else calls it later" move as events and `RequestDelegate`, but here the
storage is a plain property, not an `event`, and **the runtime** is the caller.

### 8.3 Why "suppression" exists (a small but real gotcha)

The OTLP exporter sends data with `HttpClient`. `HttpClient` is instrumented. So exporting a span would create a new
span, which gets exported, which creates a span... `SuppressInstrumentationScope` is a flag (on the Clipboard, so per
async flow) that exporters set while they work, and that the listener checks (`Sdk.SuppressInstrumentation`). It's a
guard against telemetry about telemetry.

### 8.4 What this machinery does NOT do (F7)

- It doesn't measure anything you didn't wrap. A span's duration is exactly start → stop.
- It doesn't make `StartActivity` cheap *when listened to*. Creating, tagging and exporting a span costs allocations
  and CPU; that's why sampling exists (§13).
- It doesn't send anything synchronously. Your request never waits on the network for telemetry.
- It doesn't retry forever or buffer to disk by default. If the Collector is down, batches fail; the OTLP exporter has
  a retry policy for transient errors, and optional persistent storage, but the default path ultimately drops data.

### 8.5 Edge: the queue is full (backpressure)

If spans end faster than the Mail Carrier can ship them (the Collector is slow, the network is down), the Out-Tray
fills. Then:

```csharp
// BatchExportProcessor<T>.TryExport (actual code, trimmed)
if (this.circularBuffer.TryAdd(data, maxSpinCount: 50_000, out var count))
{
    if (count == this.MaxExportBatchSize) { this.worker.TriggerExport(); }
    return true;                      // enqueued
}
// either the queue is full or exceeded the spin limit, drop the item on the floor
this.worker.IncrementDroppedCount();
this.OnItemDropped();
return false;
```

The design choice is **drop the newest, never block the caller**. A request thread must never wait because the
telemetry backend is slow; losing some telemetry is the lesser evil. That's the same three-way choice as contrib
#4473 (block / drop new / evict oldest), and the reason #4473 exists is that the InfluxDB exporter had *no* bound at
all. A firmware comparison genuinely fits here: it's a full ring buffer in an ISR-fed UART driver, and the ISR can't
wait. **Where it stops fitting:** `CircularBuffer` is written for *many* concurrent producers (any thread pool thread
can end a span), so `TryAdd` uses `Interlocked.CompareExchange` and spinning instead of the single-producer/single-
consumer index trick you'd use in firmware.

### 8.6 Try it (part 2 of the skeleton, unverified)

Add `.SetSampler(new AlwaysOffSampler())` to the skeleton in §0.5 and print `order is null` and `pay is null`.
Predicted: nothing exported; **`order` is not null but `pay` is null**. Why, from `PropagateOrIgnoreData` in
`TracerProviderSdk.cs`:

```csharp
var isRootSpan = options.Parent.TraceId == default;
// root span, or parent came from another process → keep a context-only Activity so the trace ID survives
return (isRootSpan || options.Parent.IsRemote)
    ? ActivitySamplingResult.PropagationData   // object exists, records nothing (IsAllDataRequested == false)
    : ActivitySamplingResult.None;             // no object at all → StartActivity returns null
```

`PlaceOrder` is a root → a context-only object. `ChargeCard`'s parent is local and unsampled → `null`. Compare with
B1, where `order` itself was null. The lesson (§13.3): **no listener → no object; listener says drop → either a
context-only object or nothing, depending on the parent.**

---

## 9. Journey 2: one trace across two processes (propagation)

**Rule: services share nothing but the bytes on the wire. A trace spans two services only because the caller writes
the current span's context into a header and the callee reads it back and uses it as the parent.**

### 9.1 The Stamp: `traceparent`, character by character

The W3C Trace Context standard defines one header:

```
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
             ││ └───────────── trace ID ───────────┘ └─ parent ID ──┘ └┴─ flags: 01 = sampled
             └┴─ version (always 00 today)                (the CALLER's span ID)
```

Plus an optional `tracestate` header for vendor-specific data, and a separate `baggage` header (§10.4).

The second field is the **caller's** span ID, i.e. the ID of the client span that made the call. The callee creates
its server span with that as its parent. That's how the waterfall in §4.1 gets its `CLIENT → SERVER` pairs.

### 9.2 Inject and extract, step by step

```
 SERVICE A (gateway)                                      SERVICE B (orders)
 ─────────────────                                        ─────────────────
 Clipboard: span G (server)
 1. HttpClient.SendAsync(...)
 2. HttpClient creates CLIENT span C (source
    "System.Net.Http"), parent G, same trace ID
 3. STAMPER (inject): writes
    traceparent: 00-<trace>-<C.SpanId>-01   ──── HTTP request ────►
                                                          4. Kestrel parses the request
                                                          5. ASP.NET Core Hosting: STAMPER (extract)
                                                             reads traceparent → ActivityContext
                                                             { trace, parent = C.SpanId, remote = true }
                                                          6. Starts SERVER span S from source
                                                             "Microsoft.AspNetCore", parent = that context
                                                          7. ANNOTATOR (OTel's HttpInListener) adds
                                                             semconv tags to S; at the end sets the
                                                             name to "{method} {route}"
                                                          8. S stops → exported by B's Mailroom
 9. C stops → exported by A's Mailroom   ◄──── response ───
```

Both services export **separately**, to wherever they're configured. The backend stitches the Case File together by
trace ID and parent IDs, sometimes seconds apart. **No service ever sees the whole trace.**

### 9.3 Who is the Stamper in .NET? (two of them, and why)

There are two propagator families, and they're easy to confuse:

| | Runtime's Stamper | OpenTelemetry's Stamper |
|---|---|---|
| Type | `System.Diagnostics.DistributedContextPropagator` | `OpenTelemetry.Context.Propagation.TextMapPropagator` |
| Used by | `HttpClient` and ASP.NET Core **themselves**, even with no OTel installed | OTel instrumentation libraries, and your own code for non-HTTP carriers |
| Default | W3C `traceparent`/`tracestate` | `Propagators.DefaultTextMapPropagator`; once the SDK is set up, a **composite** of W3C Trace Context + W3C Baggage |

The contrib instrumentations bridge the two. In `HttpInListener.OnStartActivity` (ASP.NET Core instrumentation), if
the configured OTel propagator is anything other than plain `TraceContextPropagator`, the Annotator **extracts again**
with the OTel propagator; if that produces a *different* parent than the one ASP.NET Core used (say you configured
B3 headers, a Zipkin format), it creates a **sibling** Activity with the right parent, marks the framework's Activity
as not-recorded (`IsAllDataRequested = false`), and exports the sibling instead. It also sets `Baggage.Current` from
the extracted baggage. That's a good example of "instrumentation" meaning *repairing and decorating* what a framework
already does, not doing it from scratch.

### 9.4 Carriers that aren't HTTP

The same inject/extract works for any "carrier" with string key/values: Kafka message headers, RabbitMQ properties,
the `params._meta` field of an MCP JSON-RPC message (scouted item O), even environment variables
(`EnvironmentVariableCarrier.cs` exists in the API). You pass the propagator a *setter* (how to write one key) and a
*getter* (how to read one key) for that carrier:

```csharp
// Producer side (e.g. before publishing a Kafka message)
Propagators.DefaultTextMapPropagator.Inject(
    new PropagationContext(Activity.Current?.Context ?? default, Baggage.Current),
    message.Headers,
    (headers, key, value) => headers.Add(key, Encoding.UTF8.GetBytes(value)));   // the setter
```

The consumer side is the mirror: `Extract(default, message.Headers, getter)`, then
`source.StartActivity("process", ActivityKind.Consumer, parentContext.ActivityContext)`.

### 9.5 Try it: break the trace on purpose (unverified)

Two minimal ASP.NET Core apps, A calling B over `HttpClient`, both with `AddAspNetCoreInstrumentation()`,
`AddHttpClientInstrumentation()` and the console exporter (or both sending OTLP to an Aspire dashboard container).

1. Call A. Predicted: B's server span has the **same trace ID** as A's spans and its parent is A's client span.
2. In A, remove the header before sending: a `DelegatingHandler` that does
   `request.Headers.Remove("traceparent");`. Predicted: B starts a **new trace** (new trace ID, no parent). Two
   unrelated Case Files. This is the single most common real-world tracing bug: some hop (a proxy, a queue, a custom
   protocol) doesn't forward the header.

### 9.6 Common misconception

"The Collector (or the backend) connects the spans." Actually, the connection is decided **at run time, inside the
services**, by the parent IDs they put in each span. The backend only groups spans that already share a trace ID. If
the header didn't make it, no backend can repair it later.

---

## 10. Inside one process: the Clipboard, `AsyncLocal`, and the Baggage bug (#7449)

**Rule: "current" (`Activity.Current`, `Baggage.Current`) means "current for this async flow", implemented with
`AsyncLocal<T>`. Values flow *down* into awaited and spawned work; changes made in a child flow do not flow back up.**

### 10.1 The problem the Clipboard solves

In §8, the DB span became a child of `PlaceOrder` without anyone passing `PlaceOrder` to the repository. How? The
repository's instrumentation read `Activity.Current`. But "current" can't be a global variable: a web server runs
hundreds of requests at once, on a shared pool of threads, and one request's `await` may resume on a different thread
than it started on. And it can't be thread-local either: after an `await`, you might be on another thread.

### 10.2 `AsyncLocal<T>`: the right kind of "current"

`AsyncLocal<T>` stores a value in the **execution context**, a small object the runtime carries along the logical
flow of an async operation: it's captured when you `await` (or start a `Task`) and restored when the continuation
runs, on whatever thread.

```
  request 1 flow:   [thread 7] Current = S1 ──await──► [thread 12] Current = S1 ──► ...
  request 2 flow:   [thread 9] Current = S2 ──await──► [thread 7]  Current = S2 ──► ...
                     thread 7 serves both requests at different times; each flow sees its own value
```

Copy-on-write semantics: the execution context is effectively **immutable**. Setting an `AsyncLocal` creates a new
context for *this* flow onward. A child task gets a copy of the parent's context at the moment it starts; if the
child then sets the value, only the child's copy changes.

> **Firmware comparison, bounded:** it's like giving each RTOS task its own copy of a "current transaction" variable
> in its task control block. Where it stops: async flows aren't threads. One thread runs many flows over time, and
> one flow hops across threads; the "control block" travels with the *flow*, not with a thread.

### 10.3 The two rules that follow

| Situation | Result |
|---|---|
| Parent sets `X`, then starts/awaits a child | Child **sees** `X` |
| Child sets `X = new value` | Parent **does not** see it, after the child finishes |

That second rule is why `Activity.Current` "restores itself" correctly: when a child span stops, the parent's flow
never saw it change.

### 10.4 The Baggage bug: storing a *mutable box* in an `AsyncLocal`

`Baggage` is a struct (an immutable value). But `Baggage.Current` doesn't store it directly. From
`src/OpenTelemetry.Api/Baggage.cs` at `f9dd754`:

```csharp
private static readonly RuntimeContextSlot<BaggageHolder> RuntimeContextSlot =
    RuntimeContext.RegisterSlot<BaggageHolder>("otel.baggage");      // AsyncLocal-backed by default

public static Baggage Current
{
    get => RuntimeContextSlot.Get()?.Baggage ?? default;
    set => EnsureBaggageHolder().Baggage = value;                    // mutates the SHARED holder object
}

private sealed class BaggageHolder { public Baggage Baggage; }      // (shape simplified)
```

The `AsyncLocal` holds a **reference** to a `BaggageHolder` object. Copying the execution context copies the
*reference*, so parent and child point at **the same holder**. The setter doesn't replace the reference (which would
be private to the child); it **writes into the shared object**. State after each step (F8):

| # | Step | Parent flow's slot → holder | Child flow's slot → holder | `holder.Baggage` | Parent reads `Baggage.Current` |
|---|---|---|---|---|---|
| 1 | Parent: `Baggage.Current = {tenant=A}` → creates holder **H** | H | — | {tenant=A} | {tenant=A} |
| 2 | Parent starts child task (context copied) | H | **H** (same object) | {tenant=A} | {tenant=A} |
| 3 | Child: `Baggage.Current = {tenant=B}` → `EnsureBaggageHolder()` finds H, writes into it | H | H | **{tenant=B}** | **{tenant=B}** ⚠ leaked |
| 4 | Child finishes | H | — | {tenant=B} | **{tenant=B}** ⚠ |

With a plain `AsyncLocal<Baggage>` (the value itself in the slot), step 3 would create a new context for the child
only, and the parent would still read `{tenant=A}`. **The leak is caused by the box, not by `AsyncLocal`.**

Why not just fix it? Because the "leak" is also a *feature* someone relies on: code that sets baggage inside a
helper method (a child flow) and expects the caller to see it afterwards works *because of* the shared box. Changing it
changes observable behavior, which is why #7449 carries `needs-majorversion-bump`, an earlier fix (PR #5208) was
reverted (#5227), and a later one (#7191) was abandoned (status as of the 2026-09-26 scouting pass; the holder code is
unchanged at `f9dd754`). That's scouted item N, and the §21 experiment reproduces it in 30 minutes.

### 10.5 Gotcha: two different "baggage"s

`System.Diagnostics.Activity` has its own `Activity.Baggage` / `Activity.AddBaggage(...)`, stored **on the Activity**.
`OpenTelemetry.Baggage.Current` is stored **in the runtime context**, independent of any Activity. They are different
mechanisms, and the OTel SDK's propagators use `OpenTelemetry.Baggage`. Mixing them is a classic source of "I set
baggage but the next service didn't get it".

---

## 11. Metrics: from `counter.Add(1)` to a number in Prometheus

**Rule: instruments report raw measurements; the SDK aggregates them in memory into one `MetricPoint` per unique
attribute combination; a reader periodically exports snapshots. Only aggregates ever leave the process.**

### 11.1 The instruments (all from `System.Diagnostics.Metrics`)

| Instrument | You call | Aggregated as | Example |
|---|---|---|---|
| `Counter<T>` | `Add(n)`, only increases | Sum | requests served, bytes sent |
| `UpDownCounter<T>` | `Add(±n)` | Sum (can go down) | items in a queue, active connections |
| `Histogram<T>` | `Record(value)` | Bucket counts + sum + min/max | request duration, payload size |
| `Gauge<T>` (.NET 9+) | `Record(value)` | Last value | current temperature |
| `ObservableCounter/UpDownCounter/Gauge<T>` | *you* supply a callback; the SDK calls it at collection time | Sum / last value | GC heap size, thread pool queue length (contrib item R would be one of these) |

```csharp
private static readonly Meter Meter = new("Orders.Domain", "1.0.0");
private static readonly Counter<long> OrdersPlaced =
    Meter.CreateCounter<long>("orders.placed", unit: "{order}", description: "Orders accepted");

OrdersPlaced.Add(1, new KeyValuePair<string, object?>("payment.method", "card"));
```

### 11.2 The pipeline, with characters

```
 OrdersPlaced.Add(1, payment.method=card)
        │  (runtime) MeterListener callback, set up by MeterProviderSdk
        ▼
 LEDGER (AggregatorStore for "orders.placed")
   tags → index lookup ──► MetricPoint[ "payment.method=card" ]  sum += 1      (lock-free Interlocked ops)
                           MetricPoint[ "payment.method=cash" ]  sum = 41
                           MetricPoint[ overflow ]                ...
        │
        │ every 60 s (PeriodicExportingMetricReader, DefaultExportIntervalMilliseconds = 60000)
        ▼
 AUDITOR snapshots all points ──► PACKER (OTLP metrics exporter) ──► wire
```

The `MeterProviderSdk` (`src/OpenTelemetry/Metrics/MeterProviderSdk.cs`) creates a `MeterListener`, enables the
instruments whose `Meter` name you added with `AddMeter(...)`, and registers `SetMeasurementEventCallback<T>` for
`long`, `int`, `short`, `byte`, `double`, `float` (the smaller integer types are widened to `long`, `float` to
`double`).

### 11.3 Cardinality: the most important word in metrics

**Cardinality** = the number of distinct attribute combinations for one metric. Every combination is a separate
`MetricPoint` in memory and a separate time series in the backend.

| Attributes on `http.server.request.duration` | Series |
|---|---|
| method (5) × route template (20) × status (8) | ~800: fine |
| method × **raw path** `/orders/1`, `/orders/2`, ... | **unbounded**: memory grows forever, backend bill explodes |
| × user ID | unbounded *and* a privacy leak |

So the SDK enforces a **cardinality limit per metric: 2000 by default**
(`MeterProviderBuilderSdk.DefaultCardinalityLimit = 2000`). Measurements for new combinations beyond the limit are
folded into one reserved **overflow** point (attribute `otel.metric.overflow = true`). There's also a limit of 1000
metrics per provider (`DefaultMetricLimit`). Change limits per metric with a **View**:
`AddView("orders.placed", new MetricStreamConfiguration { CardinalityLimit = 500 })`.

This is exactly the argument in the YARP #2667 discussion (scouted item L): using the raw request path as a span name
makes span names high-cardinality, which is why the HTTP conventions say to use the route template.

### 11.4 Temporality: cumulative vs. delta

- **Cumulative**: each export says "total since the process started" (sum = 1,204, then 1,310, then 1,377...).
  Prometheus expects this. Losing one export loses nothing.
- **Delta**: each export says "since the last export" (106, then 67...). Some backends prefer it; memory can be
  reclaimed between exports.

Set with `MetricReaderTemporalityPreference`. Default is cumulative.

### 11.5 Views

A View (`AddView`) lets the *app* change how an instrument a *library* defined is aggregated: rename it, drop it,
keep only some attributes, change histogram bucket boundaries, or change the cardinality limit. The library author
chooses what to measure; the app operator chooses how much of it to keep. That split of authority (author vs.
operator) is a recurring OTel design idea.

### 11.6 Exemplars

An **exemplar** is a sample measurement saved alongside a metric point *with the trace ID and span ID that were
current when it was recorded*. It's the link from "p99 latency spiked" to "here's one actual slow trace". Configured
with `SetExemplarFilter(...)`; code in `src/OpenTelemetry/Metrics/Exemplar/`.

### 11.7 Built-in metrics you get for free

.NET 8+ ASP.NET Core and the runtime publish their own meters (`Microsoft.AspNetCore.Hosting`,
`Microsoft.AspNetCore.Server.Kestrel`, `Microsoft.AspNetCore.Routing`, ... `System.Runtime` in .NET 9+). On modern
.NET, contrib's `AddAspNetCoreInstrumentation()` for metrics mostly just calls `AddMeter(...)` on that list of 13
built-in ASP.NET Core meter names (`AspNetCoreInstrumentationMeterProviderBuilderExtensions.cs`, `ConfigureMeters`).
The trend across the whole project: **instrumentation is moving into the libraries themselves** ("native
instrumentation"), and the contrib packages shrink to "subscribe to the right names, fill gaps for older runtimes".

---

## 12. Logs: `ILogger` with a trace ID attached

**Rule: OpenTelemetry is one more `ILoggerProvider`. It copies each log call into a pooled `LogRecord`, stamps it
with the current `TraceId`/`SpanId`, and sends it through processors and an exporter, just like spans.**

```
 _logger.LogWarning("Card declined for order {OrderId}", id)
      │ Microsoft.Extensions.Logging fans out to every registered provider:
      ├──► ConsoleLoggerProvider            (prints, as before)
      └──► OpenTelemetryLoggerProvider      ── SCRIBE'S COPIER
              │ OpenTelemetryLogger.Log(...): takes a LogRecord from a POOL, fills
              │   timestamp, severity, category, body template "Card declined for order {OrderId}",
              │   attributes { OrderId = 42 }, TraceId/SpanId from Activity.Current
              ▼
           BatchLogRecordExportProcessor ──► OTLP logs exporter ──► wire
```

Two things to notice:

- **The message template is kept separately from the values** (`{OrderId}` and `OrderId=42`), so a backend can group
  all "Card declined" logs regardless of the ID. That's structured logging, and it's the logs version of the
  cardinality idea.
- **`LogRecord`s are pooled and reused** (`src/OpenTelemetry/Logs/Pool/`). An exporter must finish with a record
  inside `Export` and never keep a reference to it afterwards: the object will be refilled with a different log.
  (C analogy, bounded: a buffer returned to a free list. The difference is that .NET won't crash; you'll just
  silently read the wrong log's data.)

---

## 13. Sampling: deciding what to keep

**Rule: head sampling decides at span start, inside the app, from little information; tail sampling decides after the
whole trace is complete, in the Collector, from everything. The default .NET sampler keeps everything, but follows
the parent's decision.**

### 13.1 Why sample

At 10,000 requests/s with 20 spans each, you'd export 200,000 spans/s. Most are boring successes. Sampling keeps a
representative fraction (and ideally all the interesting ones).

### 13.2 The built-in samplers

| Sampler | Decision |
|---|---|
| `AlwaysOnSampler` | Record and export everything |
| `AlwaysOffSampler` | Drop everything (context still propagates for roots/remote parents, §8.6) |
| `TraceIdRatioBasedSampler(0.1)` | Keep 10%, chosen **from the trace ID**, so every service using the same ratio makes the same decision for the same trace |
| `ParentBasedSampler(root)` (**default**, with `AlwaysOn` as the root sampler) | If there's a parent, copy its sampled flag; if not, ask the root sampler |
| `AlwaysRecordSampler` (new in 1.19.0) | Wraps another sampler so spans are always *recorded* (processors see them) even when they won't be exported |

**Why parent-based is the default:** if service A keeps a trace and service B independently drops its half, the trace
has holes. Following the `01`/`00` flag in `traceparent` keeps whole traces or none.

### 13.3 Three different "no"s (F11: compare the guards)

Easy to confuse, so side by side:

| Guard | Where | Asked when | If "no" |
|---|---|---|---|
| `ShouldListenTo(source)` | phone line, built from `AddSource` names | Once per source (cached by the runtime) | No listener for that source → `StartActivity` returns **null**, always |
| `Sample(...)` → `Sampler.ShouldSample` | phone line | Every `StartActivity` | `Drop` → **null** or a **context-only** Activity (§8.6) |
| `activity.IsAllDataRequested` | checked in `ActivityStarted`/`ActivityStopped` | On start and stop | The Activity exists but processors never see it; nothing exported |

### 13.4 Tail sampling

"Keep every trace with an error or slower than 2 s, plus 1% of the rest" needs to see the *whole* trace first. No
single service can. So it's done in the Collector's `tailsamplingprocessor` (contrib Collector), which buffers spans
by trace ID for a few seconds, then decides. The catch: all spans of a trace must reach the **same** Collector
instance, so you need trace-ID-aware load balancing in front of it.

---

## 14. The Collector: the Sorting Office

**Rule: the Collector is a separate program that receives telemetry in many formats, runs it through processors,
and exports it to one or more backends. Apps can skip it, but production systems almost never do.**

### 14.1 Why have one

| Without a Collector | With one |
|---|---|
| Every service needs every backend's credentials and endpoint | Services only know `localhost:4317`; the Collector holds the secrets |
| Changing backends = redeploying every service | Change one Collector config |
| Batching/retry/sampling happen in every process | Centralized; tail sampling becomes possible |
| A slow backend backs up into your app's Out-Tray | The Collector absorbs it (with its own queue) |

### 14.2 Its anatomy

```
  receivers ──► processors ──► exporters          (one PIPELINE per signal: traces, metrics, logs)
  otlp          memory_limiter  otlp (to Tempo)
  prometheus    batch           prometheusremotewrite
  jaeger        attributes      azuremonitor
  kafka         tail_sampling   debug (prints)
                 └── connectors join pipelines (e.g. spanmetrics: turn spans into RED metrics)
```

A Collector config is YAML that names components and wires them into pipelines. It's written in Go, so you'll
*configure and run* it (`otel/opentelemetry-collector-contrib` Docker image), not change it.

### 14.3 Deployment shapes

- **Agent**: one Collector per host (or a Kubernetes sidecar/DaemonSet), close to the apps.
- **Gateway**: a pool of Collectors behind a load balancer that all agents send to, where tail sampling and
  backend export happen.

---

## 15. Semantic conventions: the Dictionary

**Rule: telemetry is only useful across teams and tools if everyone uses the same names for the same things. The
semantic conventions define those names, their types, and which are stable.**

### 15.1 Examples you'll meet constantly

| Attribute / name | Meaning |
|---|---|
| `service.name` (resource) | The logical service |
| `http.request.method` | `GET`, `POST`... |
| `http.route` | The **route template** that matched: `/orders/{id}` |
| `url.path` | The actual path: `/orders/42` |
| `http.response.status_code` | 200, 404... |
| `server.address`, `server.port` | Who was called |
| `error.type` | Error class, e.g. `500` or `System.TimeoutException` |
| `db.system.name`, `db.query.text` | Database calls |
| `http.server.request.duration` (metric, seconds) | Server latency histogram |
| HTTP server **span name** | `{method} {http.route}`, e.g. `GET /orders/{id}` |

In the .NET repos, these strings live in `src/Shared/SemanticConventions.cs`, a file **linked** into each project that
needs it (not a project reference, §16.3).

### 15.2 The span-name rule, seen in real code

At `d3588e5`, contrib's `HttpInListener.OnStopActivity` does:

```csharp
var routePattern = context.GetHttpRoute();
if (!string.IsNullOrEmpty(routePattern))
{
    TelemetryHelper.RequestDataHelper.SetActivityDisplayName(activity, context.Request.Method, routePattern);
    activity.SetTag(SemanticConventions.AttributeHttpRoute, routePattern);
}
```

That block sits inside `if (!Net11OrGreater || !this.nativeAspNetCoreOpenTelemetryEnabled || createdSibling)`:
on ASP.NET Core 11+, the framework sets these tags **itself** and the Annotator steps aside, another instance of the
native-instrumentation trend (§11.7). Either way, the name is set at the **end** of the request, because the route is
only known after routing ran. At start, the name is just the method.

That's the whole YARP #2667 story in four lines: the conventions say span names must be low-cardinality (route
template, not raw path) because names are used for grouping, and paths can carry IDs and personal data.

### 15.3 Stability, and why instrumentation PRs are careful

Conventions move through *development → release candidate → stable*. HTTP conventions became stable in 2023, with
renames (`http.method` → `http.request.method`, `http.status_code` → `http.response.status_code`). Renaming an
attribute breaks every dashboard and alert that used the old name, so the spec defines opt-in migration
(`OTEL_SEMCONV_STABILITY_OPT_IN`), and instrumentation libraries treat a name change as a **breaking change**. When you
review or write an instrumentation change, the first question a maintainer asks is "which convention version, and is
this attribute stable?".

---

## 16. Ownership and disposal: who flushes the Out-Tray

**Rule: the provider owns its processors and exporters. Disposing the provider shuts them down in order, and
shutdown is the only thing that guarantees the last batch gets sent.**

### 16.1 The terms (F10)

- **Owns** = is responsible for cleaning it up. In C: the code that must call `free` (or `close`) on it.
- **Dispose** = .NET's explicit cleanup call (`IDisposable.Dispose()`), for things the garbage collector can't clean
  by itself: threads, sockets, file handles, *pending work*. The GC reclaims memory; it does not flush queues or stop
  threads.
- **Leak** (here) = an owned resource that's never cleaned up. In C, `malloc` without `free` on an error path. In
  this project, more often a **lost flush**: spans sitting in the Out-Tray when the process exits.

### 16.2 The ownership tree

```
 TracerProviderSdk  (owned by: you, if built with Sdk.Create...; the DI container, if built with AddOpenTelemetry)
  └─ owns CompositeProcessor / processors (in the order added)
       └─ BatchActivityExportProcessor  ── owns ──► the Mail Carrier thread + Out-Tray
            └─ owns OtlpTraceExporter   ── owns ──► HttpClient / gRPC channel

 provider.Dispose()
   → processor.Shutdown(timeout)          // stop accepting, wait for in-flight OnEnd calls (activeOnEndCount)
       → worker: export everything left    // the final flush
       → exporter.Shutdown()               // close connections
```

### 16.3 Where it goes wrong

| Situation | What happens | Fix |
|---|---|---|
| Console app / short job builds a provider and never disposes it | Spans in the Out-Tray are lost; with a 5 s delay, a 2 s job may export **nothing** | `using var tracerProvider = ...` (as in §0.5) |
| Hosted app killed with `SIGKILL` / container OOM | No shutdown runs at all; the last batch is lost | Unavoidable; keep batches small/frequent if it matters |
| Serverless function (e.g. AWS Lambda) freezes between invocations | Background thread doesn't run while frozen | Call `ForceFlush()` at the end of each invocation (contrib has `Instrumentation.AWSLambda` for this) |
| You add a custom processor that wraps an exporter | Your `OnShutdown` must call the exporter's `Shutdown` | Derive from `BaseExportProcessor<T>`/`SimpleExportProcessor<T>`/`BatchExportProcessor<T>` instead of writing your own (`AGENTS.md` says the same) |

---

## 17. The repository, folder by folder (and a depth budget)

**Rule: three layers (API → SDK → exporters), one folder per NuGet package under `src/`, one test project per
package under `test/`. Read the six files in §17.3 before anything else, and stop there on the first pass.**

### 17.1 `opentelemetry-dotnet` at `f9dd754`

```
opentelemetry-dotnet/
├── AGENTS.md            ◄ build/test commands + architecture + conventions, written for AI agents. Read first
├── REVIEW.md            ◄ what PR reviewers (human and bot) check: CHANGELOG, public API, packages
├── CONTRIBUTING.md      ◄ CLA, PR rules, "find a buddy", SIG meetings
├── VERSIONING.md        ◄ semver rules, API/SDK compatibility, core components released together
├── RELEASENOTES.md      ◄ highlights per release (1.19.1 is the latest stable)
├── global.json          ◄ .NET SDK 10.0.401
├── Directory.Packages.props ◄ ALL package versions (central package management); OTelLatestStableVer = 1.19.1
├── OpenTelemetry.slnx   ◄ the solution (new XML .slnx format)
├── build/               ◄ shared MSBuild props, rulesets, stylecop, xunit.runner.json (serial tests),
│                          scripts/sanitycheck.py (rejects non-ASCII + trailing whitespace)
├── src/
│   ├── OpenTelemetry.Api/                  API: Baggage, propagators, provider base classes, RuntimeContext
│   ├── OpenTelemetry.Api.ProviderBuilderExtensions/  lets libraries register services on provider builders
│   ├── OpenTelemetry/                      SDK: Sdk.cs, BaseProcessor/BaseExporter/Batch*, Trace/ Metrics/ Logs/
│   │                                       Resources/, Internal/ (CircularBuffer, workers, self-diagnostics)
│   ├── OpenTelemetry.Extensions.Hosting/   AddOpenTelemetry(), OpenTelemetryBuilder, TelemetryHostedService
│   ├── OpenTelemetry.Extensions.Propagators/ B3, Jaeger propagators
│   ├── OpenTelemetry.Exporter.OpenTelemetryProtocol/  OTLP: Builder/, Implementation/ExportClient (gRPC/HTTP),
│   │                                       Implementation/Serializer (hand-written protobuf), Transmission (retry)
│   ├── OpenTelemetry.Exporter.Console/  .InMemory/  .Zipkin/ (deprecated)  .Prometheus.AspNetCore/  .Prometheus.HttpListener/
│   ├── OpenTelemetry.Configuration.Declarative/  experimental YAML configuration (active work)
│   ├── OpenTelemetry.Shims.OpenTracing/    bridge for old OpenTracing code
│   └── Shared/                             source files LINKED into many projects (SemanticConventions.cs, Guard.cs...)
├── test/                ◄ one *.Tests project per package, plus FuzzTests, Stress tests, Benchmarks,
│                          and platform test apps (Android, Apple, Maui, BlazorWasm, AOT)
├── docs/                ◄ trace/ metrics/ logs/: getting-started-console, -aspnetcore, -jaeger,
│                          customizing-the-sdk, extending-the-sdk (how to write an exporter/processor/sampler)
└── examples/            ◄ AspNetCore, Console, GrpcService, MicroserviceExample (WebApi → RabbitMQ → Worker,
                           with a Collector 0.161.0 + Zipkin docker-compose)
```

Every project under `src/` also has:

- `CHANGELOG.md`: every behavioral change adds a line under `## Unreleased`.
- `.publicApi/Stable/PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` (+ `Experimental/`): the full list of public
  signatures. The build **fails** if your public API doesn't match the file (Roslyn's PublicApiAnalyzers). That's the
  same "API baseline" mechanism as ASP.NET Core, and it's how reviewers spot API changes at a glance.

### 17.2 Patterns worth naming (for the pattern catalog)

| Pattern | Where | Why |
|---|---|---|
| **Observer / publish-subscribe** | `ActivityListener`, `MeterListener`, `DiagnosticListener` | Producers don't know consumers; zero cost with no subscriber |
| **Builder** (fluent, deferred) | `TracerProviderBuilder`, `OpenTelemetryBuilder` | Collect intentions first, construct once, in a fixed order |
| **Chain of responsibility / pipeline** | `CompositeProcessor` → `BatchExportProcessor` → exporter | Each stage does one job and passes on |
| **Strategy** | `Sampler`, `TextMapPropagator`, `BaseExporter<T>` | Swap behavior without touching the core |
| **Null object** | No-op API defaults (`NoopTextMapPropagator`, `Propagators.DefaultTextMapPropagator = Noop` until the SDK sets one) | Libraries can always call the API safely |
| **Bounded queue + background worker** | `BatchExportProcessor` | Decouple request latency from network latency; bound memory |
| **Object pool** | `LogRecordSharedPool`, `LogRecordThreadStaticPool` | Avoid an allocation per log line |
| **Linked shared source** | `src/Shared/*.cs` via `<Compile Include=... Link=...>` | Share internal helpers without making them public API |
| **Ambient context** | `Activity.Current`, `Baggage.Current`, `RuntimeContext` | Pass context implicitly through call chains (§10) |

### 17.3 Depth budget: read these, in this order, then stop (C4)

You tend to follow every call into its implementation and lose the map. So, a budget for the first pass:

| # | File | Read for | Stop at |
|---|---|---|---|
| 1 | `AGENTS.md` | The three layers and the conventions | The end of "Key Conventions" |
| 2 | `docs/trace/getting-started-console/` | A running skeleton (official version of §0.5) | Once it runs |
| 3 | `src/OpenTelemetry/Trace/TracerProviderSdk.cs` | The constructor: how the listener is built | The `AddActivityListener` call (~line 280). Don't follow the sampler internals |
| 4 | `src/OpenTelemetry/BatchExportProcessor.cs` | `OnEnd` → `TryExport` → drop logic, the defaults | Don't open `CircularBuffer.cs` yet |
| 5 | `src/OpenTelemetry/BaseExporter.cs` + `src/OpenTelemetry.Exporter.Console/ConsoleActivityExporter.cs` | What an exporter must implement | Don't open OTLP yet |
| 6 | `src/OpenTelemetry.Api/Baggage.cs` | `Current`, `BaggageHolder`, `EnsureBaggageHolder` | That's item N |

Write down every "but how does X work?" that comes up in a **parking list** instead of opening it. Second pass picks
two items from the list.

### 17.4 `opentelemetry-dotnet-contrib` at `d3588e5`

Same conventions, but organized as ~50 independent components, each with its own CHANGELOG, owners and release tags
(e.g. `Instrumentation.AspNetCore-1.x.y`). The ones closest to your work:

| Component | Interesting because |
|---|---|
| `OpenTelemetry.Instrumentation.AspNetCore` | The Annotator (`HttpInListener`), sibling-Activity logic, metrics = `AddMeter` list |
| `OpenTelemetry.Instrumentation.Http` | Client side of §9 |
| `OpenTelemetry.Instrumentation.Runtime` | GC/thread pool metrics, item R (#4516) |
| `OpenTelemetry.Exporter.InfluxDB` | Item Q (#4473): unbounded queue → backpressure |
| `OpenTelemetry.Instrumentation.SqlClient`, `.StackExchangeRedis`, `.ConfluentKafka`, `.EntityFrameworkCore` | Each one is "how library X gets spans" |
| `OpenTelemetry.Resources.*` | Resource detectors: container ID, host, OS, process, cloud |

`.github/component_owners.yml` lists who reviews each one (e.g. `src/OpenTelemetry.Exporter.InfluxDB/: havret`).
**Contrib owners respond to their component; core maintainers merge.** Ping the owner, not the whole repo.

---

## 18. Building and testing

**Rule: `dotnet build OpenTelemetry.slnx -c Release` treats warnings as errors; tests run serially; a unit test builds
its own provider with an `InMemoryExporter` and asserts on what landed in a `List<T>`.**

### 18.1 Commands (from `AGENTS.md`)

```bash
dotnet build OpenTelemetry.slnx --configuration Release
dotnet test test/OpenTelemetry.Tests/OpenTelemetry.Tests.csproj --filter "FullyQualifiedName~BatchExport"
dotnet test --framework net10.0                 # one target framework only (much faster)
dotnet format OpenTelemetry.slnx --no-restore --verify-no-changes
python3 ./build/scripts/sanitycheck.py          # CI rejects non-ASCII (smart quotes, em dashes) and trailing spaces
```

**Target frameworks:** libraries build for every supported .NET plus `netstandard2.0` and `net462` (.NET Framework).
That's why you see `#if NET` / `#if NETFRAMEWORK` blocks: the same source compiles several times with different
APIs available. `net462`/`net472` tests only run on Windows. On your Mac, use `--framework net10.0`.

**Machine note:** both repos now pin the .NET **10.0.401** SDK. The scouting note from 2026-09-26 ("moving to the .NET
11 SDK, use Codespaces") is out of date at these commits; check `global.json` before choosing a machine.

### 18.2 Reading a real test: draw the object graph first (F12)

From `test/OpenTelemetry.Tests/Trace/BatchExportActivityProcessorTests.cs`:

```csharp
[Fact]
public async Task CheckIfBatchIsExportingOnQueueLimit()
{
    var exportedItems = new List<Activity>();
    using var exporter = new InMemoryExporter<Activity>(exportedItems);
    using var processor = new BatchActivityExportProcessor(
        exporter,
        maxQueueSize: 1,
        maxExportBatchSize: 1,
        scheduledDelayMilliseconds: 100_000);

    using var activity = new Activity("start")
    {
        ActivityTraceFlags = ActivityTraceFlags.Recorded,
    };

    processor.OnEnd(activity);

    await WaitForMinimumCountAsync(exportedItems, 1);

    Assert.Single(exportedItems);
    Assert.Equal(1, processor.ProcessedCount);
    Assert.Equal(1, processor.ReceivedCount);
    Assert.Equal(0, processor.DroppedCount);
}
```

```
  TEST METHOD (plays the Phone Line: calls OnEnd directly)
     │ act: processor.OnEnd(activity)
     ▼
  BatchActivityExportProcessor  ◄── REAL (the thing under test)
     │  Out-Tray: 1 slot; batch size 1; timer 100 s (so only the "batch full" trigger can fire)
     │  Mail Carrier thread: REAL
     ▼
  InMemoryExporter<Activity>    ◄── REAL class, used as a SINK: Export() just appends to the list
     │
     ▼
  List<Activity> exportedItems  ◄── what the test inspects

  NOT involved: TracerProvider, ActivitySource, sampler, listener, network. No mocks at all.
```

| Move | In this test |
|---|---|
| **Arrange** | A real processor with tiny limits (queue 1, batch 1) and a 100-second timer, so the only way an export can happen within the test is the "batch size reached" trigger. A real `Activity` with the `Recorded` flag |
| **Act** | Call `OnEnd` the way the listener would |
| **Assert** | Wait (bounded) for the background thread, then check the list has exactly one item and the counters say 1 received / 1 processed / 0 dropped |

**What it proves:** reaching `MaxExportBatchSize` wakes the Mail Carrier immediately (the `count == MaxExportBatchSize`
line in §8.5). Its sibling test, `CheckExportForRecordingButNotSampledActivity`, uses `ActivityTraceFlags.None` and
asserts the list stays **empty**: the export processor skips Activities that aren't sampled.

**Fake vs. mock, again:** `InMemoryExporter` is a *fake* (a working, simplified implementation you inspect afterwards),
not a *mock* (an object you script with expectations, like Moq in YARP's tests). This repo mostly tests with real
objects and in-memory sinks. `AGENTS.md`: "Build the provider inside the test, not in a shared constructor/fixture",
because providers register **global** listeners and two tests sharing one would see each other's spans. That's also
why tests run serially (`maxParallelThreads: 1`).

---

## 19. How contribution works here

**Rule: same shape as dotnet/iot (issue → comment → PR → review), plus four OTel-specific gates: the CNCF CLA, a
CHANGELOG line, the public-API files, and approval from an approver/maintainer (or the component owner in contrib).**

| Step | OpenTelemetry .NET specifics | Compared with dotnet/iot #2611 |
|---|---|---|
| Find work | Labels `help wanted`, `good first issue`; contrib labels by component (`comp:exporter.influxdb`) | Same idea |
| Get oriented | **"Find a buddy"**: post in `#otel-dotnet` on CNCF Slack, say what area you're interested in, and they'll pair you with an experienced contributor (`CONTRIBUTING.md`). There's also a regular SIG meeting, open to anyone | dotnet/iot has nothing like this. Worth using |
| Comment first | For anything non-trivial, and always in contrib (owners may have a design in mind) | Same |
| CLA | Signed through the CLA bot on your first PR. It's the **CNCF/Linux Foundation** CLA, **separate** from the .NET Foundation CLA you signed for #2611 | Different CLA |
| Code | Nullable on, StyleCop on, XML docs on every public member, SPDX header on every file, **no non-ASCII characters** (CI check) | Stricter style gates |
| Public API | Any public change updates `.publicApi/.../PublicAPI.Unshipped.txt` (use the IDE fix; never edit `Shipped`) | iot has API baselines too, less central |
| CHANGELOG | One line under `## Unreleased` in each affected component, linking the **PR** (not the issue) | iot doesn't require this |
| AI assistance | `CONTRIBUTING.md`: "We are open to bot generated PRs or AI/LLM assisted PRs", but spammy/incorrect ones get closed and repeat offenders blocked. `AGENTS.md` and `REVIEW.md` are written partly *for* AI agents | Disclose briefly anyway (root `CLAUDE.md` §5) |
| Review | Needs approval from an approver or maintainer; must stay open **at least one working day** (trivial doc fixes excepted). Contrib: component owners review first | Similar |
| Release | Core components released together (`core-1.19.1` tags, versioned by MinVer from git tags); contrib per component | iot releases the whole package |

Mode P still applies: the AI plans and builds, you learn it via a lecture and teach-back, and *then* you comment.

---

## 20. Where your scouted items fit on this map

```
                      §9 propagation   §10 AsyncLocal/Baggage   §11 metrics   §8.5 backpressure   §15 semconv
 L  YARP #2667 span naming                                                                        ●●●
 N  OTel #7449 Baggage leak                       ●●●
 O  MCP SDK trace across JSON-RPC  ●●●
 Q  contrib #4473 InfluxDB queue                                          ●            ●●●
 R  contrib #4516 GC config metric                                       ●●●                       ●
```

Suggested order is unchanged from the scouting doc: **O** (see a trace cross a boundary) → **N** (the subtle
in-process part) → **L** (naming) → **R** (first small metric contribution) → **Q** (stretch). Recheck each issue's
status before starting; the statuses here are from 2026-09-26.

---

## 21. Hands-on experiments (in order of payoff)

| # | Experiment | Time | Proves |
|---|---|---|---|
| X1 | §0.5 skeleton + breaks B1–B3 | 15 min | No listener → null; spans export on stop; parent links are automatic |
| X2 | §8.6 `AlwaysOffSampler` | 5 min | Null vs. context-only Activity |
| X3 | §9.5 two apps, then strip `traceparent` | 45 min | One trace across processes; how it breaks |
| X4 | Item N: `AsyncLocal<string>` vs `AsyncLocal<Holder>` vs `Baggage.Current`, parent + two child tasks | 30 min | §10.4's table, with your own output |
| X5 | Skeleton with `AddOtlpExporter()` → Aspire dashboard container (`mcr.microsoft.com/dotnet/aspire-dashboard`, OTLP on 18889; unverified flags) | 30 min | See the waterfall drawn by an Archive |
| X6 | Metrics: a counter tagged with `Guid.NewGuid()` for 3,000 iterations, console exporter | 15 min | The 2000 cardinality limit and the `otel.metric.overflow` point |
| X7 | `examples/MicroserviceExample` with `docker compose` | 1 h | Propagation through RabbitMQ (non-HTTP carrier), Collector config |

---

## 22. Edge cases and gotchas

- **`StartActivity` before the provider exists** (e.g. in static constructors, or before `app.Run()`): returns null.
  Spans during host startup are often missing for this reason.
- **Source name typos fail silently.** `AddSource("Orders.Domian")` → no error, no spans. Wildcards are allowed
  (`AddSource("Orders.*")`).
- **Activity ended on a different flow than it started**: `Activity.Current` restoration happens in the flow that
  calls `Stop`. Starting in one task and stopping in another corrupts "current" for one of them.
- **Fire-and-forget work** (`_ = Task.Run(...)`) inherits `Activity.Current`, so its spans become children of a
  request span that may have already ended. Often you want a **link** instead of a parent.
- **Long-running spans** (a WebSocket session, a consumer loop) are exported only when they end, so they may never be
  exported, or be exported after hours. Model each message as its own span.
- **Clock skew** between machines makes child spans appear to start before their parents in the waterfall. Normal;
  backends compensate partially.
- **Two SDKs in one process** (e.g. an Application Insights SDK and OTel) can both listen; double-export is possible.
- **`IsAllDataRequested` can be false** on a non-null Activity (§8.6, §13.3). Checking `activity != null` is not the
  same as "this will be exported". Expensive tag computations should check `activity?.IsAllDataRequested == true`.

## 23. Common mistakes

| Mistake | Why it's wrong | Instead |
|---|---|---|
| Creating a new `ActivitySource` per request | Each one is registered globally; it leaks and slows listener matching | One `static readonly` source per library |
| Raw IDs, emails or paths in span names or metric attributes | Unbounded cardinality; personal data in telemetry | Route templates; IDs only as span attributes, if at all |
| Not setting `service.name` | Everything shows up as `unknown_service:dotnet` | `ConfigureResource(r => r.AddService("orders"))` or `OTEL_SERVICE_NAME` |
| Forgetting `AddSource` for your own source | Your spans silently don't exist | Keep source names in one constant |
| Not disposing the provider in console apps | Last batch lost | `using var` |
| Assuming the exporter blocks or retries forever | Data loss under backpressure is by design | Watch the dropped-count self-diagnostics; size the queue |
| Treating OTel as the dashboard | Nothing to look at without a backend | Aspire dashboard locally; Grafana/Jaeger/etc. in prod |
| Using `Activity.Baggage` and expecting OTel's propagator to send it | Two different baggage stores (§10.5) | `Baggage.SetBaggage(...)` |

## 24. Interview relevance

- **"How would you debug latency in a microservice system?"** → distributed tracing: spans, parent/child, context
  propagation via `traceparent`, sampling. Draw §9.2.
- **"Metrics vs. logs vs. traces?"** → §1.2's table, plus correlation (trace ID in logs, exemplars).
- **"What's cardinality, and why does it matter?"** → §11.3. A favorite SRE/backend question.
- **"How do you avoid telemetry slowing down the app?"** → async batch export, bounded queue, drop-not-block,
  sampling, zero cost when nobody listens. That's a backpressure answer, which generalizes to any producer/consumer.
- **"How does `AsyncLocal` work / what's ambient context?"** → §10, with the Baggage bug as a concrete war story.
- **System design**: putting a Collector gateway with tail sampling in front of the backend (§13.4, §14) is a strong
  observability section in any design interview.
- **Microsoft angle:** Azure Monitor's current SDK for .NET *is* an OpenTelemetry distribution, and .NET Aspire's
  dashboard is an OTLP receiver. OTel is the observability standard inside the .NET ecosystem now.

## 25. Real-world production usage

A typical production setup: every service uses the OTel .NET SDK with ASP.NET Core, HttpClient, SQL and messaging
instrumentation, exports OTLP to a **Collector agent** on the same node; agents forward to a **gateway** pool that does
tail sampling (keep errors and slow traces, ~1–10% of the rest) and fans out to the backends: traces to Tempo/Jaeger
or a vendor, metrics to Prometheus/Mimir, logs to Loki/Elasticsearch. Dashboards and alerts are built on
semantic-convention names, so a framework upgrade that renames an attribute is a real operational event. For your
LLM_Monitor roadmap (C# gateway → Python service, Langfuse + Prometheus/Grafana), the same `traceparent` header is
what would connect the C# and Python halves of one request into one trace, and there are now semantic conventions for
generative-AI calls (`gen_ai.*`) for the LLM spans.

## 26. Check yourself

1. Your app has `AddSource("Shop")` and a library uses `new ActivitySource("Shop.Payments")`. Does the library's span
   get recorded? What one-character change to `AddSource` would make it?
2. A request arrives with `traceparent: ...-00` (not sampled). With the default sampler, what happens to the server
   span? What about with `AlwaysOnSampler` as the *only* sampler?
3. Why is the batch processor's queue bounded, and what's the trade-off of dropping the *newest* item rather than
   blocking?
4. In the Baggage table (§10.4), what single change to the *storage* would stop the leak, and whose code would break?
5. Why does ASP.NET Core instrumentation set the span name at the *end* of the request?
6. A metric has attributes `{method, route, status, customer_id}` and 50,000 customers. What does the SDK do, and what
   should you do?
7. Name the three guards that can stop a span from being exported, in the order they're checked.

## Teach-back checklist

1. **What OTel is for:** a vendor-neutral standard (spec, conventions, OTLP) plus libraries and a Collector to
   *produce and move* traces, metrics and logs; storage and display are someone else's job.
2. **The .NET twist:** `ActivitySource`/`Activity`/`Meter`/`ILogger` are the API and live in the runtime; OTel .NET is
   the SDK that subscribes to them (API package vs. SDK package, and why libraries only use the API).
3. **One span's life:** listener asked → sampler decides → Activity on the Clipboard → stop → processor `OnEnd` →
   bounded Out-Tray → background Mail Carrier → exporter → wire.
4. **No listener, no span:** `StartActivity` can return null, hence `activity?.`; a non-null Activity can still be
   unrecorded.
5. **Propagation:** `traceparent` carries trace ID + caller's span ID + sampled flag; inject on the way out, extract on
   the way in; without the header the trace splits.
6. **Ambient context:** `Activity.Current`/`Baggage.Current` are per async flow via `AsyncLocal`; values flow down,
   not up; the Baggage leak comes from storing a shared mutable holder.
7. **Metrics:** aggregated in memory per attribute combination; cardinality limit 2000 + overflow; exported every 60 s;
   cumulative by default.
8. **Backpressure and disposal:** drop-not-block when the queue is full; dispose (or flush) the provider or lose the
   last batch.
9. **Sampling and the Collector:** parent-based head sampling in the app keeps traces whole; tail sampling needs a
   Collector that sees the whole trace.
10. **Semantic conventions:** shared names (`http.route`, `{method} {route}` span names) and why low cardinality and
    stability matter; how that explains YARP #2667.

## References

- Source: [`open-telemetry/opentelemetry-dotnet`](https://github.com/open-telemetry/opentelemetry-dotnet) @ `f9dd754`
  (`AGENTS.md`, `REVIEW.md`, `CONTRIBUTING.md`, `VERSIONING.md`, `RELEASENOTES.md`, and the files named above);
  [`open-telemetry/opentelemetry-dotnet-contrib`](https://github.com/open-telemetry/opentelemetry-dotnet-contrib) @ `d3588e5`
- [OpenTelemetry specification](https://opentelemetry.io/docs/specs/otel/) ·
  [Semantic conventions](https://opentelemetry.io/docs/specs/semconv/) ·
  [HTTP span conventions](https://opentelemetry.io/docs/specs/semconv/http/http-spans/) ·
  [W3C Trace Context](https://www.w3.org/TR/trace-context/) · [W3C Baggage](https://www.w3.org/TR/baggage/)
- [OpenTelemetry .NET docs](https://opentelemetry.io/docs/languages/dotnet/) ·
  [Collector docs](https://opentelemetry.io/docs/collector/)
- [OpenTelemetry Has Graduated… Now what?](https://opentelemetry.io/blog/2026/otel-grad-now-what/) (graduation in May
  2026; profiles alpha; declarative config stable)
- Issues: [#7449](https://github.com/open-telemetry/opentelemetry-dotnet/issues/7449),
  [contrib #4473](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/issues/4473),
  [contrib #4516](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/issues/4516),
  [YARP #2667](https://github.com/dotnet/yarp/issues/2667)
- Workbench: [`scouting/001-issue_shortlist_sept_2026.md`](../../scouting/001-issue_shortlist_sept_2026.md) items L, N, O, Q, R
