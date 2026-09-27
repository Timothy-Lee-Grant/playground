2026_08_05_22_41-The-Archivist-Setting-Up-Kafka-In-CSharp

# The Archivist
### Getting a real Kafka broker talking to a real C# process — from Docker to consumer groups

---

## 0. Read this after the Redis lecture, not before

This lecture assumes you've done (or at least read) `lectures/redis/001-The-Fast-Librarian-Setting-Up-Redis-In-CSharp.md`. The reason: Kafka is easiest to understand as **the answer to the exact limitation Redis Pub/Sub has on purpose** — "if nobody's listening when I publish, the message is gone forever." Kafka's entire design is built around removing that limitation, at the cost of being a meaningfully heavier piece of infrastructure. Holding both in your head at once, contrasted, is how this actually sticks.

---

## 1. What Kafka actually is, in one sentence

**Kafka is a durable, ordered, append-only log, split into partitions, that many independent readers can each replay from wherever they left off.** That sentence has four load-bearing words — *durable*, *ordered*, *partitions*, *independent readers* — and each one is doing real work distinguishing Kafka from Redis Pub/Sub:

| Property | Redis Pub/Sub | Kafka |
|---|---|---|
| **Durability** | None — unreceived messages vanish | Messages are written to disk and retained (by time or size policy), whether or not anyone's reading |
| **Replay** | Impossible | Any consumer can rewind to any offset and re-read history |
| **Ordering** | No ordering guarantee across subscribers | Strict order guaranteed *within a partition* |
| **Multiple independent readers** | Every subscriber gets every message, no memory of position | Each **consumer group** tracks its own read position (offset) independently — one slow reader doesn't affect another |
| **Setup weight** | One container, instant | A broker process (or several), topic/partition configuration, a heavier client library |

