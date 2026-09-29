# Lecture 002: Delegates, Events, Callbacks and Threads (plus Extension Methods and `this`)

| | |
|---|---|
| **Prompted by** | [dotnet/iot#2403](https://github.com/dotnet/iot/issues/2403) (`GpioPin` handlers receive the wrong `sender`), and Timothy's questions after lecture 001 |
| **Date** | 2026-09-28 |
| **Upstream code quoted** | dotnet/iot `main` @ `1eb0b2f` (2026-09-24). Quotes are trimmed (XML doc comments removed); file paths are given so you can open the full source. |
| **Prerequisites** | [Lecture 001](001-the-big-picture-dotnet-iot.md) §3–§6 (the layers, and how a GPIO callback travels) |
| **Evidence status** | Code quotes are copied from the source. Statements about **runtime behavior** (which thread, what happens on an exception) come from reading the code and from how .NET works; none has been run in this repo yet. The "Try it" programs in §15 are there so you can confirm them yourself. |

---

## 0. How this lecture is built, and why

You asked a fair question: is it a good idea to learn a concept from code that is **broken** in exactly that concept?
Mostly, no. If the first example of "events" you study is #2403, you learn the wrong shape first and have to
un-learn it.

So this lecture is built the other way round:

1. **Learn every concept from dotnet/iot code that does it right.** The star example is the `Button` binding
   (`src/devices/Button/`). `ButtonBase` raises its events by the book, and `GpioButton` subscribes to GPIO callbacks
   and re-raises them correctly. Supporting examples come from the Arduino driver, the libgpiod v2 driver and the
   `Board` library.
2. **Then use #2403 as a "spot the difference" exercise** (§12). Once you know the right shape, a broken example
   becomes one of the best teachers there is: you can see *exactly* which rule it breaks and why the fix is subtle.

### What you'll be able to do after this lecture

- [ ] Explain a delegate using your C function-pointer knowledge, and say what a delegate carries that a C function pointer doesn't.
- [ ] Tell apart a delegate **type**, a delegate **instance**, and **invoking** it (the three things that got tangled in your `reactive/` attempt).
- [ ] Explain multicast delegates: `+=`, `-=`, the invocation list, and what happens if one handler throws.
- [ ] Explain why `-=` sometimes silently does nothing (delegate identity).
- [ ] Explain what the `event` keyword adds, and what custom `add`/`remove` accessors are.
- [ ] Read the sentence *"`sender` is the object that raised the event, which normally means the object you subscribed on"* and know exactly what it means.
- [ ] Explain the four meanings of `this` in C#, and how extension methods like `builder.Services.AddSingleton()` work.
- [ ] Say **which thread** runs your event handler for a given event source, and what that means for your code.
- [ ] Trace one real button press from the Linux kernel to your handler, naming every class and thread on the way.
- [ ] Explain precisely what's wrong in #2403 and why fixing it isn't a one-liner.

---

## 1. The problem events solve: "tell me when it happens"

### 1.1 Your polling loop, and what's wrong with it (and what isn't)

What you normally write:

```csharp
while (true)
{
    if (controller.Read(23) == PinValue.Low)    // has the button been pressed?
    {
        HandlePress();
    }
    Thread.Sleep(10);
}
```

This works, and in firmware it's often the right answer. Its costs show up when a program has to watch **many
things at once**: 20 pins, a network socket, a timer and a user command. Now you have to write one loop that checks
all of them, decide how often to check each, and keep adding checks as the program grows. Every piece of code that
"cares" about the button has to live inside that loop, or the loop has to know about it.

**Events flip it around.** Instead of *you* asking "did it happen yet?" over and over, you hand the thing a phone
number and it **calls you** when it happens:

```
 POLLING  (you own the loop)                    EVENTS  (the source owns the loop)
 ───────────────────────────                    ──────────────────────────────────
 you ──"pressed?"──► button   no                you ──"call me at OnPress"──► button   (once)
 you ──"pressed?"──► button   no                          ...time passes...
 you ──"pressed?"──► button   YES → HandlePress  button ──calls──► OnPress(...)
```

### 1.2 The loop didn't disappear. It moved.

This matters for your intuition: **somewhere, there is still a loop.** In dotnet/iot the libgpiod v2 driver starts a
background thread whose job is exactly the loop you would have written (`LibGpiodV2EventObserver.cs`):

```csharp
// src/System.Device.Gpio/System/Device/Gpio/Drivers/LibGpiodV2EventObserver.cs (trimmed)
private void HandleEdgeEventsOfRequestInLoop(LineRequest request)
{
    ...
    while (request.IsAlive && !_shouldExit)
    {
        int waitResult = request.WaitEdgeEventsRespectfully(WaitEdgeEventsTimeout);  // sleep in the kernel, up to 100 ms
        if (waitResult == 0) continue;                   // timed out: nothing happened, loop again
        ...
        int numberOfReadEvents = request.ReadEdgeEvents(edgeEventBuffer);
        for (int i = 0; i < numberOfReadEvents; i++)
        {
            HandleEdgeEvent(edgeEventBuffer.GetEvent((ulong)i));   // → calls everyone who subscribed
        }
    }
}
```

Two differences from your polling loop:

1. **It doesn't spin.** `WaitEdgeEventsRespectfully` blocks inside the kernel until an edge arrives (or 100 ms pass,
   so the loop can notice a shutdown request). It uses no CPU while waiting. This is the same idea as waiting on an
   interrupt instead of polling a register.
2. **It's written once, in the library**, and any number of subscribers plug into it. Your code no longer contains
   a loop at all; it only contains *reactions*.

You already know this idea under another name: **inversion of control.** You register your code with a framework,
and the framework calls you. `BackgroundService`, ASP.NET Core controllers and GPIO callbacks are all the same move.
Events are the smallest version of it.

### 1.3 The cast of characters

| Character | Real C# thing | Job |
|---|---|---|
| **The Publisher** | the class that owns the event (`ButtonBase`, a driver) | Knows when something happens. Keeps the list of who wants to hear about it. Makes the calls. |
| **The Subscriber** | your class, with a handler method | Hands over a "phone number" and does something when called. |
| **The Phone-Number Card** | a **delegate** instance | A small object holding *which method to call* and *on which object*. |
| **The Guest List** | the delegate's **invocation list** (multicast) | All the cards handed in, in order. |
| **The Doorman** | the **`event`** keyword and its `add`/`remove` accessors | Lets outsiders add or remove their own card. Only the Publisher may read the list or make the calls. |
| **The Message** | `EventArgs` subclass (`PinValueChangedEventArgs`) | What happened: which pin, which edge. |
| **The Return Address** | the `sender` parameter | *Who* is calling you. |
| **The Courier** | a **thread** | The one who actually walks down the guest list and makes each call. Whichever thread raises the event runs every handler. |

