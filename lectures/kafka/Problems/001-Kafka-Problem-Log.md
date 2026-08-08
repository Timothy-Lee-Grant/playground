2026_08_08_16_11-Kafka-Problem-Log

# Kafka — Problem Log

**This file is append-only.** Each time you say *"review my Kafka project again,"* a new `## Review NNN` section
gets appended to the bottom. Nothing above is ever edited or deleted — including findings that turned out to be
wrong. The history *is* the value: it shows which mistakes you make once, which ones you make three times, and
how long a class of bug survives before you stop making it.

**Protocol for each review:**

1. A **Status of Previous Findings** table (Fixed / Still open / Regressed / Superseded).
2. A **Findings** table for this pass, with severity.
3. Each finding written out: *what's wrong → what the compiler/runtime tells you → why you made it → the fix*.
4. **Recurring patterns** — the cross-review section. This is the one that matters most.
5. **Next actions**, ordered.

**Severity scale:**

| Level | Meaning |
|---|---|
| **BLOCKER** | Nothing runs until this is fixed |
| **BUG** | Compiles/starts, but behaves wrong |
| **DESIGN** | Works, but the shape is wrong and will hurt later |
| **PROCESS** | Not about the code — about how the code got written |
| **NOISE** | Cosmetic / dead code, but diagnostic of something |

---
---

## Review 001 — 2026_08_08

**Snapshot reviewed:** commit `90aaadb` ("attempting fixes"), working tree at 16:11.
**Scope:** `kafka/` — `docker-compose.yml`, `MyKafkaSystem.Producer`, `MyKafkaSystem.ConsumerOne`,
`MyKafkaSystem.ConsumerTwo`, `MyKafkaSystem.Contracts`.

### Status of previous findings

None — this is the first review.

### Verdict in one paragraph

The project does not compile, and the Docker Compose file does not start. Those are the headline facts, but they
are not the interesting ones. The interesting fact is this: **you created four .NET projects, a solution file, a
contracts assembly, and a Compose file before a single byte ever moved through a broker.** Of the four projects,
two (`ConsumerOne`, `ConsumerTwo`) are still the unmodified `dotnet new worker` template and don't even reference
`Confluent.Kafka`; a third (`Contracts`) defines a record that nothing references. Roughly 75% of the structure
you built is scaffolding around a path that has never once been walked end to end. That is the actual root cause
of today feeling like a wall, and it is the same thing your seniors are describing when they say you're slow.
The per-finding fixes are below; the structural argument is in
`lectures/engineering-practice/001-Closing-The-Loop.md`.

### Findings

| ID | Severity | File | Finding |
|---|---|---|---|
| 001-01 | BLOCKER | `Producer.cs` | Primary constructor and explicit constructor with identical signatures |
| 001-02 | BLOCKER | `Producer.cs` | `BootStrapServers` — wrong casing, no such member |
| 001-03 | BLOCKER | `Producer.cs` | `public overrride void Dispose()` — typo |
| 001-04 | BLOCKER | `Producer.cs` | Leftover `_producer.Produce(..., handler)` — neither symbol exists |
| 001-05 | BLOCKER | `docker-compose.yml` | Network `taks-net` referenced, `task-net` defined |
| 001-06 | BUG | `docker-compose.yml` | `KAFKA_NODE` should be `KAFKA_NODE_ID` |
| 001-07 | BUG | `docker-compose.yml` | No volume; broker log lives in the container's `/tmp` |
| 001-08 | BUG | `Producer.cs` | `_logger` and primary-ctor `logger` used in the same method |
| 001-09 | DESIGN | `docker-compose.yml` | No healthcheck — no signal for "broker is ready" |
| 001-10 | DESIGN | `Producer.cs` | Delivery result discarded; you never learn partition/offset |
| 001-11 | DESIGN | `*.csproj` | `Contracts` referenced by nothing; producer sends raw strings |
| 001-12 | DESIGN | `ConsumerOne/Two` | Untouched template — no Kafka package, no consumer, no group |
| 001-13 | DESIGN | `Producer.cs` | Broker address hardcoded, not from configuration |
| 001-14 | NOISE | `Producer.cs` | Six unnecessary `using` directives, four of them meaningless |
| 001-15 | NOISE | `docker-compose.yml` | `KAFKA_CONNECTOR_PORT` is not a Kafka setting |
| 001-16 | NOISE | `docker-compose.yml` | `version: '3.8'` is obsolete in Compose v2 |
| 001-17 | PROCESS | git history | Three commits in 16 minutes, none of which compiled |

