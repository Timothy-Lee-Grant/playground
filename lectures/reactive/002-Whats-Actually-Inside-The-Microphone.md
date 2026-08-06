2026_08_05_22_29-Whats-Actually-Inside-The-Microphone

# What's Actually Inside The Microphone
### `IObserver<T>`, `IObservable<T>`, and `Subject<T>` from the source up — plus the organizational problem hiding in your new attempt

---

## 0. The elephant in the room, first

Before the abstractions: you split the exercise into **two separate projects with two separate `Program.cs` / `Host.CreateApplicationBuilder` calls** — `reactive/` and `listener1/`. Each of those is a **separate OS process** when you run it. That single fact invalidates the approach in both files right now, independent of any syntax bugs, so it needs to be said plainly before we go further:

> **A delegate, an `event`, `IObservable<T>`, and `Subject<T>` all live in RAM, inside one process. `+=` on an event is a pointer being added to a list sitting in that process's memory. There is no version of `_stateService.OnEventTrigger += myTriggerFunction` that reaches across two `dotnet run` processes — not because you're missing a using statement, but because `listener1.exe` and `reactive.exe` do not share an address space at all.**

```
 Process: reactive.exe                    Process: listener1.exe
┌─────────────────────────┐              ┌─────────────────────────┐
│  StateService instance   │              │  Listener instance       │
│  lives at, say,           │   ✗ NO PATH  │  wants a reference to    │
│  0x00007ff8_1a2b3c40      │◄────────────►│  that exact object       │
└─────────────────────────┘   between      └─────────────────────────┘
                              processes
```

This is actually the single most important distributed-systems lesson sitting inside this exercise, and it's worth naming explicitly because it connects straight to your stated learning goals: **everything in this lecture (`event`, `IObservable<T>`, `Subject<T>`, Rx.NET) is for *in-process* reactive streams.** The moment you want two separate executables to talk, you've left "reactive programming" and entered "distributed systems" — you need an actual transport (HTTP call, TCP socket, gRPC stream, or a broker like Redis Pub/Sub — which, notably, you already have an empty `redis/` folder sitting in this same repo for). Different problem, different tools, and conflating them is exactly why `listener1` can't work as written — `StateService` isn't even a type `listener1` can see; there's no project reference, and even if there were, a reference only shares the *type*, never the *running instance*.

