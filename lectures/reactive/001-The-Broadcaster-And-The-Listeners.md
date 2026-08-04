2026_08_03_23_24-The-Broadcaster-And-The-Listeners

# The Broadcaster and The Listeners
### A review of your `reactive/` attempt, and the concepts underneath it

---

## 1. What you were trying to build

You scaffolded a .NET Worker Service (`reactive/reactive/`) with three pieces:

| File | Role you gave it | Current state |
|---|---|---|
| `Worker.cs` | A `BackgroundService` that ticks every second and should "emit an event" | Compiles, but only logs — no emission yet |
| `Services/StateService.cs` | "The owner of all state changes... exposes the event as an observable" | Does not compile — it's a thinking-out-loud sketch |
| `reactive.csproj` | Project file | References `Microsoft.Extensions.Hosting` only — **no Rx.NET package** |

Your own comments in `StateService.cs:1-12` are the best diagnostic I could ask for. You wrote:

> "So if I have my worker service which is 'doing events' and I am here in this file, then I guess I need a way to send out those events to all of the listening (subscribed) callback?"

and

> "I would guess that this would need to return back a function pointer. but I also know that c# does not like to do that"

That second line is the whole lecture in one sentence. You're reasoning in **C**, where "give the caller a way to be called back later" means "hand them a function pointer." That instinct is *correct* — it's exactly the right primitive to reach for. What's missing is the C# vocabulary for it, and that's fixable in about twenty minutes of concept-building. The bigger, more valuable gap is one you haven't hit yet: you're about to hand-build a pub/sub dictionary inside a project literally named `reactive`, without knowing that .NET already ships the exact abstraction you're reinventing — and that "Reactive Extensions" (Rx.NET) is a specific, real library one `dotnet add package` away, not just a folder name.

---

## 2. The character map

Since you like analogies with names and roles, here's the cast for this whole domain:

```
        ┌─────────────────────────────────────────────┐
        │                THE BROADCASTER               │
        │   (StateService — owns the only microphone)  │
        └───────────────────────┬───────────────────────┘
                                 │ "OnNext(event)"
                 ┌───────────────┼───────────────┐
                 ▼               ▼               ▼
          ┌───────────┐   ┌───────────┐   ┌───────────┐
          │ LISTENER A │   │ LISTENER B │   │ LISTENER C │
          │ (subscriber)│  │ (subscriber)│  │ (subscriber)│
          └───────────┘   └───────────┘   └───────────┘
```

- **The Broadcaster** never asks "who's listening?" before it speaks. It just says its line into the microphone. This is the whole point: *decoupling*. The Broadcaster doesn't hold references to concrete listener types, doesn't call their methods by name, doesn't know how many there are.
- **The Listeners** each hand the Broadcaster a slip of paper before the show starts — "when something happens, do *this*." That slip of paper is a **delegate**. Handing it over is **subscribing**.
- **The Worker** in your code is not the Broadcaster. It's more like a **stage manager** who notices things happening (a clock ticking) and *tells* the Broadcaster "say this now." Your comment in `Worker.cs:12-13` has the Worker and StateService relationship slightly backwards — worth fixing as you implement, see §7.

---

## 3. Concept #1 (the one you're actually missing): Delegates, not function pointers

In C, a callback is a raw address: `void (*callback)(int)`. You store the address, you call through it, the compiler trusts you completely and gives you zero safety.

C# has the same *idea* but wraps it in a type-safe object. This is a **delegate**.

```csharp
// A delegate type is a *type* — like declaring what shape of function you accept.
public delegate void EventHandlerish(string message);

// Two built-in generic delegate types cover 95% of real code, so you
// almost never declare your own:
Action<string> onMessage;        // a callback that takes 1 arg, returns void
Func<string, int> parseIt;       // a callback that takes 1 arg, returns int
```