The rest of the lecture takes these one at a time.

---

## 2. Delegates: C function pointers, grown up

### 2.1 From C to C#

In C you'd declare a callback like this, and you've done exactly this with native libraries:

```c
// C: a function-pointer type, and a registration call with a user-data pointer
typedef void (*pin_callback_t)(int pin, int level, void *userdata);
int register_callback(int pin, pin_callback_t cb, void *userdata);
```

Notice the `void *userdata`. C callbacks need it because a function pointer is **just an address**. If your
callback needs to know *which object* it belongs to (which sensor, which connection), you have to smuggle that
pointer in separately and cast it back.

In C#, the equivalent of the `typedef` is a **delegate type**. Here is the one at the heart of #2403:

```csharp
// src/System.Device.Gpio/System/Device/Gpio/PinChangeEventHandler.cs
public delegate void PinChangeEventHandler(object sender, PinValueChangedEventArgs pinValueChangedEventArgs);
```

Read it as: *"`PinChangeEventHandler` is the type of any method that takes an `object` and a
`PinValueChangedEventArgs` and returns `void`."*

A **delegate instance** is the C# version of "function pointer + userdata", bundled into one safe object:

| | C function pointer | C# delegate instance |
|---|---|---|
| Which code to run | the address | `.Method` (a `MethodInfo`) |
| Which object it belongs to | you pass `void *userdata` by hand | `.Target`: the object is captured automatically (`null` for static methods) |
| Type safety | cast and hope | checked by the compiler |
| More than one target | you build a list yourself | built in (**multicast**, §3) |

So when `GpioButton` registers its method `PinStateChanged`, the delegate that gets created remembers *both*
"call `PinStateChanged`" *and* "on **this particular** `GpioButton` object". No `void*` needed.

### 2.2 The three things people mix up: type, instance, invocation

This is the knot your `reactive/` attempt got tied in (more in §11), so it's worth being very precise:

| # | Thing | What it is | C analogy | dotnet/iot example |
|---|---|---|---|---|
| 1 | **Delegate type** | A *declaration*: the shape of acceptable methods. Declares nothing to call. | `typedef void (*cb_t)(...)` | `public delegate void PinChangeEventHandler(object sender, PinValueChangedEventArgs e);` |
| 2 | **Delegate instance** (usually in a field or variable) | An object that points at real method(s). This is what you can call. | a `cb_t` variable holding an address | `PinChangeEventHandler? threadSafeCopy = OnPinChanged;` |
| 3 | **Invocation** | Actually calling the method(s) it points at. | `cb(pin, level, ud);` | `threadSafeCopy?.Invoke(PinNumber, new PinValueChangedEventArgs(...));` |

You can only `.Invoke()` #2, never #1. Declaring the type is like declaring a `struct`: it creates a *kind* of
thing, not a thing.

### 2.3 Three ways to make a delegate instance

```csharp
// (a) Method group: name an existing method. The compiler builds the delegate for you.
_gpioController.RegisterCallbackForPinValueChangedEvent(_buttonPin, PinEventTypes.Falling | PinEventTypes.Rising,
                                                        PinStateChanged);            // src/devices/Button/GpioButton.cs

// (b) Lambda expression: an inline, unnamed method.
controller.RegisterCallbackForPinValueChangedEvent(7, PinEventTypes.Rising | PinEventTypes.Falling, (o, e) =>
{
    wasCalled = true;
    callbackAsNo = e.PinNumber;
});                                                                                   // src/devices/Gpio/tests/VirtualGpioTests.cs

// (c) Local function (a named method inside a method), then passed as a method group.
void Callback(object o, PinValueChangedEventArgs e) { wasCalled = true; callbackAsNo = e.PinNumber; }
controller.RegisterCallbackForPinValueChangedEvent(7, PinEventTypes.Falling, Callback); // same test file
```

In (b), the lambda uses `wasCalled` and `callbackAsNo`, which are local variables of the test method. The lambda
**captures** them (a *closure*): the compiler moves those variables into a hidden object so the lambda can still
reach them when it runs later, on some other thread. It's the automatic version of stuffing locals into a
`userdata` struct.

---

## 3. Multicast delegates: one card, many phone numbers

### 3.1 `+=` builds a list

A delegate instance can point at **several** methods. `+=` returns a *new* delegate whose invocation list is the old
list plus the new entry. Calling it calls each entry **in order, one after another, on the calling thread**.

The Arduino GPIO driver keeps one multicast delegate per pin (`src/devices/Arduino/ArduinoGpioControllerDriver.cs`):

```csharp
private class CallbackContainer
{
    public event PinChangeEventHandler? OnPinChanged;     // the multicast delegate lives behind this event
    ...
    public bool NoEventsConnected => OnPinChanged == null;  // null means "nobody subscribed"

    public void FireOnPinChanged(PinEventTypes eventType)
    {
        // Copy event instance, prevents problems when elements are added or removed at the same time
        PinChangeEventHandler? threadSafeCopy = OnPinChanged;
        threadSafeCopy?.Invoke(PinNumber, new PinValueChangedEventArgs(eventType, PinNumber));
    }
}

protected override void AddCallbackForPinValueChangedEvent(int pinNumber, PinEventTypes eventTypes, PinChangeEventHandler callback)
{
    lock (_callbackContainersLock)
    {
        if (_callbackContainers.TryGetValue(pinNumber, out CallbackContainer? cb))
        {
            cb.EventTypes = cb.EventTypes | eventTypes;
            cb.OnPinChanged += callback;                   // add to the existing guest list
        }
        else
        {
            CallbackContainer cb2 = new CallbackContainer(pinNumber, eventTypes);
            cb2.OnPinChanged += callback;                  // first guest
            _callbackContainers.Add(pinNumber, cb2);
        }
    }
}
```

What this shows:

```
 OnPinChanged == null                         (nobody subscribed)
 OnPinChanged += A      →  [A]
 OnPinChanged += B      →  [A, B]
 OnPinChanged += A      →  [A, B, A]          (duplicates are allowed: A will run twice)
 OnPinChanged -= A      →  [A, B]             (removes the LAST matching A)
 OnPinChanged -= B      →  [A]
 OnPinChanged -= A      →  null               (empty list is represented as null, not an empty delegate)
 OnPinChanged.Invoke(…) →  calls A, then B, in that order, on the thread that called Invoke
```