**So: for this lecture, we fix the organization back to one process** (merge `listener1`'s logic back into `reactive`, or keep it as a second class in the same host — either way, same executable) so you can actually observe `IObservable`/`Subject` working, correctly, before you reach for cross-process transport in a later exercise. That's not a step backward — it's sequencing: understand the mechanism in one room before you try to run wires between two buildings.

---

## 1. The two interfaces — this is the *entire* contract

Everything Rx-flavored in .NET is built on two interfaces that live in the base `System` namespace (no package needed — they've shipped since .NET Framework 4.0, in `mscorlib`/`System.Private.CoreLib`). Here they are, complete, not simplified:

```csharp
namespace System
{
    public interface IObserver<in T>
    {
        void OnNext(T value);
        void OnError(Exception error);
        void OnCompleted();
    }

    public interface IObservable<out T>
    {
        IDisposable Subscribe(IObserver<T> observer);
    }
}
```

That's it. That's the whole contract. No magic, no hidden base class. Two interfaces, four methods total. Let's give each method a job title, since you like naming the characters:

| Member | Character | What it actually means |
|---|---|---|
| `IObservable<T>.Subscribe(observer)` | **The Sign-Up Sheet** | "Here's an observer. Add them to your notification list. Hand me back a receipt so I can leave later." |
| `IObserver<T>.OnNext(value)` | **The Doorbell** | "A new value exists. Here it is." Called once per event/value. |
| `IObserver<T>.OnError(ex)` | **The Fire Alarm** | "Something went wrong, and the stream is now *dead* — no more `OnNext` will ever come." Terminal. |
| `IObserver<T>.OnCompleted()` | **The Closing Bell** | "The stream ended normally — no more values, no error." Also terminal. |
| `Subscribe`'s return value (`IDisposable`) | **The Unsubscribe Ticket** | Calling `.Dispose()` on it is how you leave the list. This is the answer to last lecture's `something SubscribeToMyEvents()` placeholder — the "something" is `IDisposable`. |

Notice: `IObserver<T>` has **three** notification methods, not one. Your `Publish()` sketch and the `event`-based version both only ever handle the "here's a value" case (`OnNext`'s job). They have no concept of "the stream is over" or "the stream errored" — that's a real capability gap plain `event`/`Action<T>` has compared to `IObservable<T>`, and it's the first concrete reason Rx.NET exists rather than everyone just using `event` forever: **`IObservable<T>` gives every stream a defined *lifecycle* (values → then either an error or a clean completion), not just an infinite stream of values with no way to say "done."**

---

## 2. Where `Subject<T>` comes from, and what it does — no magic, let's build it

`Subject<T>` lives in the `System.Reactive` NuGet package, namespace `System.Reactive.Subjects`. It is a completely ordinary C# class. It is **not** a language feature, **not** compiler magic — it's a class someone at Microsoft wrote using exactly the tools you have. Here is a faithful, simplified reconstruction — not the real (heavily optimized, thread-safe, allocation-tuned) source, but mechanically equivalent, so you can see precisely what `.OnNext()` and `.Subscribe()` are actually doing:

```csharp
// This is a teaching reconstruction — NOT the real Subject<T> source,
// but it does the same job with the same two interfaces.
public class MiniSubject<T> : IObservable<T>, IObserver<T>
{
    private readonly List<IObserver<T>> _observers = new();

    // --- IObservable<T> half: people subscribe to ME ---
    public IDisposable Subscribe(IObserver<T> observer)
    {
        _observers.Add(observer);
        return new Unsubscriber(_observers, observer);
    }

    // --- IObserver<T> half: I AM an observer too, so anyone can feed ME values ---
    public void OnNext(T value)
    {
        foreach (var observer in _observers.ToArray())   // copy: safe to mutate list mid-notify
            observer.OnNext(value);
    }

    public void OnError(Exception error)
    {
        foreach (var observer in _observers.ToArray())
            observer.OnError(error);
    }

    public void OnCompleted()
    {
        foreach (var observer in _observers.ToArray())
            observer.OnCompleted();
    }

    private class Unsubscriber(List<IObserver<T>> observers, IObserver<T> me) : IDisposable
    {
        public void Dispose() => observers.Remove(me);
    }
}
```

Read that `OnNext` method again: **it is your `Dictionary<string, List<Action>>` instinct from lecture 001, done properly.** `_observers` *is* your subscriber registry. The `foreach` loop *is* the "notify everyone who signed up" step you were reasoning toward in your very first comment: *"I need a way to send out those events to all of the listening (subscribed) callback."* You had the right idea from day one — `Subject<T>` is that idea, written once, correctly, thread-safely, and shipped so 40 years of .NET developers never have to write it again.

This also directly answers "where is `.OnNext()` coming from" — it's not coming from anywhere exotic. It's an ordinary instance method on an ordinary class, exactly like `.Publish()` in your own `StateService`. The only difference is *who wrote the class* and *how battle-tested it is*.

### What the real `Subject<T>` adds on top of `MiniSubject<T>`

- A `lock` (or lock-free structure) around the observer list, so `Subscribe`/`OnNext` from different threads don't corrupt each other — this is the exact race condition flagged in lecture 001 §5, solved for you.
- Once `OnError`/`OnCompleted` has fired, it remembers that and immediately replays it to any *new* subscriber who joins late, rather than silently going quiet.
- It throws `ObjectDisposedException` if you call `OnNext` after disposing the subject — a defined lifecycle instead of undefined behavior.

---

## 3. Two ways to become a subscriber — and why your code never needed a `Listener : IObserver<T>` class

You don't have to write a class that implements `IObserver<T>` to subscribe — that's the *formal* way, but Rx.NET ships extension methods on `IObservable<T>` that build an `IObserver<T>` for you out of plain lambdas:

```csharp
// Formal way: implement the interface yourself
public class ConsoleObserver : IObserver<string>
{
    public void OnNext(string value) => Console.WriteLine($"got: {value}");
    public void OnError(Exception ex) => Console.WriteLine($"error: {ex.Message}");
    public void OnCompleted() => Console.WriteLine("done");
}
subject.Subscribe(new ConsoleObserver());

// Idiomatic way: Rx.NET's Subscribe() overloads build the IObserver<T> for you
subject.Subscribe(
    onNext: value => Console.WriteLine($"got: {value}"),
    onError: ex => Console.WriteLine($"error: {ex.Message}"),
    onCompleted: () => Console.WriteLine("done"));

// Minimal, common case: only care about values
subject.Subscribe(value => Console.WriteLine($"got: {value}"));
```

All three do the same thing under the hood: something is created that implements `IObserver<T>`, and it's handed to `Subscribe`. The lambda overloads exist purely for convenience — this is a very common C# pattern (an interface with an "anonymous implementation via delegates" convenience wrapper) that shows up constantly once you notice it.

Your `listener1/Listener.cs:24` — `_stateService.OnEventTrigger += myTriggerFunction;` — is doing the `event`-flavored equivalent of exactly this: `myTriggerFunction` is a plain method being wrapped into a delegate and added to an invocation list. Same idea, different (older, simpler, lifecycle-less) mechanism.

---

## 4. `Publish()` is a trap word — two unrelated meanings collide

This deserves its own section because it will bite you later if it isn't named now. You wrote a method called `Publish()` on `StateService` to mean "notify my subscribers." That's a perfectly reasonable name for *your own* method. But **Rx.NET also has an operator literally named `.Publish()`**, and it means something completely different:

| `.Publish()` | What it means |
|---|---|
| **Your `StateService.Publish()`** | A method you invented: "fire the event now." |
| **Rx.NET's `IObservable<T>.Publish()`** | An *operator* that converts a "cold" observable (one that starts producing values fresh for each subscriber, like re-running a query per subscriber) into a "hot"/`ConnectableObservable<T>` (one underlying source, shared and multicast to every subscriber, that only starts running once you call `.Connect()`) |

You will see `.Publish()` in Rx.NET tutorials meaning the second thing, and if you still associate the word with your own first meaning, it will actively confuse you. Cold-vs-hot observables are a real, slightly advanced Rx concept (should you go implement full Rx.NET operator chains) — flagging it now so future-you isn't blindsided, but it's not something to chase down right now.

---

## 5. Fixing what's actually broken, line by line

### `Worker.cs` — two constructors fighting each other

```csharp
public class Worker(ILogger<Worker> logger) : BackgroundService     // primary constructor #1
{
    Worker(StateService stateService)                                // constructor #2
    {
        var _stateService = stateService;                            // local variable, NOT a field!
    }
    ...
    _stateService.Publish();   // ❌ doesn't exist here — out of scope, and this ctor never even runs
```

Two separate problems stacked on top of each other:

1. **A class with a primary constructor (`Worker(ILogger<Worker> logger)`) can still declare other constructors, but every other constructor *must* chain to the primary one with `: this(...)`.** Constructor #2 doesn't do that, so this does not compile.
2. Even if it did compile: `var _stateService = stateService;` inside a constructor body declares a **local variable** scoped to that constructor call. It vanishes the instant the constructor returns. It is not a field. `ExecuteAsync` later referencing `_stateService` is referencing something that was never declared at class scope.

The fix — fold both dependencies into the *one* primary constructor, and let C# create the field for you (primary-constructor parameters are automatically captured and usable anywhere in the class body):

```csharp
public class Worker(ILogger<Worker> logger, StateService stateService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            stateService.Publish($"tick at {DateTimeOffset.Now}");
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

No separate constructor, no local-vs-field confusion — one parameter list, both dependencies, DI (`AddSingleton<StateService>()` in `Program.cs:6`, which you already correctly added) supplies both.

### `StateService.cs` — a delegate *type* is not a delegate *instance*

```csharp
public delegate void OnEventTrigger(string message);   // declares a TYPE named OnEventTrigger

private void Publish()
{
    OnEventTrigger?.Invoke();    // ❌ OnEventTrigger is a type name here, not a variable
}
```

`public delegate void OnEventTrigger(string message);` is exactly as much a "thing you can call" as `public class Foo { }` is — it defines a *shape*, not a *value*. It's the C# equivalent of declaring `typedef void (*OnEventTrigger)(char*);` in C — you still need an actual variable of that function-pointer type before you can call anything. The declaration and the instance are two separate lines:

```csharp
public class StateService
{
    public delegate void OnEventTrigger(string message);   // the TYPE (the shape of a callback)

    public event OnEventTrigger? Triggered;                 // an actual INSTANCE of that type, as a field

    public void Publish(string message) => Triggered?.Invoke(message);
}
```

(In practice nobody hand-declares a custom delegate type like `OnEventTrigger` for a single-string-argument callback — `Action<string>` already is that shape. `public event Action<string>? Triggered;` is the idiomatic version. Custom delegate types are worth knowing exist, but reach for `Action<T>`/`Func<T,R>` first.)

### `listener1/Program.cs` — a comment swallowed by a `using`

```csharp
using // Path to the other file so I can do dependency injection
```

A `using` directive needs a namespace immediately after it, on the same statement, ending in `;`. Writing a comment where the namespace should go leaves the statement unterminated — this is a hard syntax error, the compiler can't get past line 2. Once `listener1` is folded back into the same project/process as `reactive` (per §0), this whole file mostly disappears — you won't need a second `Program.cs`/host at all for now.

---

## 6. Setting it up for real — the actual commands

This is the part you asked for explicitly: how to *get* `Subject<T>` into your fingers, mechanically.

```bash
# from reactive/reactive/ (next to reactive.csproj)
dotnet add package System.Reactive
```

That adds a line to `reactive.csproj` looking like:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Hosting" Version="11.0.0-preview.5.26302.115" />
  <PackageReference Include="System.Reactive" Version="6.x.x" />
</ItemGroup>
```

Then the `using` you need in any file that uses `Subject<T>`:

```csharp
using System.Reactive.Subjects;   // Subject<T>, BehaviorSubject<T>, ReplaySubject<T>
```

And the rewritten `StateService`, now genuinely observable:

```csharp
public class StateService
{
    private readonly Subject<string> _events = new();

    public IObservable<string> Events => _events;   // exposed as read-only IObservable —
                                                       // callers can Subscribe, but the compile-time
                                                       // type hides OnNext/OnError/OnCompleted from them
    public void Publish(string message) => _events.OnNext(message);
}
```

That `public IObservable<string> Events => _events;` line is worth sitting with: `_events` (the field) is a `Subject<string>`, which implements *both* `IObservable<string>` and `IObserver<string>` — meaning it has `OnNext`/`OnError`/`OnCompleted` *and* `Subscribe`. But the **property**'s declared type is only `IObservable<string>`. C# lets you expose a more-capable object through a less-capable interface reference on purpose — outside code holding `stateService.Events` literally cannot call `.OnNext()` on it, because the compiler only knows about the `IObservable<string>` half. This is the modern, idiomatic version of exactly the encapsulation rule the `event` keyword was enforcing back in lecture 001 — "only `StateService` may publish" — except now it's enforced by *interface segregation* rather than a language keyword.

And subscribing, from wherever:

```csharp
IDisposable subscription = stateService.Events.Subscribe(msg => Console.WriteLine($"got: {msg}"));

// later, to stop listening:
subscription.Dispose();
```

---

## 7. Recommended shape of the project, right now

| Do | Don't (yet) |
|---|---|
| Keep `Worker` and `StateService` in one project (`reactive/reactive/`) | Split into a second `dotnet run`-able process for pub/sub — no transport exists yet to bridge them |
| Add a second **class** (not a second **process**) implementing the "listener" role, registered as another hosted service or a plain singleton in the *same* `Program.cs` | Keep `listener1/` as a separate project expecting to share `StateService`'s instance — it structurally cannot |
| Get one publisher → one in-process subscriber working with `Subject<T>` first | Reach for Rx operators (`.Where`, `.Throttle`) before the plain subscribe/notify loop is solid |
| When ready for real cross-process pub/sub, treat it as a *new, separate exercise* — pick a transport deliberately (Redis Pub/Sub is sitting right there in `redis/`, or gRPC streaming, or a raw `TcpListener`) | Silently assume `event`/`IObservable` will "just work" once you add a network call somewhere — the object model changes entirely once a wire is involved (serialization, connection lifecycle, reconnect/backoff) |

If you want `listener1` to exist as a *separate learning exercise in cross-process notification*, that's a great next project — just go into it knowing it's a different problem domain (see §0), not a continuation of the `IObservable<T>` material.

---

## 8. Common mistakes at this stage

- **Confusing a delegate *type* declaration with a delegate *instance*/field** — `public delegate void Foo(...)` declares a shape, not a value; you still need `public event Foo? bar;` or similar before there's anything to invoke.
- **Local variable vs. field** — assigning a constructor parameter to a `var` inside the constructor body throws the value away the moment the constructor returns. Fields need `private readonly StateService _stateService;` declared at class scope, or (cleaner) use a primary constructor and just reference the captured parameter directly.
- **Mixing a primary constructor with a secondary one that doesn't chain to it** — pick one constructor and put every dependency in it, unless you have a real reason for constructor overloads.
- **Assuming shared memory across `dotnet run` processes** — the single most expensive assumption to carry forward uncorrected, because it will make every future "why isn't my subscriber getting notified" debugging session look like a code bug when it's actually an architecture bug.
- **Overloading a word Rx.NET already uses for something else** (`Publish`) — not wrong, just worth knowing before it causes confusion reading real Rx.NET docs later.

## 9. Interview relevance

Being able to draw the four-method table in §1 from memory, and say "`Subject<T>` is just a class implementing both interfaces with an internal observer list — here's roughly what `OnNext` does" is a strong signal in an interview: it shows you understand Rx as *ordinary object-oriented code with a name*, not as framework magic. The in-process-vs-cross-process distinction in §0 is an even stronger signal — it's the kind of thing that separates "has used a pub/sub library" from "understands what a pub/sub library is a substitute for," and interviewers asking distributed-systems-flavored questions are specifically listening for that.

## 10. Next steps, concretely

1. Delete/ignore `listener1` as a separate running project for now; add its logic as a second class inside `reactive/reactive/`, registered as another hosted service (or a plain `Subscribe` call in `Worker` for a first pass — even simpler).
2. Fix `Worker.cs` and `StateService.cs` per §5, using plain `event Action<string>` first — get one publish, one in-process subscribe, working and printing to console.
3. Swap to `Subject<string>` per §6, confirm identical behavior — the point of this step is proving to yourself it's a drop-in upgrade, not new magic.
4. Add a second in-process subscriber to prove fan-out (multiple `.Subscribe()` calls on the same `IObservable<string>`).
5. Only after that's solid: decide, as a deliberate new exercise, what real transport you want to learn for cross-process notification — Redis Pub/Sub is the most natural next step given `redis/` is already in this repo.