---

### 001-01 — BLOCKER — Two constructors, same signature

```csharp
public class Producer(ILogger<Producer> logger) : BackgroundService   // ← primary constructor
{
    public Producer(ILogger<Producer> logger)                          // ← and again
    { ... }
```

**What the compiler says:** `CS0111: Type 'Producer' already defines a member called '.ctor' with the same
parameter types`, and `CS8862: A constructor declared in a type with a parameter list must have 'this' constructor
initializer`.

**What's actually happening.** `class Producer(ILogger<Producer> logger)` is a **primary constructor** (C# 12).
That parameter list *is* a constructor. Writing a second constructor with the same signature is the same error as
declaring the same method twice. And C# has a hard rule: once a type has a primary constructor, *every* other
constructor must chain to it with `: this(...)`, because the primary constructor's parameters have to be
initialized no matter which entry point you came in through.

**Why you made it.** You started from the `dotnet new worker` template, which uses a primary constructor, and then
needed constructor *logic* (build the config, build the producer). Primary constructors have no body, so you added
a normal constructor — without removing the one you already had.

**This is the second time.** `reactive/`, two days ago, produced the same shape: a primary constructor plus a
non-chaining secondary constructor fighting each other, with a `var` shadow in the body. That makes it a genuine
knowledge gap, not a slip.

**The rule to memorize:** *a primary constructor is for capturing parameters, not for running code. The moment you
need a constructor body, delete the primary constructor.*

**Fix:**

```csharp
public class Producer : BackgroundService     // no parameter list
{
    private readonly ILogger<Producer> _logger;
    private readonly IProducer<Null, string> _kafkaProducer;

    public Producer(ILogger<Producer> logger)
    {
        _logger = logger;
        // ...
    }
}
```

---

### 001-02 — BLOCKER — `BootStrapServers`

```csharp
BootStrapServers = "localhost:9092",   // ← capital S
```

**Compiler:** `CS0117: 'ProducerConfig' does not contain a definition for 'BootStrapServers'`.

**Correct member:** `BootstrapServers` — lowercase `s`. (Verified directly against the shipped
`Confluent.Kafka.dll` in your `bin/`: it exports `get_BootstrapServers`/`set_BootstrapServers`, nothing with a
capital `S`.) It maps to the librdkafka property `bootstrap.servers`; the C# binding PascalCases each dot-separated
word once, so `bootstrap.servers` → `BootstrapServers`, not `BootStrapServers`.

**Why this one matters more than a typo should.** It is a *sixteen-second* bug — IntelliSense would have
struck it out, and `dotnet build` names it exactly. It survived across three commits because you never built.
See 001-17.

**Fix:**

```csharp
BootstrapServers = "localhost:9092",
```

---

### 001-03 — BLOCKER — `overrride`

```csharp
public overrride void Dispose()
```

Three r's. This is a plain typo, but worth confirming the surrounding claim, because it's the kind of thing you'd
otherwise burn twenty minutes second-guessing: **`BackgroundService.Dispose()` really is `public virtual void
Dispose()`**, so `override` is legal here. Its base implementation calls `_stoppingCts?.Cancel()`, which is why
calling `base.Dispose()` last (after your flush) is the right order — you want to flush while the world is still
alive, then let the base cancel.

**Fix:** `public override void Dispose()`.

---

### 001-04 — BLOCKER — The leftover line

```csharp
_producer.Produce("my-topic", new Message<Null, string> {Value = "hello world"}, handler);
```

**Compiler:** `CS0103: The name '_producer' does not exist in the current context` and `CS0103: The name 'handler'
does not exist in the current context`.