A delegate variable isn't one function pointer — it's a **multicast** invocation list. `+=` appends a callback, `-=` removes one, and invoking the delegate calls every callback in the list, in order. That list *is* your "dictionary of subscribers" — the CLR already gives you a `List<Action<T>>` with `+=`/`-=` syntax sugar over `.Add()`/`.Remove()`. This is why your `_pubSub` dictionary in `StateService.cs:16` was the right shape of thought (a registry of callbacks) but the wrong tool (you don't need to hand-roll it — you need to know it already exists).

```csharp
Action<string> subscribers = null;
subscribers += msg => Console.WriteLine($"A heard: {msg}");
subscribers += msg => Console.WriteLine($"B heard: {msg}");
subscribers?.Invoke("tick");
// A heard: tick
// B heard: tick
```

### The `event` keyword: why it exists

If `subscribers` above were a public field, *anyone* holding a reference to your object could call `.Invoke()` on it — including code that should only be allowed to *listen*, not *broadcast*. That breaks your own rule from `StateService.cs:3-4`: *"No other entity is allowed to publish events. Other classes are only allowed to observe those published events."*

`event` is C#'s enforcement mechanism for exactly that rule:

```csharp
public class StateService
{
    public event Action<string> OnStateChanged;   // outsiders can += / -=, NOT invoke

    private void Publish(string message)
    {
        OnStateChanged?.Invoke(message);           // only StateService can invoke
    }
}
```

Outside the class, `someService.OnStateChanged("hi")` is a **compile error**. Outside the class, only `+=` and `-=` are legal. That's the language mechanically guaranteeing your design constraint, instead of you enforcing it by convention. This is the single most direct fix to what you were sketching.

---

## 4. Concept #2: The Observer Pattern — the 1994 idea underneath all of this

Delegates are the *mechanism*. The **Observer pattern** (Gang of Four, 1994) is the *design pattern* you're implementing with them: one Subject, many Observers, Subject notifies Observers of state changes without knowing their concrete types. Pub/sub, the `event` keyword, and Rx.NET are three different **implementations of the same idea** at increasing levels of sophistication:

| Layer | What it is | What it gives you | What it lacks |
|---|---|---|---|
| Hand-rolled `Dictionary<string, List<Action>>` | Your `StateService` sketch | Full control | You must write thread-safety, unsubscribe, error handling yourself |
| C# `event` + delegate | Language feature since C# 1.0 | Type safety, encapsulation, `+=`/`-=` | No composition — can't easily "only fire every 5th event" |
| `IObservable<T>` / `IObserver<T>` | BCL interfaces, `System` namespace, since .NET 4.0 | A **contract** every .NET dev recognizes; `Subscribe()` returns an `IDisposable` for clean unsubscribe | Still no operators — just the contract |
| **Rx.NET** (`System.Reactive` NuGet package) | The actual "Reactive Extensions" library | LINQ-style operators over *time*: `.Where()`, `.Select()`, `.Throttle()`, `.Buffer()`, `.CombineLatest()`, `.Merge()`, schedulers | A learning curve, and it's a real dependency you haven't added yet |

This is the gap worth naming plainly: **your project is named `reactive`, but `reactive.csproj:10-12` references only `Microsoft.Extensions.Hosting` — no `System.Reactive` package.** Nothing forces you to use Rx.NET (plain `event` is a perfectly legitimate, more common choice for this exact scenario in production ASP.NET/Worker code), but you should be making that choice *deliberately*, not by accident.

```
IObserver<T> / IObservable<T>          →  the interface contract (built in, free)
        │
        ▼
Subject<T> (from System.Reactive)      →  a concrete class that is BOTH an
                                           IObservable (others subscribe to it)
                                           AND an IObserver (you call .OnNext() on it)
        │
        ▼
Rx operators (.Where, .Throttle, ...)  →  compose new observables from existing ones
```

`Subject<T>` is the class that would make your `StateService` comment — "I need a way to send out those events to all of the listening subscribed callback" — a one-liner instead of a hand-built dictionary:

```csharp
private readonly Subject<string> _events = new();
public IObservable<string> Events => _events;   // read-only view: only StateService calls OnNext
public void Publish(string msg) => _events.OnNext(msg);
```

---

## 5. Concept #3: The bug that isn't visible yet — thread safety

This is the concept your persona notes flag as a standing weakness (async/race conditions), and it is *lying in wait* in this exact design. Walk through it:

1. `Worker.ExecuteAsync` runs on a background-service thread, looping every second.
2. Some other request-handling thread calls `StateService.Subscribe(callback)` — maybe an ASP.NET controller, maybe another hosted service starting up.
3. If "subscribe" means `_pubSub[key].Add(callback)` on a plain `List<T>`, and "publish" means iterating that same list to invoke callbacks — **and these two things can happen on different threads at the same time** — you have a data race. `List<T>` is explicitly documented as not thread-safe for concurrent read+mutate.

The C# `event` keyword's `+=`/`-=` operators are actually compiled to *replace the whole delegate field* with a new combined delegate (via `Delegate.Combine`), which is safer than mutating a `List<T>` in place, but still isn't fully race-free without care (a subscriber added mid-invoke may or may not get that invocation — that's usually acceptable, but you should know it's a "may" not a guarantee).

**What to actually do about it, in order of how much you should care:**
- Cheapest fix: guard the subscriber collection with a `lock` (uncontended locks are fast; this loop runs once a second, not once a microsecond — don't over-engineer here).
- More idiomatic: use `System.Collections.Concurrent.ConcurrentBag<T>` or `ConcurrentDictionary`.
- The "why does everyone reach for Rx/Channels instead of hand-rolled pub/sub in real systems" answer: `Subject<T>` and `System.Threading.Channels.Channel<T>` have *already solved this* for you, and been through years of production hardening you will not replicate correctly on the first attempt. This is a genuinely good instance of "use the abstraction you don't fully understand yet" — the exact skill your persona notes call out as something to build (see §8).

---

## 6. Concept #4: Worker vs. StateService — who's the Broadcaster, really?

One design question hiding in your comments: right now `Worker.ExecuteAsync` (`Worker.cs:5-17`) *is* the thing detecting "something happened" (a tick), but your intent per `StateService.cs:3-4` is that **StateService is the only entity allowed to publish**. So the Worker shouldn't hold its own broadcaster — it should be a *client* of StateService's broadcaster, more like this:

```
Worker (stage manager, notices the clock)
   │  calls
   ▼
StateService.Publish(tickEvent)     ← the ONLY place OnNext/Invoke happens
   │  notifies
   ▼
[all subscribers, whoever they are]
```

`Worker` gets `StateService` injected via constructor (you already have DI wired up in `Program.cs:5` via `AddHostedService`, so `AddSingleton<StateService>()` + constructor injection is a one-line addition), and calls `stateService.Publish(...)` instead of only logging. That keeps your original invariant intact: exactly one publisher, everyone else observes.

---

## 7. A worked fix, so the shape is concrete

```csharp
// StateService.cs
using System.Reactive.Subjects;   // needs: dotnet add package System.Reactive

public class StateService
{
    private readonly Subject<WorkerTick> _ticks = new();

    // Read-only surface: callers can Subscribe, cannot Publish.
    public IObservable<WorkerTick> Ticks => _ticks;

    public void Publish(WorkerTick tick) => _ticks.OnNext(tick);
}

public record WorkerTick(DateTimeOffset Time);
```

```csharp
// Worker.cs
public class Worker(ILogger<Worker> logger, StateService state) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var tick = new WorkerTick(DateTimeOffset.Now);
            logger.LogInformation("Worker running at: {time}", tick.Time);
            state.Publish(tick);              // <-- the emission you were reasoning about
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

```csharp
// Anyone else, anywhere, with StateService injected:
state.Ticks
     .Where(t => t.Time.Second % 5 == 0)      // Rx operator: only every 5th tick
     .Subscribe(t => logger.LogInformation("5-tick milestone: {t}", t.Time));
```

That last block is the payoff for adding the real Rx.NET package: filtering-by-time (`.Where`, `.Throttle`, `.Sample`) is exactly the kind of logic that's painful to hand-write correctly (timers, race conditions, cleanup) and is a one-liner once you're on `IObservable<T>`.

---

## 8. What was actually broken, line by line

Useful to see named explicitly, since some of it is C#-syntax-specific and worth just knowing cold:

| Location | Problem | Why |
|---|---|---|
| `StateService.cs:14` | `public class StateService : .` | Trailing `.` after `:` — inheritance/interface list left mid-thought |
| `StateService.cs:16` | `IDictionary<> _pubSub = new Dictionary<string, List<>>();` | C# generics require concrete type arguments at declaration time — `<>` with nothing inside is only legal in `typeof(Dictionary<,>)` or an *open generic type* context, never in a field declaration |
| `StateService.cs:31` | `public something SubscribeToMyEvents()` | `something` isn't a real type — this is a placeholder for "I don't know what type a subscription handle should be." (Answer: usually `IDisposable`, so the caller can `.Dispose()` to unsubscribe — that's exactly what `IObservable<T>.Subscribe()` returns.) |
| `reactive.csproj:10-12` | No `System.Reactive` package reference | The project is conceptually reaching for Rx.NET but hasn't installed it |

None of these are "you're bad at this" mistakes — they're exactly the shape of mistake you'd expect from someone translating C mental models into C# for the first time. The dictionary-of-lists instinct, the "give me back a function pointer" instinct, the "I need a registry" instinct — all correct systems-thinking. The gap is vocabulary, not judgment.

---

## 9. Common mistakes at this stage (broader than just your code)

- **Reinventing `List<Action<T>>` when `event` already gives you it** — very common first move, seen constantly in junior PRs.
- **Forgetting unsubscribe entirely** — every `Subscribe()` should have a matching way to stop listening (`IDisposable.Dispose()`, or `-=`), or you leak memory: subscribers keep dead objects alive because the publisher still holds a reference to their callback. This is one of the most common real memory leaks in long-running .NET services (event handlers on singletons holding references to short-lived objects).
- **Using `event`/`Action` when you actually want backpressure or async subscribers** — plain events are synchronous and fire-and-forget; if a subscriber's callback does slow I/O, it blocks the publisher's thread. Rx.NET schedulers and `Channel<T>` exist specifically to solve this.
- **Choosing Rx.NET for a one-publisher/one-subscriber scenario** — it's genuinely overkill sometimes; a plain `event` is fine when you don't need operators over time. Not every reactive-shaped problem needs Reactive Extensions.

---

## 10. Interview relevance

"Explain the Observer pattern and how it's implemented in .NET" is a real, common systems-design interview question, especially at companies doing event-driven backend work. Being able to say — cleanly — "the GoF Observer pattern maps to C#'s `event`/delegate for the simple case, and to `IObservable<T>`/Rx.NET when you need composition over time, and here's the tradeoff between them, and here's the thread-safety issue with the naive version" is a materially stronger answer than "I'd use pub/sub." This exercise, once you push it through to a working version, is a legitimate STAR-story candidate: "built a pub/sub layer by hand first to understand the mechanism, then reimplemented on Rx.NET/`IObservable<T>` once I understood what the abstraction was buying me" — that's a good story precisely because you did the hard version first.

## 11. Real-world production usage

- **`event`/delegates**: everywhere in .NET — UI frameworks (WinForms/WPF), `IHostApplicationLifetime.ApplicationStopping`, ASP.NET Core's own internal notification points.
- **`IObservable<T>` / Rx.NET**: heavily used in reactive UI frameworks (ReactiveUI), stream-processing pipelines, and anywhere you're composing streams of events with time-based operators (debounce a search box, throttle a sensor feed — relevant to your embedded background).
- **`System.Threading.Channels`**: the modern go-to for producer/consumer inside a single process when you want `async`/`await`-native backpressure without pulling in Rx.NET — worth a future lecture, since it's arguably the more "distributed-systems-shaped" primitive of the three (it's structurally a single-process analogue of a message queue, which lines up directly with your Kafka/pub-sub learning goals).

---

## 12. Suggested next steps, concretely

1. Fix the compile errors in `StateService.cs` using the `event`-based version in §7 first (no new package — build intuition with the built-in tool).
2. Once that works end-to-end (Worker publishes, something subscribes, log proves it fired), swap to `Subject<T>` + `dotnet add package System.Reactive`, and add one Rx operator (`.Where` or `.Throttle`) to feel the difference.
3. Add a second, independent subscriber to prove the "many listeners, one broadcaster" decoupling actually works — right now there's only ever been the idea of a subscriber, never a second real one to test fan-out.
4. When you're comfortable, look at `System.Threading.Channels` as the alternative to both — that's the one that will transfer most directly to Kafka-style thinking later.