(Ignore for now that Arduino passes `PinNumber`, a boxed `int`, as `sender`. That's one of the wrong senders from
#2403; §12.)

### 3.2 Delegates are immutable (and why that matters)

`+=` never modifies the existing delegate. It builds a **new** one and stores it back into the field. That's why the
"thread-safe copy" idiom above works:

```
 thread 1 (raising)                         thread 2 (subscribing)
 ──────────────────                         ──────────────────────
 copy = OnPinChanged      →  copy = [A, B]
                                            OnPinChanged += C   → field now points at NEW [A, B, C]
 copy.Invoke(...)         →  calls A, B     (copy still points at the old, unchanged [A, B])
```

Without the copy, the code would read the field twice ("is it null?" then "invoke it"), and another thread could set
it to `null` in between, giving a `NullReferenceException`. `threadSafeCopy?.Invoke(...)` reads the field once. The
shorter `OnPinChanged?.Invoke(...)` also reads it only once, so it's equally safe. The Arduino code just spells the
copy out, with a comment explaining why.

### 3.3 Two things that surprise people

| Behavior | What happens | Why it matters for GPIO |
|---|---|---|
| **An exception in one handler** | `Invoke` stops right there. Later handlers in the list **don't run**, and the exception flies back into whoever called `Invoke`. | The caller is usually a driver's background thread. §9.3 shows what that does in the libgpiod v2 driver. |
| **Return values** | For a non-`void` delegate, `Invoke` returns only the **last** handler's result. | This is why event handler delegates return `void`. |

### 3.4 Another way to hold many subscribers: a `List` of delegates

The libgpiod v2 driver doesn't use a multicast delegate at all. It keeps a plain list:

```csharp
// LibGpiodV2EventObserver.cs
private readonly Dictionary<EventSubscription, List<PinChangeEventHandler>> _handlersBySubscription = new();
...
private void CallEventHandlers(Offset offset, GpiodEdgeEventType edgeEventType, IEnumerable<PinChangeEventHandler> eventHandlers)
{
    foreach (PinChangeEventHandler eventHandler in eventHandlers)
    {
        eventHandler.Invoke(this, new PinValueChangedEventArgs(Translator.Translate(edgeEventType), (int)offset));
    }
}
```

Same outcome (everyone gets called in order), different storage. A list gives the driver more control (per-edge
filtering, counting, removing by rule), at the cost of doing the locking itself. Both designs are common. Recognizing
"this is just a list of callbacks" in any codebase is the skill.

---

## 4. Delegate identity: why `-=` sometimes does nothing

To remove a subscription, `-=` (or `List.Remove`) has to find an **equal** delegate in the list. Two delegates are
equal when they point at the **same method on the same target object**.

`GpioButton` gets this right. It subscribes in its constructor and unsubscribes in `Dispose`, both times writing the
method name:

```csharp
// src/devices/Button/GpioButton.cs
_gpioController.RegisterCallbackForPinValueChangedEvent(_buttonPin, PinEventTypes.Falling | PinEventTypes.Rising, PinStateChanged);
...
_gpioController.UnregisterCallbackForPinValueChangedEvent(_buttonPin, PinStateChanged);
```

Each mention of `PinStateChanged` creates a **new delegate object**, but both have Method = `PinStateChanged` and
Target = this button, so they compare equal and the removal works.

Lambdas behave differently:

```csharp
controller.RegisterCallbackForPinValueChangedEvent(7, PinEventTypes.Rising, (o, e) => Console.WriteLine("hi"));
controller.UnregisterCallbackForPinValueChangedEvent(7, (o, e) => Console.WriteLine("hi"));   // removes NOTHING
```

The two lambdas *look* identical, but the compiler turns each into its **own hidden method**. Different method →
not equal → nothing is removed, and no error is reported. The fix is to keep the delegate in a variable (or use a
named method) and pass the *same* one both times.

```
 Equal?               same Method?   same Target?   → removable
 ────────────────     ────────────   ────────────   ─────────────
 PinStateChanged ×2       yes            yes         yes  ✅
 two identical lambdas    no             —           no   ❌  (silently)
 same lambda, stored      yes            yes         yes  ✅
 in a variable
```

**Keep this in mind for #2403.** Any fix that wraps your handler in a new lambda has to remember that wrapper,
because your later `-=` will pass your *original* handler, which doesn't match the wrapper that was registered.

---

## 5. Events: a delegate with a doorman

### 5.1 What the `event` keyword adds

You could expose a public delegate field:

```csharp
public PinChangeEventHandler? OnPinChanged;       // a plain public field: dangerous
```

But then *anyone* could do things only the publisher should do:

| Outsider writes... | Plain public delegate field | `event` |
|---|---|---|
| `x.OnPinChanged += Mine;` | allowed | allowed |
| `x.OnPinChanged -= Mine;` | allowed | allowed |
| `x.OnPinChanged = Mine;` (wipes everyone else) | allowed 😬 | **compile error** |
| `x.OnPinChanged = null;` | allowed 😬 | **compile error** |
| `x.OnPinChanged.Invoke(...)` (fake an event) | allowed 😬 | **compile error** |

`event` is the doorman: from **outside** the class you may only add or remove *your own* card. Only the owning class
can look at the list, clear it, or make the calls. That enforces the rule you wrote in your `StateService` comment,
*"no other entity is allowed to publish events"*, in the compiler.

### 5.2 Field-like events (the normal case)

`ButtonBase` declares its events like this (`src/devices/Button/ButtonBase.cs`):

```csharp
public class ButtonBase : IDisposable
{
    public event EventHandler<EventArgs>? ButtonUp;
    public event EventHandler<EventArgs>? ButtonDown;
    public event EventHandler<EventArgs>? Press;
    public event EventHandler<EventArgs>? DoublePress;
    public event EventHandler<ButtonHoldingEventArgs>? Holding;
    ...
}
```

This is a **field-like event**. The compiler generates three things behind it:

```
 public event EventHandler<EventArgs>? ButtonDown;
                         │
                         ▼  (roughly what the compiler writes)
 private EventHandler<EventArgs>? ButtonDown;          ← hidden backing delegate field (the guest list)
 public void add_ButtonDown(EventHandler<EventArgs> v)    { /* thread-safe: field = field + v */ }
 public void remove_ButtonDown(EventHandler<EventArgs> v) { /* thread-safe: field = field - v */ }
```

Outside code's `button.ButtonDown += H` becomes a call to `add_ButtonDown(H)`. Inside `ButtonBase`, the name
`ButtonDown` refers to the hidden field, which is why `ButtonBase` can write `ButtonDown?.Invoke(...)`. The generated
`add`/`remove` are thread-safe (they use an atomic compare-and-swap loop), so subscribing from different threads
can't lose a subscriber.

### 5.3 Custom accessors: an event with no storage of its own

Sometimes a class doesn't want to *store* the subscribers. It wants to pass them on to somebody else. Then you write
the accessors yourself, like a property's `get`/`set` but named `add`/`remove`. `GpioPin` does exactly this:

```csharp
// src/System.Device.Gpio/System/Device/Gpio/GpioPin.cs
public virtual event PinChangeEventHandler ValueChanged
{
    add
    {
        _controller.RegisterCallbackForPinValueChangedEvent(_pinNumber, PinEventTypes.Falling | PinEventTypes.Rising, value);
    }

    remove
    {
        _controller.UnregisterCallbackForPinValueChangedEvent(_pinNumber, value);
    }
}
```

- `value` is the delegate the subscriber handed in (same keyword as in a property setter).
- There's **no backing field**. The pin keeps no guest list; it forwards the card straight to the controller.
- So `pin.ValueChanged += H` really means "controller, please call H when pin N changes".

Hold on to this: **`GpioPin` has no list, so it never makes the call itself.** That single fact is the root of
#2403 (§12).

---

## 6. The `(sender, e)` convention, and what that sentence means

### 6.1 The standard event shape

.NET has a standard shape for event handlers:

```csharp
public delegate void EventHandler<TEventArgs>(object? sender, TEventArgs e);
```

- **`sender`**: the object that is raising the event.
- **`e`**: an object describing what happened. It derives from `EventArgs`. dotnet/iot's examples:
  `PinValueChangedEventArgs` (`ChangeType`, `PinNumber`) and `ButtonHoldingEventArgs` (`HoldingState`).

`PinChangeEventHandler` is the same shape with a specific args type. dotnet/iot declared its own delegate type
instead of using `EventHandler<PinValueChangedEventArgs>`, but it's shaped identically.

### 6.2 "The object you subscribed on": a walk-through with a button

Take this app code:

```csharp
var button = new GpioButton(buttonPin: 23);    // one object, stored in the variable `button`
button.ButtonDown += OnButtonDown;              // "subscribe ON button": the thing left of the dot

void OnButtonDown(object? sender, EventArgs e)
{
    var which = (GpioButton)sender!;            // works: sender IS that same object
}
```

- **"The object you subscribed on"** means the object on the **left of the dot** in `button.ButtonDown +=`. You
  handed your card to *that* object.
- **"The object that raised the event"** means the object whose code runs `ButtonDown?.Invoke(this, ...)`.
- In correct code **these are the same object**, because the publisher both keeps the list and makes the call:

```csharp
// ButtonBase.HandleButtonPressed()
IsPressed = true;
ButtonDown?.Invoke(this, new EventArgs());      // `this` = the button object → becomes `sender`
```

```
 app:     button.ButtonDown += OnButtonDown       ← subscribed ON `button`
                     │ (card stored in button's own list)
                     ▼
 button:  ButtonDown?.Invoke(this, e)             ← `this` is `button`, so it raised it
                     │
                     ▼
 app:     OnButtonDown(sender: button, e)         ← sender == the object you subscribed on ✅
```

### 6.3 Why `sender` is typed `object`

So **one handler can serve many publishers**:

```csharp
var names = new Dictionary<GpioButton, string>
{
    [new GpioButton(5)]  = "Start",
    [new GpioButton(6)]  = "Stop",
    [new GpioButton(13)] = "E-stop",
};
foreach (var b in names.Keys) b.Press += OnAnyPress;       // one handler, three publishers

void OnAnyPress(object? sender, EventArgs e)
{
    var b = (GpioButton)sender!;                            // which one? sender tells you
    Console.WriteLine($"{names[b]} pressed");
}
```

That's why the convention matters: code written this way **relies** on `sender` being what it says.

---

## 7. The four meanings of `this`

`this` confused you before, understandably: it's one keyword with four jobs. All four appear in the code you've
already seen:

| # | Where you see it | Meaning | Real example |
|---|---|---|---|
| 1 | Inside an instance method or property | **"the object this code is running on right now"** | `ButtonDown?.Invoke(this, new EventArgs());` (`ButtonBase.cs`): pass *myself* as `sender` |
| 2 | Before the **first parameter** of a `static` method in a `static` class | **"this is an extension method: it can be called as if it were a method of that parameter's type"** (§8) | `public static List<int> PerformBusScan(this I2cBus bus, ...)` (`src/devices/Board/I2cBusExtensions.cs`) |
| 3 | After a constructor's parameter list: `: this(...)` | **"first run my other constructor with these arguments"** (constructor chaining) | `public GpioButton(int buttonPin, ...) : this(buttonPin, TimeSpan.FromTicks(DefaultDoublePressTicks), ...)` (`GpioButton.cs`); `public SimulatedI2cDevice() : this(new I2cConnectionSettings(1, 1))` (`Bmxx80/tests/SimulatedI2cDevice.cs`) |
| 4 | As a member name: `this[int index]` | **an indexer**: lets the object be used with `[]` like an array | e.g. `list[0]` on a `List<T>` works because `List<T>` declares `this[int index]` |

Meaning #1 is the only one that's "a value". #2, #3 and #4 are pieces of syntax that happen to reuse the word. When
you meet `this`, ask **"where is it sitting?"** and the table tells you which meaning applies.

---

## 8. Extension methods: how `builder.Services.AddSingleton()` works

### 8.1 The problem they solve

You want to add a method to a type you **don't own** and can't change: a framework type like `I2cBus`, or an
*interface* like `IServiceCollection`, which can't contain method bodies for everyone. An extension method lets you
write a static helper that **reads like** an instance method.

### 8.2 A real one from dotnet/iot: an `i2cdetect` for C#

```csharp
// src/devices/Board/I2cBusExtensions.cs (trimmed)
namespace Iot.Device.Board
{
    public static class I2cBusExtensions                     // (1) static class
    {
        public static List<int> PerformBusScan(this I2cBus bus, int lowest = 0x3, int highest = 0x77)
        //            (2) static method        (3) `this` on the first parameter
        {
            List<int> ret = new List<int>();
            for (int addr = lowest; addr <= highest; addr++)
            {
                try
                {
                    using (I2cDevice device = bus.CreateDevice(addr))
                    {
                        device.ReadByte();   // Success means that this does not throw an exception
                        ret.Add(addr);
                    }
                }
                catch (Exception x) when (x is IOException || x is UnauthorizedAccessException)
                {
                    // Ignore and continue
                }
            }
            return ret;
        }
    }
}
```

That's the same address scan `i2cdetect` does. With `using Iot.Device.Board;` at the top of your file you can write:

```csharp
List<int> found = bus.PerformBusScan();                  // what you write
List<int> found = I2cBusExtensions.PerformBusScan(bus);  // what the compiler actually calls
```

**The whole trick is that rewrite.** The compiler sees `bus.PerformBusScan()`, finds no such method on `I2cBus`,
looks for a static method in an imported static class whose first parameter is `this I2cBus`, and calls that with
`bus` as the first argument. Nothing is added to `I2cBus` itself. It's syntax, not magic.

### 8.3 The same trick in code you've used

| You write | The compiler calls | Where it's defined |
|---|---|---|
| `builder.Services.AddSingleton<IFoo, Foo>()` | `ServiceCollectionServiceExtensions.AddSingleton<IFoo, Foo>(builder.Services)` | `Microsoft.Extensions.DependencyInjection` (`IServiceCollection` is an interface, so this is the only way to give it helpers) |
| `_handlersBySubscription.Keys.Where(...)` | `Enumerable.Where(keys, ...)` | LINQ, `System.Linq` |
| `relevantSubscriptions.ToArray()` | `Enumerable.ToArray(relevantSubscriptions)` | LINQ (used in `LibGpiodV2EventObserver.HandleEdgeEvent`) |
| `bus.PerformBusScan()` | `I2cBusExtensions.PerformBusScan(bus)` | dotnet/iot `Board` |

### 8.4 Rules worth knowing

- They only work if their namespace is **imported** (`using ...`). "Method not found" is often just a missing `using`.
- A real instance method with the same signature **always wins** over an extension method.
- They can't see `private` members. They're outside helpers, with the same access as any other outside code.
- **Go to Definition** on an extension method jumps to the static class, which tells you where it really lives.

Extension methods don't change anything about `sender` or events. They're here because the word `this` means
something different in each place, and now you can read all four meanings.

---

## 9. Threads: who actually runs your handler?

### 9.1 `Invoke` is just a function call

Raising an event is an ordinary, **synchronous** method call. `ButtonDown?.Invoke(this, e)` calls every handler in
the list, one after another, **on the thread that executed that line**, and returns only after the last handler
returns. There's no queue and no hand-off to your thread.

So the question "which thread runs my handler?" always has the same answer: **whichever thread raised the event.**
That depends on the publisher. For the event sources we've looked at:

| Event source | Who raises it | Thread your handler runs on | Evidence in the code |
|---|---|---|---|
| `LibGpiodV2Driver` callbacks | `LibGpiodV2EventObserver.HandleEdgeEventsOfRequestInLoop` | A **dedicated `Thread`** the observer starts, one per line request | `var thread = new Thread(() => HandleEdgeEventsOfRequestInLoop(request)); thread.Start();` in `Observe()` |
| `LibGpiodDriver` (v1) callbacks | `LibGpiodDriverEventHandler` | A **thread-pool** thread from `Task.Run` | `return Task.Run(() => { ... }, token);` |
| `ArduinoGpioControllerDriver` callbacks | Firmata's input reader | The Firmata **input thread** reading the serial port | `FirmataDevice._inputThread` → `DigitalPortValueUpdated` → `FireOnPinChanged` |
| `ButtonBase.ButtonDown`, `ButtonUp`, `Press` | `HandleButtonPressed`/`Released`, called from `GpioButton.PinStateChanged` | **The driver's thread** (whatever called `PinStateChanged`) | `GpioButton` subscribes `PinStateChanged` to the controller |
| `ButtonBase.Holding` (Started) | `StartHoldingHandler`, a `System.Threading.Timer` callback | A **thread-pool** thread from the timer | `_holdingTimer = new Timer(StartHoldingHandler, null, (int)_holdingMs, Timeout.Infinite);` |
| An event you raise in your own code | you | **your** thread | — |

So one `GpioButton` can call your handlers from **two different threads**: `ButtonDown` from the GPIO driver's
thread, `Holding` from a timer thread.

```
 Your process
 ┌───────────────────────────────────────────────────────────────────────────┐
 │ Main thread          : Main() → new GpioButton(...) → button.Press += ... │
 │                        then waits / serves HTTP / whatever                 │
 │                                                                           │
 │ libgpiod observer    : loop { wait for kernel edge → HandleEdgeEvent →    │
 │ thread                 GpioButton.PinStateChanged → ButtonDown/Press      │
 │                        → YOUR handlers run here }                         │
 │                                                                           │
 │ Thread-pool thread   : timer fires → StartHoldingHandler → Holding        │
 │                        → YOUR Holding handler runs here                   │
 └───────────────────────────────────────────────────────────────────────────┘
```

### 9.2 Consequence 1: keep handlers short

While your handler runs, the observer thread is **not** waiting for the next edge. It's busy inside your code.
Events for the other pins handled by that thread wait too. Do quick work in the handler (record a value, set a
flag, queue a message) and do slow work elsewhere (§9.6).

### 9.3 Consequence 2: an exception in your handler lands on the driver's thread

Your exception propagates out of `Invoke`, into the code that raised the event. In the libgpiod v2 observer the whole
loop sits inside one `try`:

```csharp
// LibGpiodV2EventObserver.HandleEdgeEventsOfRequestInLoop (trimmed)
try
{
    ...
    while (request.IsAlive && !_shouldExit)
    {
        ...
        HandleEdgeEvent(edgeEvent);                 // ← your handler runs inside this
    }
}
catch (Exception e) when (e is not OperationCanceledException)
{
    Console.WriteLine($"Unhandled exception while handling libgpiodv2 edge events: {e}");
}
finally
{
    RemoveSubscriptions(requestedOffsets);
    _observedRequests.Remove(request);
}
```

Reading this, an exception thrown by **your** handler would leave the `while` loop, print a message, and the
`finally` would **remove the subscriptions** for that request. The pin would stop reporting edges, and your program
would carry on without them. ⚠ That's inferred from reading the code, not from a run, but it's easy to test (§15,
Try-it 5 sketches how). **Rule of thumb for any callback:** wrap the body in `try/catch` and handle or log the error
yourself. The publisher's thread is not yours to crash.

### 9.4 Consequence 3: shared data needs protection

If your handler updates a field that your main thread also reads, two threads touch the same memory. That's the same
problem as sharing a variable between an ISR and your main loop in firmware, where you'd use `volatile` plus
disabling interrupts, or an atomic. In C#:

```csharp
private int _pressCount;

void OnPress(object? sender, EventArgs e)
{
    Interlocked.Increment(ref _pressCount);     // atomic: safe from any thread
}

// main thread
int snapshot = Volatile.Read(ref _pressCount);
```

For anything bigger than a counter, use a `lock`, or better, don't share at all and pass messages (§9.6).

A real spot worth studying: `ButtonBase` touches `_holdingTimer` from the driver's thread (`HandleButtonPressed`
creates it, `HandleButtonReleased` disposes it) and from the timer's thread (`StartHoldingHandler` disposes it),
with no lock. From reading, a release that lands at the same instant the hold timer fires *could* race. ⚠ Unverified,
and probably rare, but noticing "which threads touch this field?" is exactly the habit to build.