This is a fossil from your first attempt (commit `bc4e6fb`), when the field was called `_producer` and you'd
sketched a delivery-report callback named `handler` that was never written. When you rewrote the body in `6bc52c0`
you added the new `ProduceAsync` block *above* it instead of replacing it. The result is a method that, if it
compiled, would send two different messages per second.

**Fix:** delete the line.

**The habit worth extracting:** when you replace an approach, delete the old one in the same edit. Leaving both
"just in case" means you now have to hold two mental models of the method at once, and the compiler errors from
the dead one mask the real errors in the live one.

---

### 001-05 — BLOCKER — `taks-net`

```yaml
networks:
  task-net:        # ← defined as task-net
    driver: bridge
services:
  kafka:
    networks:
      - taks-net   # ← referenced as taks-net
```

**What Docker says:** `service "kafka" refers to undefined network taks-net: invalid compose project`.

The whole file fails to parse. Nothing starts. **Nothing about your C# has ever been tested against a real
broker**, which means every "connection refused" you saw today was telling you about this line, not about your
code.

**Fix:** `- task-net`.

---

### 001-06 — BUG — `KAFKA_NODE` → `KAFKA_NODE_ID`

In KRaft mode every node needs a unique integer ID, and it must match the left side of
`KAFKA_CONTROLLER_QUORUM_VOTERS: '1@kafka:29093'`. The setting is `node.id`, so the Confluent image's env var is
`KAFKA_NODE_ID`.

`KAFKA_NODE` is not a real setting. The Confluent image translates every `KAFKA_*` variable into a line in
`server.properties`, so you end up with a property called `node` that Kafka logs as *"The configuration 'node' was
supplied but isn't a known config"* — a warning buried in the startup log — while `node.id` stays unset and the
broker refuses to start in KRaft mode.

**This is the exact failure mode of environment-variable configuration and it's worth internalizing:** there is no
schema, no autocomplete, and no error. A misspelled env var is silently accepted and silently ignored. When
configuring anything through env vars, the verification step is *read the container's startup log for
"isn't a known config"*, not "the container is running."

**Fix:** `KAFKA_NODE_ID: 1`.

---

### 001-07 — BUG — The volume that isn't there

You have no `volumes:` section, and `KAFKA_LOG_DIRS: '/tmp/kraft-combined-logs'` points the broker's log at a
throwaway path inside the container.

Kafka's entire identity — the thing separating it from Redis Pub/Sub, per your own lecture 001 — is that
**messages survive**. With no volume, `docker compose down` erases the log, which means the single most
instructive demo in the whole exercise (*kill the consumer → produce 10 messages → restart the consumer → watch
all 10 arrive*) can be silently broken by an unrelated `down`/`up`.

**Note the two-part fix.** Mounting a volume alone does nothing if the broker writes somewhere else; the mount path
and `KAFKA_LOG_DIRS` have to agree. Half-fixes like this ("I added a volume, why didn't it persist?") are a
classic source of long, wrong debugging sessions.

**Fix:** see the corrected Compose file at the end of this review.

---

### 001-08 — BUG — Two loggers, one method

```csharp
private readonly ILogger<Producer> _logger;   // field, assigned in the ctor
// ...
logger.LogInformation("Producer running at: {time}", ...);   // primary-ctor parameter
_logger.LogError(ex, "Kafka delivery failed: ...");          // field
```

Same object, two names, both live in one method. Once 001-01 is fixed the primary-ctor `logger` disappears and this
resolves — but flag it as a *symptom*: you were editing without a coherent model of which construction mechanism
you were using. When you can't answer "where does this variable come from?" instantly, stop and simplify rather
than adding another one.

---

### 001-09 — DESIGN — No healthcheck

A Kafka broker takes several seconds to become usable after the container reports "running." Without a
healthcheck, `docker compose up -d` returns almost immediately, you start the producer, and you get connection
errors — which look exactly like code bugs. You will spend that time reading your C#.

