2026_08_05_22_41-The-Fast-Librarian-Setting-Up-Redis-In-CSharp

# The Fast Librarian
### Getting a real Redis server talking to a real C# process — from Docker to Pub/Sub

---

## 0. Why this folder exists, and what it's actually for

You've said the goal isn't a useful app — it's learning to *integrate* a real piece of infrastructure into a C# project, end to end: run the server, connect a client, use its actual protocol, see it fail in the ways it really fails. That's a different (and in some ways harder) skill than writing algorithms — it's "how do I plug two separate systems together and reason about the seam between them," which is most of what backend/infra engineering actually is day to day.

This also isn't a random new topic. Lecture `002-Whats-Actually-Inside-The-Microphone.md` ended by pointing out that your `reactive/` exercise hit a real wall: `Subject<T>`/`event` only work *inside one process's memory*, and the moment you split `Worker` and `Listener` into two separate `dotnet run` processes, there was no way for them to reach each other. Redis Pub/Sub is the most direct fix to exactly that problem — it's a real, separate broker process that unrelated C# processes (even on different machines) can both connect to over the network. This lecture is the "how" for that.

---

## 1. What Redis actually is, in one sentence

**Redis is a single-threaded, in-memory key/value server that answers every request out of RAM, and also happens to speak Pub/Sub as one of its features.** That's the whole model. There's no query planner, no schema, no disk-first storage (by default) — it's closer to a shared, network-accessible `Dictionary<string, T>` with a very fast wire protocol, plus a handful of extra data structures and features bolted on: lists, sets, sorted sets, hashes, expiring keys, and — the part relevant to you right now — **channels**, which are the Pub/Sub primitive.

**The character:** Redis is **The Fast Librarian** — she keeps everything on the desk in front of her, not filed away in the archive, so she can hand you an answer instantly. She has a megaphone too (Pub/Sub): she'll shout a message to everyone currently standing in the room, but she keeps no transcript — if you weren't in the room when she shouted, you'll never know she said it. That last property is the single most important thing to understand about Redis Pub/Sub, and §6 comes back to it.

---

## 2. The architecture you're about to build

```
┌───────────────────────┐        TCP :6379        ┌───────────────────────┐
│   Publisher process    │ ───────────────────────▶│                       │
│  (C# console app /      │                        │   Redis Server        │
│   your old "Worker")    │                        │   (Docker container)  │
└───────────────────────┘                          │                       │
                                                     │  channel registry:    │
┌───────────────────────┐        TCP :6379          │  "ticks" -> [conn A,  │
│  Subscriber process     │◄───────────────────────│              conn B]  │
│  (C# console app /      │                        └───────────────────────┘
│   your old "Listener")  │
└───────────────────────┘
```

Compare this to the diagram from lecture 002: before, `Worker` held the subscriber list *inside its own process's memory* (a `Subject<T>`/`List<IObserver<T>>`). Now, **the subscriber list lives inside the Redis server process**, and your two C# apps are just thin network clients talking to it. That's the entire conceptual shift: you're not writing the broadcaster anymore — Redis *is* the broadcaster; you're writing two clients that connect to it.

---

## 3. Getting Redis running — Docker, not an install

Don't install Redis natively (Windows support is poor and you don't want it as a permanent background service). Run it as a disposable container:

```bash
docker run --name redis -p 6379:6379 -d redis:7
```

Or, since you'll likely want it alongside your C# project long-term, a `docker-compose.yml` in the `redis/` folder (infra config, not "implementing" the exercise, so this is fine to add there):

```yaml
services:
  redis:
    image: redis:7
    ports:
      - "6379:6379"
```

Verify it's actually alive before writing a single line of C# — this is a habit worth having permanently: **prove the server works with the server's own tool before blaming your client code.**

```bash
docker exec -it redis redis-cli ping
# PONG
```

`redis-cli` is Redis's own command-line client — the same protocol your C# app will speak, just typed by hand. Worth poking around in it directly for five minutes (`SET foo bar`, `GET foo`, `PUBLISH mychannel hello`) before automating any of it, so when the C# version behaves oddly later, you already know what "correct" looks like.

---

## 4. The C# client: `StackExchange.Redis`