### 9.5 Consequence 4: be careful what you do while holding a lock

Publishers usually protect their subscriber list with a `lock`. The two drivers make **different choices** about
whether to call your handler while still holding it:

| | libgpiod v2 observer | Arduino driver |
|---|---|---|
| Decides who to call | inside `lock (_handlersBySubscription)` | inside `lock (_callbackContainersLock)` |
| **Calls your handler** | **still inside the lock** (`HandleEdgeEvent` → `CallEventHandlers`) | **after releasing the lock** (`FireOnPinChanged` is outside the `lock` block) |
| Protects itself against handlers that subscribe/unsubscribe during the call | `.ToArray()` copies, with the comment *"Should an event handler re enter this class and modify the collection, it will throw, so ToArray the collection first"* | the immutable delegate copy (`threadSafeCopy`) |
| Risk | Your handler runs with the driver's lock held. If it blocks waiting on another thread that needs that lock (say, one trying to subscribe), the two threads deadlock. | Your handler may still be called just after you unsubscribed (you unsubscribe after the copy was taken). |

Neither is "wrong"; this is a real design trade-off. The general guideline in .NET is to **avoid calling unknown
code while holding a lock**, and to accept (and document) that a handler can run once more after unsubscribing.

(Why doesn't the v2 observer deadlock when a handler unsubscribes *on the same thread*? .NET's `lock` is
**re-entrant**: a thread that already holds a lock can take it again. The danger is only *other* threads. The
`ToArray()` copies stop the other failure: changing a collection while a `foreach` is walking it throws.)