```yaml
healthcheck:
  test: ["CMD", "kafka-broker-api-versions", "--bootstrap-server", "localhost:9092"]
  interval: 5s
  timeout: 10s
  retries: 12
```

Now `docker compose ps` says `healthy` or it doesn't, and you know which layer to look at. **Every piece of
infrastructure you add to a Compose file should come with a healthcheck in the same edit** — it converts an
ambiguous failure into an unambiguous one, and ambiguous failures are where hours go.

---

### 001-10 — DESIGN — You throw away the answer

```csharp
DeliveryResult<Null, string> ReadResult = await _kafkaProducer.ProduceAsync(...);
```

`ReadResult` is never used. (It's also PascalCase — locals are camelCase — and named "Read" for a write.)

That object is the single most educational thing Kafka hands you: it tells you the **topic, partition, and
offset** the message landed at. Logging it means that the first time you run this, you *see* partitions and
offsets increment — the two concepts your lecture 001 says are load-bearing — instead of reading about them.

```csharp
var result = await _kafkaProducer.ProduceAsync("my-topic", message, stoppingToken);

_logger.LogInformation("Delivered to {Topic}[{Partition}]@{Offset}",
    result.Topic, result.Partition.Value, result.Offset.Value);
```

**Related, worth knowing now:** `ProduceAsync` returns a `Task` per message and you `await` it, so each send waits
for the broker's ack before the next begins. The alternative, `Produce(...)`, is fire-and-forget with a delivery
*callback* — much higher throughput, harder to reason about. (`handler` in your dead line 001-04 was you reaching
for this second form.) For learning, `ProduceAsync` is the right choice: it's slower and it makes failure visible.
Choose it deliberately, not accidentally.

---

### 001-11 — DESIGN — `Contracts` is dead code

`MyKafkaSystem.Contracts/TaskCreatedEvent.cs` defines a nice record:

```csharp
public record TaskCreatedEvent(Guid TaskId, string TaskType, string Payload, DateTime CreatedAtUtc);
```

Nothing references it. `MyKafkaSystem.Producer.csproj` has no `<ProjectReference>` to it, and the producer sends
`$"Hello Kafka! {DateTimeOffset.Now}"` — a bare string.

The instinct behind a shared contracts assembly is *correct and senior*: producer and consumers must agree on the
wire format, and a shared type is the standard way to enforce that. The problem is purely one of **ordering**. You
built the agreement before there were two parties to agree, and the cost is that it sat there for an hour as a
decision you'd already spent thinking-budget on but couldn't validate.

The right moment for `Contracts` is the moment you have a working producer *and* a working consumer exchanging
strings, and you go "these two now need to agree on a shape." That's step 6, not step 1.

**One small thing to fix when you do wire it up:** `DateTime CreatedAtUtc` should be `DateTimeOffset`. `DateTime`
does not carry an offset, so "is this UTC?" becomes a convention enforced only by the field name — which is exactly
the kind of thing that goes wrong across process boundaries.

---

### 001-12 — DESIGN — The consumers do not exist

`ConsumerOne/Worker.cs` and `ConsumerTwo/Worker.cs` are byte-for-byte the `dotnet new worker` template. Neither
`.csproj` references `Confluent.Kafka`. There is no consumer, no `GroupId`, no `Subscribe`, no offset handling.

So: **two of your four projects are empty costumes.** They make the solution look like a distributed system while
containing none of it. Worse, `ConsumerTwo` exists to demonstrate consumer-group behaviour — a concept you can't
observe until `ConsumerOne` works — so it's a placeholder for a placeholder.

Here is a minimal working consumer for `ConsumerOne`, with the one non-obvious trap called out:

```csharp
using Confluent.Kafka;

namespace MyKafkaSystem.ConsumerOne;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Consume() is BLOCKING and synchronous — Confluent.Kafka has no async consume.
        // Calling it directly here would block the host's startup path. Task.Run moves it
        // to a thread-pool thread. This is the #1 thing people get wrong with this library.
        return Task.Run(() =>
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId          = "consumer-one",
                AutoOffsetReset  = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe("my-topic");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var cr = consumer.Consume(stoppingToken);
                    logger.LogInformation("[{Partition}]@{Offset} {Value}",
                        cr.Partition.Value, cr.Offset.Value, cr.Message.Value);
                }
            }
            catch (OperationCanceledException) { /* shutdown */ }
            finally
            {
                consumer.Close();   // commits final offsets and leaves the group cleanly
            }
        }, stoppingToken);
    }
}
```

Add to `MyKafkaSystem.ConsumerOne.csproj`:

```xml
<PackageReference Include="Confluent.Kafka" Version="2.15.0" />
```

Three details worth understanding rather than copying:

- **`Ignore` and `Null`.** These are Confluent marker types, not language keywords. `Null` means "the key is
  always null" (producer side); `Ignore` means "don't even deserialize the key" (consumer side). This is also why
  your earlier `DeliveryResult<null, string>` failed — `null` is a literal, `Null` is a type. You already caught
  that one in commit `90aaadb`; good.
- **`AutoOffsetReset.Earliest`.** Only applies when the group has *no* committed offset — i.e. the first run. It's
  what makes "start a consumer and see the history" work. Leave it `Latest` and a fresh consumer looks broken.
- **`consumer.Close()`.** Skip it and the group waits out `session.timeout.ms` (~45s) before rebalancing, so
  restarting the consumer appears to hang. This will absolutely confuse you at step 8 if you don't know it now.

---

### 001-13 — DESIGN — Hardcoded broker address

`"localhost:9092"` is baked into the constructor. It's the *correct* address for now — it matches the
`PLAINTEXT_HOST` listener, which exists so processes on your Mac can reach the broker. But note that you have two
listeners for a reason, and you should be able to say why out loud:

| Listener | Advertised as | Who uses it |
|---|---|---|
| `PLAINTEXT` | `kafka:29092` | Clients *inside* the Compose network (container-to-container) |
| `PLAINTEXT_HOST` | `localhost:9092` | Clients on your Mac (`dotnet run` from the terminal) |

The broker tells every connecting client which address to use for follow-up traffic — that's what *advertised*
means. Get it wrong and you see the classic symptom: the initial connection succeeds, then everything times out,
because the client was handed an address it can't resolve.

Move it to `appsettings.json` when you containerize the producer, not before — that's the moment the value
actually needs to differ per environment.

---

### 001-14 — NOISE — The `using` shotgun

```csharp
using Confluent.Kafka;
using Microsoft.Extensions.Logging;     // already implicit (Worker SDK)
using System.Collections.Concurrent;    // unused
using System.Net;                       // unused
using System.Reflection;                // unused
using Microsoft.Extensions.Hosting;     // already implicit
using System;                           // already implicit
using System.IO.Pipelines;              // unused
```

Only the first is doing anything. `System.Reflection` is a fossil from `ProducerBuilder<NullabilityInfo, string>`
in your first attempt — the IDE offered a `using` to resolve a type you'd typed by accident, and you took it.

This is worth naming as a behaviour, because it's a reliable tell: **adding `using` directives hoping an error
goes away.** It never works, because "type not found" almost always means *you named the wrong type*, not *the
namespace is missing*. When the IDE offers a using for a type you don't recognise, that's a signal you typed the
wrong thing.

**Fix:** delete all but `using Confluent.Kafka;`. The Worker SDK's `ImplicitUsings` already covers `System`,
`Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Logging`, and more.

---

### 001-15 — NOISE — `KAFKA_CONNECTOR_PORT: 9093`

Not a Kafka setting. It becomes `connector.port` in `server.properties`, gets a "not a known config" warning, and
does nothing. You likely meant it to describe the controller port — but that's already fully specified by
`KAFKA_LISTENERS` (`CONTROLLER://0.0.0.0:29093`) and `KAFKA_CONTROLLER_QUORUM_VOTERS` (`1@kafka:29093`).

Delete it. Config lines that do nothing are worse than absent ones: next time you debug this file you will read it
and believe it means something.

---

### 001-16 — NOISE — `version: '3.8'`

Compose v2 ignores the top-level `version` key and warns that it's obsolete. Delete it. Add `name:` instead if you
want a stable project name.

---

### 001-17 — PROCESS — Three commits, sixteen minutes, zero builds

This is the most important finding in this document.

| Commit | Time | Message | Compiles? |
|---|---|---|---|
| `bc4e6fb` | 15:36 | "my incorrect attempt" | No |
| `6bc52c0` | 15:50 | "attempting fixes" | No |
| `90aaadb` | 15:52 | "attempting fixes" | No |

Look at what `90aaadb` — your most recent, most considered state — actually changed: it fixed
`DeliveryResult<null,...>` → `DeliveryResult<Null,...>` (a real fix, found by reading) and simultaneously added
`public overrride void Dispose()` (a new blocker). **Net progress: zero.** And you couldn't know that, because
nothing ran.

Meanwhile the file contains, at that moment: `BootStrapServers`, a duplicate constructor, an undefined `_producer`,
and an undefined `handler`. `dotnet build` would have listed all five errors, by file and line, in about four
seconds. Instead you spent sixteen minutes reasoning about which one might be the problem.

**This is the mechanism behind "you're too slow."** Not depth. Not curiosity. The gap between writing a line and
finding out whether it's right. Everything else in this review is downstream of it — the shotgunned usings, the
duplicated `Produce`, the two loggers: those are all what code looks like when it's been edited many times without
ever being executed.

The full argument, and the protocol to fix it, is in
`lectures/engineering-practice/001-Closing-The-Loop.md`. The one-line version:

> Run `dotnet build` after every edit that changes more than one line. Not at the end. After every edit.

---

### Corrected `docker-compose.yml`

```yaml
name: kafka-exercise

networks:
  task-net:
    driver: bridge

volumes:
  kafka-data:

services:
  kafka:
    image: confluentinc/cp-kafka:7.6.0
    container_name: kafka
    ports:
      - "9092:9092"
    networks:
      - task-net
    volumes:
      - kafka-data:/var/lib/kafka/data
    environment:
      CLUSTER_ID: 'MkU3OEVBNTcwNTJENDM2Qk'
      KAFKA_NODE_ID: 1
      KAFKA_PROCESS_ROLES: 'broker,controller'
      KAFKA_CONTROLLER_QUORUM_VOTERS: '1@kafka:29093'
      KAFKA_CONTROLLER_LISTENER_NAMES: 'CONTROLLER'
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: 'CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT'
      KAFKA_LISTENERS: 'PLAINTEXT://0.0.0.0:29092,CONTROLLER://0.0.0.0:29093,PLAINTEXT_HOST://0.0.0.0:9092'
      KAFKA_ADVERTISED_LISTENERS: 'PLAINTEXT://kafka:29092,PLAINTEXT_HOST://localhost:9092'
      KAFKA_INTER_BROKER_LISTENER_NAME: 'PLAINTEXT'
      KAFKA_LOG_DIRS: '/var/lib/kafka/data'
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
      KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS: 0
      KAFKA_AUTO_CREATE_TOPICS_ENABLE: 'true'
    healthcheck:
      test: ["CMD", "kafka-broker-api-versions", "--bootstrap-server", "localhost:9092"]
      interval: 5s
      timeout: 10s
      retries: 12
```

Changed from yours: removed `version`, fixed `taks-net`, `KAFKA_NODE` → `KAFKA_NODE_ID`, dropped
`KAFKA_CONNECTOR_PORT`, moved `KAFKA_LOG_DIRS` onto a named volume, added `KAFKA_AUTO_CREATE_TOPICS_ENABLE`
explicitly (it was defaulting to `true` and silently creating `my-topic` with **one** partition — worth knowing,
since a one-partition topic hides most consumer-group behaviour), and added the healthcheck.

### Corrected `Producer.cs`

```csharp
using Confluent.Kafka;

namespace MyKafkaSystem.Producer;

public class Producer : BackgroundService
{
    private readonly ILogger<Producer> _logger;
    private readonly IProducer<Null, string> _kafkaProducer;

    public Producer(ILogger<Producer> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers  = "localhost:9092",
            Acks              = Acks.All,
            EnableIdempotence = true   // set it explicitly; librdkafka's default is not the Java default
        };

        _kafkaProducer = new ProducerBuilder<Null, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = new Message<Null, string>
                    {
                        Value = $"Hello Kafka! {DateTimeOffset.Now:O}"
                    };

                    var result = await _kafkaProducer.ProduceAsync("my-topic", message, stoppingToken);

                    _logger.LogInformation("Delivered to {Topic}[{Partition}]@{Offset}",
                        result.Topic, result.Partition.Value, result.Offset.Value);
                }
                catch (ProduceException<Null, string> ex)
                {
                    _logger.LogError(ex, "Kafka delivery failed: {Reason}", ex.Error.Reason);
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown — the host cancelled stoppingToken
        }
    }

    public override void Dispose()
    {
        _kafkaProducer.Flush(TimeSpan.FromSeconds(5));
        _kafkaProducer.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

Note the exception structure, since it's a real thing you'd otherwise hit: `ProduceException` is caught *inside*
the loop so a transient broker failure logs and retries. `OperationCanceledException` is caught *outside* the loop
because it means "shut down," and it can come from either `ProduceAsync` or `Task.Delay`. Left unhandled, it faults
`ExecuteAsync` and (since .NET 6, whose default `BackgroundServiceExceptionBehavior` is `StopHost`) makes a clean
Ctrl-C look like a crash.

`Flush` before `Dispose` matters: librdkafka buffers in a background thread, so without the flush your last
second or so of messages die in memory. That was a genuinely good instinct on your part — the only thing wrong
with it was the spelling.

---

### Recurring patterns

Only one review so far, so this section is a seed. Two candidates to watch:

| Pattern | Evidence | Watch for |
|---|---|---|
| **Primary-constructor collision** | `reactive/` (2026-08-05), `kafka/` (2026-08-08) | Two occurrences in four days. If it appears a third time, stop and write out the primary-constructor rules by hand. |
| **Breadth-first scaffolding** | `reactive/` split into two processes before one worked; `kafka/` built 4 projects before one message flowed | Any time you create a second project, second service, or shared-contract assembly *before* the first path runs end to end. |

---

### Next actions, in order

Do these strictly in sequence. Do not start step *n+1* until step *n* has produced visible output.

1. Fix `taks-net` → `task-net`. Run `docker compose up -d`. Run `docker compose logs kafka | grep -i "not a known config"` and fix everything it names.
2. `docker compose exec kafka kafka-topics --bootstrap-server localhost:9092 --list`. **Proves the broker works with zero C# involved.** If this fails, no amount of C# debugging helps.
3. `docker compose exec kafka kafka-console-producer --bootstrap-server localhost:9092 --topic my-topic`, and in a second terminal `kafka-console-consumer --bootstrap-server localhost:9092 --topic my-topic --from-beginning`. Type; watch it arrive. **You now have a known-good reference implementation.** Every future failure is either "the console tools work and my C# doesn't" (your bug) or "neither works" (infrastructure).
4. Fix `Producer.cs` per above. `dotnet build`. Then `dotnet run`. Watch messages appear in the console consumer from step 3.
5. Only now: implement `ConsumerOne`. Kill the console consumer. `dotnet build`, `dotnet run`.
6. **The payoff demo:** stop `ConsumerOne`. Let the producer run 20 seconds. Restart `ConsumerOne`. Watch it catch up. *That* is the thing Redis Pub/Sub cannot do, and it's the entire point of this exercise.
7. Then, and only then: `ConsumerTwo` (same `GroupId` to see partition splitting — you'll need to create the topic with `--partitions 3` first, or the split won't happen; different `GroupId` to see independent offsets).
8. Then, and only then: wire in `Contracts` + JSON serialization.

Steps 1–4 should take under 45 minutes now that the errors are enumerated. If any single step exceeds 20 minutes,
that's the signal to stop and write down what you don't understand rather than continue editing.