**The character:** if Redis's Pub/Sub is The Fast Librarian's megaphone (heard only by whoever's in the room, never repeated), Kafka is **The Archivist** — she writes every single message into a permanent, numbered ledger the instant it arrives, and she'll happily hand any reader the ledger starting from page 1, page 4000, or "whatever page you last told me you stopped at." She never shouts anything directly at anyone; readers come to her and ask for pages.

---

## 2. The vocabulary you need before any code makes sense

```
                 Topic: "ticks"
        ┌─────────────────────────────────────────┐
        │  Partition 0:  [0][1][2][3][4][5]────▶   │  ← append-only, offsets 0,1,2,3...
        │  Partition 1:  [0][1][2][3]──────▶       │  ← a SEPARATE ordered log
        │  Partition 2:  [0][1][2][3][4]────▶      │
        └─────────────────────────────────────────┘
              ▲                          │
              │ produce                  │ consume (each group tracks its OWN offset)
        ┌───────────┐            ┌───────────────────┐    ┌───────────────────┐
        │ Producer   │            │ Consumer Group "A"  │    │ Consumer Group "B"  │
        │ (your      │            │ reading from        │    │ reading from        │
        │  Worker)   │            │ offset 3            │    │ offset 5            │
        └───────────┘            └───────────────────┘    └───────────────────┘
```

| Term | What it is |
|---|---|
| **Broker** | One Kafka server process. Production clusters run several; your dev setup runs one. |
| **Topic** | A named stream, e.g. `"ticks"` — roughly Kafka's version of a Redis "channel," but durable. |
| **Partition** | A topic is split into 1+ partitions, each its own independent, strictly-ordered append-only log. Ordering is only guaranteed *within* a partition, never across partitions of the same topic. |
| **Offset** | A message's position within its partition — an integer bookmark. This is what makes replay possible: "give me everything from offset 42 onward." |
| **Producer** | A client that appends messages to a topic (optionally targeting a partition via a message key). |
| **Consumer** | A client that reads a topic starting from some offset. |
| **Consumer Group** | A named set of consumers that share the work of reading a topic — Kafka tracks one committed offset *per group, per partition*, so two different groups can read the same topic completely independently, each at its own pace, without affecting each other. |

That last row is the concept most worth sitting with: in Redis Pub/Sub, "one message, many subscribers" just means *everyone connected gets a copy, live, no memory*. In Kafka, "one topic, many consumer groups" means *each group has its own independent, durable bookmark and can be anywhere in the log at any time* — a subscriber that's five minutes behind doesn't lose anything, and doesn't affect anyone else's read position.

---

## 3. Getting a broker running — pick your setup deliberately

Real production Kafka (multiple brokers, replication) is a heavier lift than this exercise needs. You have two reasonable options; pick one on purpose rather than by accident:

| Option | What it is | Tradeoff |
|---|---|---|
| **Real Apache Kafka, single node, KRaft mode** | The actual Kafka broker, just one instance, using Kafka's modern built-in consensus (KRaft) instead of the old separate ZooKeeper process | More "authentic" to production Kafka; slightly heavier container, JVM startup time |
| **Redpanda** (or similar Kafka-API-compatible engine) | A from-scratch, single-binary broker that speaks the Kafka wire protocol | Much lighter/faster to start, simpler Docker setup; you're technically not running "real" Kafka internals, but for *learning the client API and the concepts in §2*, the wire protocol is what matters, and it's identical |

Either is legitimate for "learn to integrate the technology" — Redpanda is a very reasonable pragmatic choice specifically because your stated goal is learning the integration, not running production infrastructure. If part of the goal is also "get comfortable with the actual Kafka ecosystem's quirks" (KRaft config, topic tooling, etc.), pick real Kafka.

> **Flagging honestly:** exact Docker image tags and startup flags/env-vars for both Kafka's KRaft mode and Redpanda change fairly often between versions. Treat the snippets below as *shape*, not copy-paste-guaranteed — check the image's current Docker Hub page/quickstart for the exact env-vars at the time you run this.

A representative single-node KRaft `docker-compose.yml` (illustrative — verify current env-var names against the `apache/kafka` image docs):

```yaml
services:
  kafka:
    image: apache/kafka:latest
    ports:
      - "9092:9092"
    environment:
      KAFKA_NODE_ID: 1
      KAFKA_PROCESS_ROLES: broker,controller
      KAFKA_LISTENERS: PLAINTEXT://:9092,CONTROLLER://:9093
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_CONTROLLER_QUORUM_VOTERS: 1@localhost:9093
      KAFKA_CONTROLLER_LISTENER_NAMES: CONTROLLER
```

Or, the Redpanda equivalent (also illustrative — check current quickstart):

```yaml
services:
  redpanda:
    image: redpandadata/redpanda:latest
    ports:
      - "9092:9092"
    command:
      - redpanda start --smp 1 --memory 1G --overprovisioned --node-id 0 --check=false
```

Once it's up, create a topic and prove the broker works *before* writing C#, same discipline as the Redis lecture:

```bash
# using Kafka's own CLI tooling (bundled in the apache/kafka image, or a separate kcat/rpk tool for Redpanda)
kafka-topics.sh --create --topic ticks --bootstrap-server localhost:9092 --partitions 3
kafka-topics.sh --list --bootstrap-server localhost:9092
```

---

## 4. The C# client: `Confluent.Kafka`

```bash
dotnet add package Confluent.Kafka
```

This is the official, most widely used .NET client — a thin wrapper around `librdkafka`, the battle-tested C library most Kafka clients across languages are built on (not a from-scratch .NET reimplementation of the protocol).

### Producer — and the parallel to the Redis multiplexer

```csharp
using Confluent.Kafka;

var config = new ProducerConfig { BootstrapServers = "localhost:9092" };
using var producer = new ProducerBuilder<Null, string>(config).Build();

await producer.ProduceAsync("ticks", new Message<Null, string>
{
    Value = $"tick at {DateTimeOffset.Now}"
});
```

Exactly like `ConnectionMultiplexer`, **a producer is meant to be created once and reused** — it maintains its own internal connection pool and batches messages internally for throughput. Creating a new `IProducer` per message throws away that batching and is the Kafka-world equivalent of the "new `ConnectionMultiplexer` per call" mistake from the Redis lecture. Register it as a singleton the same way.

The generic parameters `<Null, string>` are `<TKey, TValue>` — Kafka messages are always a key/value pair, even when you don't need a key (`Null`). The key matters more than it looks: **Kafka guarantees all messages with the same key land in the same partition**, which is how you get ordering guarantees for a specific entity (e.g., all events for `userId=42` processed in order) while still spreading unrelated messages across partitions for parallelism.

### Consumer — this is structurally your `Worker.ExecuteAsync` loop again

```csharp
var config = new ConsumerConfig
{
    BootstrapServers = "localhost:9092",
    GroupId = "tick-readers",                    // the consumer group (see §2)
    AutoOffsetReset = AutoOffsetReset.Earliest    // where to start if this group has never read before
};

using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
consumer.Subscribe("ticks");

while (!stoppingToken.IsCancellationRequested)
{
    var result = consumer.Consume(stoppingToken);   // blocks until a message arrives
    Console.WriteLine($"got: {result.Message.Value} (offset {result.Offset})");
}
```

Put this inside a `BackgroundService.ExecuteAsync` and it is, shape-for-shape, the same pattern as your original `Worker` — a long-running loop owned by the host, polling for the next thing to do. That's a genuinely useful thing to notice: **the "background service polling a source" pattern doesn't change based on the transport underneath it.** What changes is what `Consume()` is actually talking to over the wire, and what guarantees it gives you.

---

## 5. `GroupId` is the single most important line in that config

If you omit `GroupId`, or give two unrelated consumers the same `GroupId` when you meant them to each see everything independently, you'll get confusing results:

- **Same `GroupId` on multiple consumer instances** → Kafka treats them as *one logical group sharing the work* — each partition is read by only one consumer in that group at a time (this is how Kafka gets parallelism: more partitions + more consumers in a group = more throughput, up to one consumer per partition).
- **Different `GroupId`s** → each group gets its own independent full copy of the stream, each with its own offset bookmark — this is the "two independent readers" row from the §2 table.

For this learning exercise: start with **one consumer, one group**, prove messages arrive in order. Then, as a stretch goal, run **two consumers with the same `GroupId`** against a 3-partition topic and watch Kafka split the partitions between them (this is "consumer group rebalancing," a real production concept — worth seeing happen once, even in a toy setup).

---

## 6. Commit semantics — the part that actually matters in production

By default, `Confluent.Kafka`'s consumer auto-commits its offset periodically. That interacts with *when* you consider a message "done," and it's worth understanding the tradeoff explicitly rather than accepting the default blindly:

| Strategy | What happens on a crash mid-processing | Delivery guarantee |
|---|---|---|
| **Auto-commit, commit happens on a timer regardless of processing** | Offset may already be committed for a message you hadn't actually finished handling | Message can be **lost** (never reprocessed) |
| **Manual commit, called *after* you finish processing the message** | If you crash before committing, the message will be delivered again on restart | Message can be **duplicated**, never lost — "at-least-once" |
| **Exactly-once** | Requires transactional producers/consumers, real coordination | Possible in Kafka, but a genuinely advanced topic — not needed for this exercise |

"At-least-once, with your own logic tolerating occasional duplicates" (e.g., processing keyed by an idempotency key) is the realistic default almost everywhere in production. Knowing that Kafka defaults to *possible loss* unless you deliberately commit after processing is exactly the kind of detail that separates "used the client library" from "understands the guarantee it's actually giving you" — and it's a very common interview probe.

---

## 7. Suggested build order for this exercise

1. Bring up a broker (§3), confirm with the CLI (`kafka-topics.sh --list`) before writing any C#.
2. Create a topic with 3 partitions explicitly — don't accept Kafka's default of 1, or you'll never see partition behavior.
3. One producer console app: a loop identical in shape to your old `Worker`, publishing a tick every second with no key (`Message<Null, string>`).
4. One consumer console app (own `GroupId`), print everything, confirm ordering *within* the topic for this simple single-partition-effective case.
5. Stop the consumer, let the producer run for 10-15 seconds unattended, then restart the consumer with `AutoOffsetReset.Earliest` — confirm, directly, that **nothing was lost**, in contrast to what you saw in the Redis exercise's step 4. That side-by-side felt difference is the entire point of building both.
6. Stretch: add a message key (e.g., a fake `deviceId`), produce from a few different keys, and inspect which partition each key lands in (`result.Partition` when producing) — confirms the "same key → same partition" guarantee from §4.
7. Stretch: run two consumer instances with the same `GroupId` against your 3-partition topic, watch the partitions split between them via logging (`consumer.Consume()` results show which partition each message came from).

---

## 8. Common mistakes

- **A new `IProducer` per message** — same category of bug as a new `ConnectionMultiplexer` per Redis call; expensive and throws away internal batching.
- **Forgetting `GroupId`, or reusing one accidentally across unrelated consumers** — leads to "why is my second consumer not getting all the messages" (§5).
- **Leaving the topic at 1 partition** and then wondering why adding a second consumer to the group didn't add parallelism — with 1 partition, only one consumer in a group can ever be active at a time; the rest sit idle.
- **Assuming Kafka is "just a queue"** — it isn't; a queue typically removes a message once consumed, whereas Kafka retains messages (per its retention policy) regardless of consumption, and multiple independent consumer groups can each read the same data — much closer to a shared, replayable log than a work queue.
- **Blindly trusting default auto-commit timing** without understanding the loss/duplication tradeoff in §6.

---

## 9. Interview relevance

Kafka is one of the most commonly probed distributed-systems topics precisely because it exercises so many real concepts at once: partitioning and ordering guarantees, consumer group rebalancing, offset management and delivery semantics (at-most-once/at-least-once/exactly-once), and the durability-vs-latency tradeoff versus something like Redis Pub/Sub. Being able to explain *why* you'd reach for Kafka over Redis Pub/Sub for a given requirement — and name the specific guarantee (durability, replay, independent consumer pacing) that drove the choice — is a much stronger signal than reciting the producer/consumer API. This directly matches the "Kafka-style architectures" gap you already flagged as a distributed-systems weakness.

## 10. Real-world production usage

Event sourcing and audit logs (the durable-replayable-log property is exactly what those need), decoupling microservices (a service publishes an event once; any number of current *and future* consumers can read it independently), log/metrics aggregation pipelines, and change-data-capture (tools like Debezium stream database row changes into Kafka topics for other services to react to). Stream-processing layers like Kafka Streams or ksqlDB sit on top of exactly the partition/offset model in §2 to do continuous computation over these logs.