### 9.6 Getting work off the driver's thread: the hand-off pattern

The professional pattern: the handler does almost nothing. It drops a message into a thread-safe queue, and **your
own** long-running loop (for example a `BackgroundService`, which you've built at work) processes messages at its
own pace:

```csharp
using System.Threading.Channels;

public sealed class ButtonWorker : BackgroundService
{
    private readonly Channel<PinValueChangedEventArgs> _queue = Channel.CreateUnbounded<PinValueChangedEventArgs>();
    private readonly GpioController _controller;

    public ButtonWorker(GpioController controller)
    {
        _controller = controller;
        _controller.OpenPin(23, PinMode.InputPullUp);
        _controller.RegisterCallbackForPinValueChangedEvent(23, PinEventTypes.Falling, OnEdge);
    }

    // Runs on the DRIVER's thread: do nothing slow here.
    private void OnEdge(object sender, PinValueChangedEventArgs e) => _queue.Writer.TryWrite(e);

    // Runs on YOUR thread (a thread-pool thread owned by the host): do the real work here.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var e in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            // slow work is fine here: database, HTTP, logging, ...
        }
    }

    public override void Dispose()
    {
        _controller.UnregisterCallbackForPinValueChangedEvent(23, OnEdge);   // same method group → removal works (§4)
        base.Dispose();
    }
}
```

That's the event-driven version of the ISR → ring buffer → main-loop pattern from firmware: minimum work in the
interrupt context, real work in the main context. **The shape is the same; only the vocabulary changes.**

### 9.7 The other option: no callbacks at all

If you want to handle edges one at a time in your own loop, skip callbacks and `await` the next edge:

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        WaitForEventResult r = await _controller.WaitForEventAsync(23, PinEventTypes.Falling, stoppingToken);
        if (!r.TimedOut) { /* handle one press */ }
    }
}
```

This looks like your polling loop, but it doesn't spin: the `await` parks the method until the driver reports an
edge. (In the v2 driver, `WaitForEvent` is itself built on a callback plus a wait handle: see `ObserveSingleEvent`
in `LibGpiodV2EventObserver.cs`. Callbacks and waiting are two views of the same machinery.)

### 9.8 Subscriptions keep objects alive

A publisher's list holds a **strong reference** to every subscriber's delegate, and each delegate's `Target` is the
subscriber object. As long as the publisher lives and you're subscribed, your object can't be garbage-collected.
Forgetting `-=` is the classic .NET memory leak. `GpioButton.Dispose` does it right: it unregisters **first**, then
releases the controller:

```csharp
// GpioButton.Dispose(bool)
_gpioController.UnregisterCallbackForPinValueChangedEvent(_buttonPin, PinStateChanged);
if (_shouldDispose) { _gpioController?.Dispose(); } else { _gpioController.ClosePin(_buttonPin); }
```

---

## 10. Everything together: one button press, end to end

Assume a `GpioButton` on pin 23, with a controller using `LibGpiodV2Driver` (for example a Pi 5 on current Raspberry
Pi OS), and your code has done `button.ButtonDown += OnButtonDown;`.

| # | Where | Code | Thread | `sender` at this step |
|---|---|---|---|---|
| 0 | setup | `GpioButton` ctor → `_gpioController.RegisterCallbackForPinValueChangedEvent(23, Falling \| Rising, PinStateChanged)` → `LibGpiodV2Driver.AddCallbackForPinValueChangedEvent` → `_eventObserver.Observe(request, subscription, callback)`; this starts the observer thread | main | — |
| 1 | kernel | falling edge on line 23 | — | — |
| 2 | `LibGpiodV2EventObserver` | `WaitEdgeEventsRespectfully` returns; `ReadEdgeEvents` | observer | — |
| 3 | same | `HandleEdgeEvent` → takes the lock → finds subscriptions for line 23 → `CallEventHandlers` | observer | — |
| 4 | same | `eventHandler.Invoke(this, new PinValueChangedEventArgs(Falling, 23))` | observer | **the observer** |
| 5 | `GpioButton.PinStateChanged(object sender, e)` | ignores `sender`, looks at `e.ChangeType == Falling` with pull-up → `HandleButtonPressed()` | observer | (ignored) |
| 6 | `ButtonBase.HandleButtonPressed` | debounce check → `IsPressed = true` → `ButtonDown?.Invoke(this, new EventArgs())` | observer | **the button** |
| 7 | your `OnButtonDown(sender, e)` | your code | observer | **the button** ✅ |

Two things to take from this table:

- **The thread never changes.** From step 2 to step 7, everything runs on the observer thread, including your
  handler. (Unless you hand work off, §9.6.)
- **`sender` is correct at the layer that follows the convention.** At step 4 the driver passes its internal
  observer. `GpioButton` knows to ignore that and only reads `e`. At step 6 `ButtonBase` raises its *own* event with
  `this`. The button is a well-behaved publisher sitting on top of a badly-behaved one.

---

## 11. Your `reactive/` attempt, revisited

You said you tried this before in `hand_experiments/reactive/` and it didn't work. Looking at it with this lecture's
vocabulary, the instincts were right and three specific pieces were missing. (The full reviews are in
`hand_experiments/lectures/reactive/001` and `002`; this is the short version.)

Your `StateService` had:

```csharp
public delegate void OnEventTrigger(string message);      // (1) a delegate TYPE
// "but how does the worker tell me?"
private void Publish() { OnEventTrigger?.Invoke(); }      // (3) invoking… the type
```

| What was there | What was missing | In dotnet/iot terms |
|---|---|---|
| A delegate **type** (#1 in §2.2) | An **event field** of that type to hold subscribers (#2) | `ButtonBase` has `public event EventHandler<EventArgs>? ButtonDown;` |
| A `Publish()` that invokes | Invoking the *field*, with the argument | `ButtonDown?.Invoke(this, new EventArgs());` |
| The question *"how does the worker tell me?"* | The answer: **the worker calls a normal public method on the publisher**, and that method raises the event | `GpioButton.PinStateChanged` (called by the driver) → `HandleButtonPressed()` → `ButtonDown?.Invoke(...)` |

A corrected minimal version, for comparison:

```csharp
public sealed class StateService
{
    public event EventHandler<string>? StateChanged;                // the guest list, with a doorman