This is the de facto standard .NET client — not "a" library, effectively *the* library (used by Stack Overflow itself, which is where the name comes from).

```bash
dotnet add package StackExchange.Redis
```

### The one concept you must get right immediately: the `ConnectionMultiplexer`

```csharp
using StackExchange.Redis;

IConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379");
```

The name is precise, not marketing: this object holds **one physical TCP connection** and *multiplexes* many logical Redis commands over it concurrently (the same idea as HTTP/2 multiplexing several requests over one TCP connection instead of opening a new one per request). It is:

- **Thread-safe** — safe to share across your whole app.
- **Expensive to create** — it does a real TCP handshake and protocol negotiation.
- **Meant to be created exactly once and reused for the lifetime of your process.**

> **The single most common StackExchange.Redis mistake, seen constantly in real production incidents:** calling `ConnectionMultiplexer.Connect(...)` inside a request handler or a loop, once per operation. This creates a new TCP connection every time, exhausts connection limits under load, and adds handshake latency to every call. Register it once — in a Worker Service, that means a **singleton** in DI, exactly like you already learned to do with your own `StateService`:

```csharp
// Program.cs
builder.Services.AddSingleton<IConnectionMultiplexer>(
    _ => ConnectionMultiplexer.Connect("localhost:6379"));
```

---

## 5. Step 1 of the exercise: prove the client works with plain key/value first

Don't jump straight to Pub/Sub. Get the simplest possible round-trip working so you know your Docker container, your connection string, and your package reference are all correct *before* adding the complexity of channels and background listeners.

```csharp
IDatabase db = redis.GetDatabase();

db.StringSet("greeting", "hello from csharp");
string? value = db.StringGet("greeting");
Console.WriteLine(value);   // hello from csharp
```

`GetDatabase()` is cheap and can be called as often as you like — it's a lightweight logical handle over the shared multiplexer, not a new connection. (This trips people up: "one multiplexer, many `GetDatabase()` calls" is correct; "one multiplexer per operation" is the mistake.)

Once that prints correctly, you've validated the entire pipeline end to end. Now add the actual feature you're here for.

---

## 6. Step 2: Pub/Sub — this is the payoff

```csharp
ISubscriber subscriber = redis.GetSubscriber();

// --- Subscriber side (run this in one process) ---
await subscriber.SubscribeAsync("ticks", (channel, message) =>
{
    Console.WriteLine($"got: {message}");
});

// --- Publisher side (run this in a DIFFERENT process) ---
await subscriber.PublishAsync("ticks", "tick at " + DateTimeOffset.Now);
```

Notice: both sides call `redis.GetSubscriber()` — "subscriber" here is Redis's name for "the Pub/Sub handle," and confusingly it's the *same* object type used for both publishing and subscribing (a naming quirk of the library, not a design flaw worth worrying about).

Map this directly onto what you already built by hand in lecture 001/002:

| Your hand-rolled version | Redis's version |
|---|---|
| `Dictionary<string, List<Action>>` keyed by event name | Redis's internal channel registry, keyed by channel name (`"ticks"`) — same idea, just living in the Redis process instead of yours |
| `_events.OnNext(message)` / `Triggered?.Invoke(msg)` | `subscriber.PublishAsync("ticks", msg)` |
| `_events.Subscribe(msg => ...)` / `+= myTriggerFunction` | `subscriber.SubscribeAsync("ticks", (ch, msg) => ...)` |
| Limited to one process's memory | Works across processes, machines, languages — any Redis client in any language can publish/subscribe to `"ticks"` |

You are not learning a new *concept* here. You're learning that the same Observer-pattern shape you already understand gets a genuinely new capability (crossing process boundaries) once you move the subscriber registry into a shared server. That's the entire lesson of this exercise, and it's worth feeling that click.

### The one property of Redis Pub/Sub you must not miss

> **Redis Pub/Sub is fire-and-forget. If no subscriber is connected at the moment you `PUBLISH`, that message is gone forever — not queued, not stored, not replayable.** There is no "catch up on what I missed." The Fast Librarian's megaphone reaches only whoever is standing in the room *right now*.