    public void Publish(string message)                             // the "worker tells me" entry point
        => StateChanged?.Invoke(this, message);                     // raise: sender = this StateService
}

// Worker (the one who knows something happened):   _stateService.Publish("tick");
// Listener (the one who wants to know):             _stateService.StateChanged += (s, msg) => Console.WriteLine(msg);
```

That's the same three-role structure as the GPIO stack: **the driver** (like your Worker) knows something happened
and calls in, **the button** (like your `StateService`) owns the event and raises it, and **your app** (like your
Listener) subscribes. You were one field and one argument away.

---

## 12. Spot the difference: #2403

Now the broken example. You have everything needed to see exactly what's wrong.

### 12.1 Put the two side by side

```
 CORRECT: ButtonBase                              BROKEN: GpioPin
 ───────────────────                              ───────────────
 public event EventHandler<EventArgs>?            public virtual event PinChangeEventHandler ValueChanged
     ButtonDown;                                  {
                                                      add    { _controller.RegisterCallback…(_pinNumber, …, value); }
   (field-like: ButtonBase keeps its own list)        remove { _controller.UnregisterCallback…(_pinNumber, value); }
                                                  }
                                                    (custom accessors: GpioPin keeps NO list; it forwards
                                                     your delegate down to the controller → driver)

 ButtonDown?.Invoke(this, e);                      (GpioPin never invokes anything. The driver's runner does:)
   raised BY the object you subscribed on          eventHandler.Invoke(this, …)   // this = LibGpiodV2EventObserver
   → sender == button ✅                             → sender == observer, not the pin ❌
```

Apply the §6.2 test to `pin.ValueChanged += H`:

- *The object you subscribed on:* `pin`.
- *The object that raised the event:* the driver's internal runner (§5 of the conversation briefing lists which
  one, per driver).
- They're different objects, so the convention is broken. A handler doing `(GpioPin)sender` gets an
  `InvalidCastException`.

### 12.2 What the correct shape would be

`GpioButton` already shows it: **subscribe to the lower layer with your own method, then raise your own event with
`this`.** For `GpioPin`, the equivalent is to register a *wrapper* with the controller that calls the user's handler
with the pin as `sender`:

```csharp
// SKETCH ONLY: not tested, not a proposed patch
add
{
    PinChangeEventHandler wrapper = (_, e) => value(this, e);    // `this` = the pin
    _controller.RegisterCallbackForPinValueChangedEvent(_pinNumber, PinEventTypes.Falling | PinEventTypes.Rising, wrapper);
}
```

### 12.3 Why that sketch isn't a fix yet (use §3, §4, §9)

| Problem | Which section explains it |
|---|---|
| `remove` gets the user's original `value`, but the controller holds `wrapper`. They're not equal, so `-=` removes nothing and the handler keeps firing. The pin has to **remember** which wrapper belongs to which handler (a map). | §4 delegate identity |
| The same handler added twice must fire twice, and removing it once must leave one. A simple `Dictionary<handler, wrapper>` loses the second one. | §3.1 multicast semantics |
| `+=`/`-=` can be called from any thread while events are firing on the driver's thread, so the map needs a lock. | §9.4, §9.5 |
| `VirtualGpioPin` overrides `ValueChanged`; `GpioController.RegisterCallback…` is `public virtual` and has its own `sender` question (the controller should pass *itself*). | §5.3 custom accessors |
| Anyone who currently relies on today's `sender` (e.g. Arduino users reading the boxed `int`) breaks at run time. That's why the maintainers want a major release. | lecture 001 §15; conversation #1 §7 |

That's why a "one-line" bug touches almost every concept in this lecture, and why it's a good issue to *study* even
if the PR has to wait.

---

## 13. Common mistakes

1. **Invoking a delegate *type*** instead of a field/instance (`OnEventTrigger.Invoke()`). Declare an `event` field
   and invoke that.
2. **Unsubscribing with a new lambda.** It compiles, runs, and removes nothing. Keep the delegate in a variable, or
   use a named method.
3. **Forgetting to unsubscribe.** Memory leaks, and handlers that run on "dead" objects. Unsubscribe in `Dispose`.
4. **Doing slow or blocking work in a hardware callback.** You're on the driver's thread. Hand the work off.
5. **Letting exceptions escape a handler.** They land in the publisher's code (§9.3). Catch them.
6. **Assuming your handler runs on the main thread.** It runs on the raiser's thread. Protect shared state.
7. **Trusting `sender` blindly** in dotnet/iot GPIO callbacks (#2403). Use `e.PinNumber`, or capture the pin in a
   lambda: `pin.ValueChanged += (_, e) => Handle(pin, e);`.
8. **Raising an event without the null check.** `MyEvent(this, e)` throws if nobody subscribed. Use
   `MyEvent?.Invoke(this, e)`.

## 14. Interview relevance

- **"Explain delegates and events in C#."** Delegate = type-safe function reference that also captures its target
  object; multicast; `event` restricts outsiders to `+=`/`-=`. Mention the function-pointer-plus-userdata analogy;
  interviewers like hearing that you know what's underneath.
- **"What thread does an event handler run on?"** The raiser's. Follow-ups: UI thread marshaling, thread-safe
  raising (`?.Invoke`), exceptions stopping the invocation list.
- **"How do you avoid memory leaks with events?"** Unsubscribe in `Dispose`; weak-event patterns exist for UI
  frameworks.
- **Observer pattern / pub-sub:** events are in-process pub/sub. Kafka and Redis pub/sub are the cross-process
  versions (your `hand_experiments/` track).
- **A strong story:** *"I studied a bug in dotnet/iot where GPIO event handlers get the wrong `sender`. The fix looks
  like one line but runs into delegate identity for unsubscription, multicast semantics, thread safety, and
  behavioral compatibility."* That shows depth without claiming a merged PR.

## 15. Try it (small console programs for your Mac)

Each is a `dotnet new console` program with no hardware and no packages. The "Expected output" is **predicted from
how .NET works**; run it to confirm, and if it differs, that's a great question for the conversation log.

**Try-it 1: multicast order, duplicates and removal**
```csharp
Action a = () => Console.WriteLine("A");
Action b = () => Console.WriteLine("B");
Action? list = null;
list += a; list += b; list += a;
list?.Invoke();                       // Expected: A B A
list -= a;
list?.Invoke();                       // Expected: A B   (the LAST a was removed)
Console.WriteLine(list!.GetInvocationList().Length);   // Expected: 2
```

**Try-it 2: the lambda `-=` trap**
```csharp
Action? e = null;
e += () => Console.WriteLine("hi");
e -= () => Console.WriteLine("hi");   // a different lambda
e?.Invoke();                          // Expected: hi   (nothing was removed)
```

**Try-it 3: an exception stops the rest**
```csharp
Action? e = null;
e += () => Console.WriteLine("first");
e += () => throw new InvalidOperationException("boom");
e += () => Console.WriteLine("third");
try { e(); } catch (Exception ex) { Console.WriteLine($"caught: {ex.Message}"); }
// Expected: first, caught: boom   ("third" never prints)
```

**Try-it 4: which thread runs the handler**
```csharp
var pub = new Pub();
pub.Tick += () => Console.WriteLine($"handler on thread {Environment.CurrentManagedThreadId}");
Console.WriteLine($"main is thread {Environment.CurrentManagedThreadId}");
pub.Raise();                                          // Expected: same id as main
new Thread(pub.Raise).Start();                        // Expected: a different id
await Task.Run(pub.Raise);                            // Expected: a thread-pool id
class Pub { public event Action? Tick; public void Raise() => Tick?.Invoke(); }
```

**Try-it 5: a mini "driver" that dies on a bad handler** (sketch, to model §9.3)
```csharp
var pub = new Pub();
pub.Edge += () => throw new Exception("bad handler");
var t = new Thread(() =>
{
    try { while (true) { Thread.Sleep(200); pub.Raise(); Console.WriteLine("edge handled"); } }
    catch (Exception ex) { Console.WriteLine($"observer loop died: {ex.Message}"); }
});
t.Start(); t.Join();
// Expected: "observer loop died: bad handler" and nothing after it: the same shape as LibGpiodV2EventObserver
class Pub { public event Action? Edge; public void Raise() => Edge?.Invoke(); }
```

**Try-it 6: an extension method of your own**
```csharp
Console.WriteLine(0x76.ToHexAddress());               // Expected: 0x76
static class AddressExtensions
{
    public static string ToHexAddress(this int address) => $"0x{address:X2}";
}
```

## 16. Check yourself

1. In `ButtonBase`, where is the list of `ButtonDown` subscribers actually stored?
2. Why can `GpioButton` unsubscribe with `PinStateChanged` even though that creates a new delegate object?
3. Your handler is subscribed to a libgpiod v2 callback and takes 2 seconds. What happens to edges on that pin, and
   on other pins in the same request, during those 2 seconds?
4. Name the four meanings of `this`, and give the dotnet/iot line for three of them.
5. What does the compiler turn `bus.PerformBusScan()` into, and what would make it fail to compile?
6. A `GpioButton`'s `Holding` handler and `ButtonDown` handler both increment the same `int`. Why is `++` not
   enough?
7. Explain #2403 in two sentences using the words *custom accessor*, *forward*, and *raiser*.
8. Why does the naive wrapper fix break `-=`? What data structure would fix it, and what makes it tricky?

## 17. Glossary

| Term | Meaning |
|---|---|
| **Delegate type** | A declaration of a method shape (parameters + return type). Like a C function-pointer `typedef`. |
| **Delegate instance** | An object referencing one or more methods, each with its target object. Callable with `Invoke` or `()`. |
| **Method group** | Writing a method's name without calling it (`PinStateChanged`); converts to a delegate. |
| **Lambda** | An inline anonymous method, `(a, b) => ...`. |
| **Closure** | A lambda plus the local variables it captured. |
| **Multicast / invocation list** | A delegate holding several methods, called in order. |
| **Event** | A member that exposes only `+=`/`-=` to outsiders; the owner raises it. |
| **Field-like event** | `public event X? E;`: the compiler generates the storage and thread-safe accessors. |
| **Custom accessors** | `event X E { add {…} remove {…} }`: you decide where subscriptions go. |
| **Publisher / raiser** | The object that owns and invokes the event. |
| **Subscriber / handler** | The method that gets called. |
| **`sender`** | By convention, the publisher that raised the event. |
| **Extension method** | A static method with `this` on its first parameter, callable like an instance method. |
| **Re-entrant lock** | A lock the same thread can take again while holding it (.NET `lock` is re-entrant). |
| **`Channel<T>`** | A thread-safe async queue for handing work between threads. |

## Sources

dotnet/iot `main` @ `1eb0b2f` (2026-09-24):
`src/System.Device.Gpio/System/Device/Gpio/{PinChangeEventHandler,GpioPin,GpioController}.cs`,
`.../Drivers/{LibGpiodV2EventObserver,LibGpiodV2Driver,LibGpiodDriverEventHandler}.cs`,
`src/devices/Button/{ButtonBase,GpioButton}.cs`, `src/devices/Arduino/ArduinoGpioControllerDriver.cs`,
`src/devices/Board/I2cBusExtensions.cs`, `src/devices/Gpio/tests/VirtualGpioTests.cs`,
`src/devices/Bmxx80/tests/SimulatedI2cDevice.cs`. This repo: `hand_experiments/reactive/`.