This is a deliberate design tradeoff, not a bug: it's what makes Redis Pub/Sub extremely cheap and fast. But it means Redis Pub/Sub is the wrong tool the moment you need "every message must eventually be processed, even if the consumer was down for five minutes" — that requirement is exactly what **Kafka** (see the companion lecture in `lectures/kafka/`) exists to solve, by writing every message to a durable, replayable log instead of just broadcasting it. Keep that contrast in your head as you build both exercises — it's a real, common system-design decision point ("do I need durability/replay, or is at-most-once broadcast fine?").

### If you want durability without leaving Redis: Streams (stretch goal, optional)

Redis also has **Streams** (`XADD`, `XREAD`, `XREADGROUP`, `XACK`) — an append-only log data structure inside Redis itself, with consumer groups that track their own read position. It's Redis's own answer to "I want some of what Kafka offers without running a second system." Not necessary for this exercise, but worth knowing it exists as the bridge concept between Pub/Sub and Kafka, if you want a third mini-exercise later.

---

## 7. Suggested build order for this exercise

1. `docker-compose.yml` in `redis/`, `docker compose up -d`, confirm `redis-cli ping` → `PONG`.
2. One console app project: connect, `StringSet`/`StringGet` round-trip, confirm end to end.
3. Split into **two** console app projects (this is the corrected version of your `reactive`/`listener1` split): a `Publisher` that loops and calls `PublishAsync("ticks", ...)` every second (structurally identical to your old `Worker.ExecuteAsync` loop — same shape, new transport), and a `Subscriber` that calls `SubscribeAsync` once and then just waits (`Console.ReadLine()` or an infinite delay) so the process stays alive to receive callbacks.
4. Run both, in separate terminals, at the same time. Watch messages cross the process boundary that your `reactive`/`listener1` attempt couldn't cross. Then **stop the subscriber, publish a few messages, restart the subscriber** — confirm for yourself that those messages are simply lost. That's not a bug to fix; it's the property from §6 to feel firsthand.
5. Stretch: swap `Subscribe`/`Publish` for `StreamAdd`/`StreamRead` with a consumer group, and repeat step 4 — this time the messages should still be there when the subscriber reconnects. That contrast, felt directly, is worth more than reading about it.

---

## 8. Common mistakes

- **A `ConnectionMultiplexer` per operation** (§4) — the single biggest real-world Redis performance bug category.
- **Using the sync API (`StringSet`, `Subscribe`) inside `async` methods instead of the `*Async` versions** (`StringSetAsync`, `SubscribeAsync`) — blocks a thread-pool thread waiting on network I/O for no reason; always prefer the `Async` suffix in an `async` codebase.
- **Assuming Pub/Sub persists anything** (§6) — the mistake that costs the most in production if discovered late: "why did we lose messages during the deploy restart?" — answer: that's what Pub/Sub always does, by design.
- **Forgetting the subscriber process needs to stay alive** — `SubscribeAsync` registers a callback and returns immediately; if your `Main` method then exits, the process dies and there's nothing left to receive callbacks. In a Worker Service this isn't an issue (the host keeps running), but in a bare console app you need something blocking at the end (`Console.ReadLine()`, `await Task.Delay(Timeout.Infinite)`, or just make it a proper `BackgroundService` like you already know how to do).

---

## 9. Interview relevance

Redis shows up constantly in system-design interviews for three roles: **cache** (in front of a slower database), **ephemeral coordination** (rate limiting via `INCR`+expiry, distributed locks), and **lightweight real-time fan-out** (Pub/Sub for things like "typing..." indicators or live viewer counts, where losing a message occasionally is fine). Being able to say precisely *why* you would or wouldn't reach for Redis Pub/Sub versus Kafka for a given requirement — durability, ordering, replay, consumer independence — is a stronger answer than knowing the API surface of either.

## 10. Real-world production usage

Session storage, API response caching, leaderboards (via sorted sets — `ZADD`/`ZRANGE`), rate limiting, distributed locks (`SET ... NX EX`), and lightweight pub/sub for real-time UI features at companies that don't need Kafka-grade durability for that particular signal. Almost every large-scale web backend has a Redis (or Redis-compatible, e.g. AWS ElastiCache/Valkey) instance somewhere in the request path.
