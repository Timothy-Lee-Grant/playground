# How a .NET Application Fits Together: From a Callback to a Running Program on a Raspberry Pi

Audio lecture 001, written on 2026-09-29 to be listened to as a NotebookLM Audio Overview. It follows reading lecture 002, on delegates, events, callbacks and threads, and goes one level up: how all the pieces of a real .NET program fit together.

## Who this is for and what it's about

This episode is for an engineer who knows embedded C well, has written some C# and some ASP.NET Core, and can get things working, but who feels the big picture is still a bit blurry. Not broken. Blurry.

That blurriness is worth taking seriously. When you don't know something at all, you notice. It stops you, and you go and learn it. But when you half-know something, you can stumble through for months. The code compiles, the app runs, and the fuzzy picture never gets fixed, because nothing forces it. This episode goes after exactly that kind of fuzziness.

The key idea for the whole episode fits in one sentence. **To understand any .NET program, ask two questions: where does each piece of code come from, and who calls whom?** Everything that follows answers one of those two questions.

We'll go in six steps. First, a quick tune-up on delegates and events, the smallest example of "who calls whom". Second, where code comes from: from source files to something that runs on your Pi. Third, the difference between a solution, a project and a running process. Fourth, the difference between a library and a framework. Fifth, the host: the one real framework in a typical app, and what its builder, build and run steps actually do. And sixth, all of it put together, following one button press on a Raspberry Pi all the way to a web page.

## Part one: a tune-up on delegates and events

Let's start small, with the cast from the delegates lecture.

The first character is **the Phone-Number Card**. That's a delegate instance. It's a small object that holds two things: which method to call, and which object to call it on. That second part is what a C function pointer doesn't have. In C, a function pointer is just an address, and if the callback needs to know which sensor or which connection it belongs to, you pass a void-star user-data pointer separately. A C# delegate carries its target object inside it. The key idea: **a delegate is a function pointer plus the object it belongs to, packaged in one type-safe object.**

The second character is **the Guest List**, the invocation list inside a delegate. One delegate can hold several cards, and when you invoke it, it calls each one in order, on whatever thread did the invoking.

Now a C habit that trips people up. In C, the only way to put a function into a function pointer is the equals sign. So it's natural to assume the equals sign is how you add a method to a delegate. **It isn't. In C#, the equals sign replaces. The plus-equals operator adds.** Walk through it slowly. Say you have a delegate variable called h. You write h equals A: now h holds just A. You write h plus-equals B: now h holds A and then B. Then you write h equals C, and h holds only C. A and B are gone. Nothing warned you. That's exactly why the equals sign is dangerous in the wrong hands, and it's the reason the event keyword exists, which we'll get to in a moment.

Next, immutability. The rule in one line: **a delegate object never changes after it's created. What changes is the variable or field that points at it.** When you write plus-equals, C# doesn't reach into the existing guest list and add a name. It builds a brand-new delegate with the longer list and points your field at the new one. The old object is left exactly as it was.

Why does that matter? Here's the sequence, with two threads. Thread one is about to raise an event. It reads the field and gets the current delegate, say one holding A and B. At that moment, thread two unsubscribes everyone and sets the field to null. If thread one reads the field a second time to invoke it, it gets null and crashes. The fix is to read the field **once**, into a local variable, or to use the question-mark-dot operator, which does the same thing. Thread one then holds the old delegate with A and B, and because delegate objects never change, that local copy stays whole and safe to call. A common misconception is that the delegate object itself might "become null". Objects never become null. **The field can be re-pointed to null. The object you already hold can't change.**

Now **the Doorman**, which is the event keyword. Here's a good design exercise. Say you're writing a Button class, and you want other classes to be able to subscribe to its Pressed notification. The dangerous version is a public delegate field, because then anyone outside could use the equals sign and wipe out everyone else's subscription, or invoke it and fake a button press. So you'd make the delegate field private, and add two public methods: one to add a subscriber and one to remove one. Only the Button itself can invoke the list.

If you'd designed that yourself, well done, because **that's exactly what the event keyword is.** When you declare a public event, the compiler generates a private delegate field and two public methods, add and remove, and it makes them thread-safe for you. From outside the class, the only operations that compile are plus-equals and minus-equals. Inside the class, you can do everything, including invoking.

It's just as important to say what the Doorman does **not** do. **He doesn't know who's at the door.** .NET has no built-in notion of "which object is calling this method" that an event could check. The event doesn't validate subscribers or identity. It restricts which operations the compiler allows from outside the class. So minus-equals from outside will remove any card that matches the one you pass, whoever added it. In practice you can usually only name your own handlers, so it works out, but the protection comes from the compiler limiting operations, not from any identity check.

One last point about delegates, because it answers a common question: why do delegates seem to show up all over an application? Because a delegate is just a value, and values get passed around. When you call Task-dot-Run with a lambda, you're passing a delegate. When you write a LINQ Where with a lambda, you're passing a delegate. When you register a GPIO callback, you pass a delegate to the pin, which passes it to the controller, which passes it to the driver, and only the driver finally stores it in a list. So delegates travel. An event is the pattern where they **end up** in one place, owned by the publisher.

Quick recap of part one. A delegate is a function pointer plus its target object. The equals sign replaces; plus-equals adds. Delegate objects never change; fields get re-pointed. The event keyword is the private-field-plus-add-and-remove pattern, generated for you, and it restricts operations, not identity.

## Part two: where code comes from

Now the first of our two big questions: where does each piece of code come from? Let's follow code from the person who writes it all the way to the chip it runs on. There are five characters on this journey.

The first is **the Author's source**: the C# files in a repository, say the dotnet/iot repository on GitHub, written by Microsoft engineers and community contributors.

The second is **the Translator**, the C# compiler. Here's the first big surprise for an embedded engineer. The C# compiler doesn't produce ARM machine code or x86 machine code. It produces something called **IL**, intermediate language: a CPU-neutral instruction set, packaged in a file called an assembly, which is the DLL you see on disk. **The same DLL runs on your Mac, on a Linux desktop and on a Raspberry Pi.**

The third character is **the Shipping Crate**, which is a NuGet package. A package is basically a zip file of those assemblies, sometimes a few versions for different .NET versions, plus a description of what the package depends on. A common misconception is that packages are compiled separately for every CPU and the package manager downloads the one for your chip. For ordinary C# code, that's not how it works: there's one IL DLL for every CPU. The exception is **native** code, meaning actual C libraries shipped inside a package. Those really are built per platform and sit in platform-specific folders inside the crate. The package System.Device.Gpio is almost entirely IL; the C library it talks to, libgpiod, isn't in the crate at all. It's installed on the Pi by Linux, and the C# code reaches it through P/Invoke.

The fourth character is **the Address Book**, which is your project file, the one ending in dot-c-s-proj. A line in it called a PackageReference says "this project uses the System.Device.Gpio package, version such-and-such". **That line is what gives your code access to the package.** When you build, the SDK fetches the crate and hands its assemblies to the compiler as references.

The fifth character is **the Nickname**, the using directive at the top of a C# file. Another common misconception: that the using line is what unlocks a package. It isn't. The using line only saves typing. Instead of writing the fully qualified name, System-dot-Device-dot-Gpio-dot-GpioController, you write using System-dot-Device-dot-Gpio once at the top and then just say GpioController. If you removed every using line and typed full names everywhere, the program would still compile, because access comes from the Address Book. One more detail: namespaces and assemblies are separate ideas. One assembly can contain many namespaces, and packages from different teams can put types into related namespaces. A namespace is a naming scheme, not a container you download.

And the last step: **the On-Site Builder**, the just-in-time compiler inside the .NET runtime on the device. When your program starts on the Pi, the runtime reads the IL and compiles each method to real ARM machine code the first time it's called. That's why the same DLL can run on any CPU: the final translation to machine code happens on the device itself.

So the journey is: source code, then the Translator turns it into IL in an assembly, the Shipping Crate carries the assemblies, the Address Book in your project file references the crate, the Nickname in your C# file saves typing, and the On-Site Builder turns IL into machine code on the device where it runs.

Recap of part two. C# compiles to CPU-neutral IL, not to machine code. Packages carry IL assemblies, and only native pieces are per platform. Access comes from the package reference in the project file; the using directive is only a shortcut for names. The JIT compiler on the device makes the final machine code.

## Part three: solution, project, process

Now three words that often get blurred together: solution, project and process.

Start with **the Filing Cabinet**, which is the solution file, the one ending in dot-s-l-n. The key idea: **a solution is just a way for your editor and build tools to group projects together. It has no effect at all on how anything runs.** Delete the solution file and every project still builds on its own. It's a folder label.

Next, **the Recipe**, which is a project. **Each project produces exactly one assembly.** A project has an output type, and there are two that matter. An executable project produces an assembly with an entry point, a Main method, and it can be started as a program. A class library project produces an assembly with no entry point. It can't run by itself; it's only ever loaded by something else.

Then **the Workshop**, which is a process: a running program, with its own memory, its own threads, and its own copy of everything it has loaded. Only executable projects become processes.

Now here's the misconception to clear up. It's tempting to think each project in a solution runs as its own process. Only executables do. A class library runs **inside** whichever process loads it. Say your solution has two executable projects, a web API and a background worker, and one class library called Shared-dot-Data that talks to a database. When you start both programs, you get two Workshops. Each one loads its own copy of Shared-dot-Data. Two copies of the code running, two separate sets of objects in memory, two separate connections to the database. The class library is **shared code**, not a shared running thing. If the two Workshops need to share live state, they have to talk to each other across a process boundary: through a database, a message queue, or a network call. Nothing in the solution file makes them share memory.

Recap of part three. A solution is a filing cabinet with no runtime meaning. A project is a recipe for exactly one assembly, either an executable or a library. A process is a running executable, and libraries run inside whichever process loads them, separately in each.

## Part four: library or framework, and who owns the flow

Now the second big question: who calls whom?

In a **library**, you call it. Your program is in charge. You decide when to call its methods, and when your Main method ends, the program ends. Examples are an HTTP client, a JSON serializer, and, importantly for us, **dotnet/iot**.

In a **framework**, it calls you. You configure it, hand it pieces of your code, and then hand over control, and from then on the framework decides when your code runs. This is called inversion of control, sometimes nicknamed the Hollywood principle: don't call us, we'll call you. ASP.NET Core is a framework. So is the .NET Generic Host. So is a test runner like xUnit, which finds your test methods and calls them.

Now the trap, and it's worth slowing down for. A callback is a small piece of inversion of control. When you register a GPIO callback, you hand over a delegate and the driver decides when it runs. So it's tempting to conclude that dotnet/iot must be a framework, like ASP.NET Core. **It isn't. Using inversion of control in one spot doesn't make something a framework.** The test is who owns the program's flow. With dotnet/iot, you create a GPIO controller yourself, you call read and write when you choose, there's no run method that takes over your main thread, and your program keeps doing whatever it was doing. The callback is a **small pocket of inversion inside a program you still control.** That makes dotnet/iot a library, and the GPIO controller is closer to an HTTP client than to the web application object in ASP.NET Core.

The same goes for the device bindings in dotnet/iot, the one hundred and thirty-some folders under source-slash-devices, one per sensor or chip. They're library code too, shipped together in a package called Iot-dot-Device-dot-Bindings. You use them yourself. To read a BMP280 pressure sensor, for example, you first create an I2C device, telling it which I2C bus and which address the chip is at, say bus one and address hex seventy-six. Then you create a Bmp280 object and pass it that I2C device. Then you call its read methods whenever you like. Nobody calls you. That's how your application talks to I2C: through ordinary objects that you create and call.

Recap of part four. The test for library versus framework is who owns the flow. dotnet/iot is a library, its bindings are libraries, and a callback is a small pocket of inversion of control, not a framework.

## Part five: the host, the one real framework in your app

If dotnet/iot isn't a framework, where does the framework show up in a real app? In the **host**. Meet **the Stage Manager**. In ASP.NET Core it's the web application object. Underneath, it's the .NET Generic Host, which also exists without any web parts at all. The Stage Manager's job is to own the program's lifetime: start everything in the right order, keep it running, and shut it down cleanly.

The Stage Manager works in three phases, and you've written all three if you've done any ASP.NET Core. Let's give each one its real purpose.

Phase one is **the builder**. You create a builder and use it to decide **what exists**: which services, which configuration settings, which logging. When you register a service, you're telling the dependency-injection container "when someone asks for this type, here's how to make one, and here's how long it lives". There are three lifetimes. A singleton is created once and shared for the whole life of the app. A scoped service is created once per scope, and in a web app a scope means one HTTP request. A transient service is created fresh every time someone asks for one. The container doesn't just create these objects; it also disposes of them at the end of their lifetime. For a scoped service, that means at the end of the request. The memory itself is later reclaimed by the garbage collector; there's no delete like in C.

Phase two is **build**. When you call build, the Stage Manager freezes the list of services and creates the container. After that, the list is read-only. Try to register another service and you get an exception. What you get back is the application object.

Phase three is **configuring the request pipeline on the application object**, and then **run**. Why is the pipeline set up on the application and not on the builder? Because they answer different questions. The builder answers "what exists?" The pipeline answers "in what order does each request travel through my code?" Middleware often needs services from the finished container, which only exists after build. So the order is: decide what exists, freeze it, then wire the path requests take through those things. In fact, the application object is itself a pipeline builder. That's the role it plays in phase three.

Finally, you call run. This is the moment of inversion of control. Your Main method hands its thread to the Stage Manager and waits. The Stage Manager starts Kestrel, the web server, which we'll call **the Front Door**. When a request arrives, the Front Door gets or reuses a context object for that request, opens a scope for scoped services, sends the request through your middleware in order, and cleans up the scope at the end. Your code only ever runs because the Stage Manager called it.

The Generic Host can also run **hosted services**, and this is the piece that makes everything else fit. A hosted service is a class, usually derived from one called BackgroundService, with an execute method that starts when the app starts and keeps running in the background until the app shuts down. You register it on the builder, just like any other service. One host can run the Front Door and any number of background services at the same time, all in one process.

Recap of part five. The host is the framework: it owns the program's lifetime. The builder decides what exists and how long things live. Build freezes the container. The pipeline, configured on the application, decides the order requests flow through. Run hands over control. And one host can run a web server and background services together.

## Part six: one button press, all the way to a web page

Now let's put everything together on a Raspberry Pi. The goal is a small app that watches a button and shows on a web page how many times it's been pressed.

How many frameworks does this app have? **One.** Not "an IoT framework plus a web framework". One host, the Stage Manager, running two things: the Front Door serving web requests, and a background service we'll call **the Pin Watcher**. dotnet/iot is just a library the Pin Watcher uses.

Here's how it's wired, in words. In the builder phase, you register one GPIO controller as a singleton, because there's only one set of pins and everyone should share the same controller. You register it with a small factory method, so the container creates it. You register a small counter object as a singleton too, so both the Pin Watcher and the web endpoint see the same count. And you register the Pin Watcher as a hosted service. Then you build, map one web endpoint that returns the count, and run.

Now follow one button press.

Step one. The Stage Manager starts the Pin Watcher. The Pin Watcher asks the container for the GPIO controller, opens pin seventeen as an input, and registers a callback for the falling edge. That callback is a Phone-Number Card: a delegate holding the Pin Watcher's handler method and the Pin Watcher object itself as its target. The card travels from the controller to the driver, which stores it in its list.

Step two. Somebody presses the button. The voltage on pin seventeen falls. The Linux kernel notices the edge and marks the event as ready.

Step three. The driver's own background thread, which has been waiting in the kernel, wakes up, reads the event, walks its guest list and invokes the Pin Watcher's card. **Your handler runs on the driver's thread**, not on the host's thread and not on any web request's thread. That's the second question, who calls whom, answered at the smallest scale.

Step four. The handler increments the counter. Because the web endpoint might read the counter from a different thread at the same moment, the increment has to be thread-safe: an interlocked increment or a lock. The handler stays short and returns, so the driver can go back to watching for edges.

Step five. Somebody opens the web page. The Front Door receives the request, the Stage Manager sends it through the pipeline, the endpoint asks the container for the counter, reads it safely, and returns the number.

Step six. When you stop the app, the Stage Manager shuts things down in order. The Pin Watcher's execute method is told to stop. The Pin Watcher unregisters its callback, and because it passes the same method on the same object, the removal matches. Then the container disposes the singletons, including the GPIO controller, which releases the pins. Here's something the container does **not** do: it only disposes objects it created itself. That's why we registered the controller with a factory. If you'd created the controller yourself and handed the finished object to the container, disposing it would still be your job.

Look at the characters in that story. The Stage Manager owns the flow: that's the framework. The GPIO controller and the driver are library code that the Pin Watcher chose to call. The one place where control is inverted inside the library is the callback, and that callback runs on a thread the library owns. Every piece of code came from somewhere definite: your project's own assembly, the System.Device.Gpio package, the ASP.NET Core shared framework, and libgpiod, installed on the Pi by Linux. All of it runs in **one** Workshop, one process, because there's only one executable project.

## Common misconceptions, one more time

To close, here are the misconceptions from this episode, each with its correction, because hearing the wrong version next to the right one is what makes the right one stick.

A common misconception is that the equals sign adds a method to a delegate. Actually, it replaces the whole list. Plus-equals adds.

A common misconception is that the delegate object can become null while you're using it. Actually, objects never change or become null. The field can be re-pointed, so read it once.

A common misconception is that an event checks who is subscribing. Actually, it only restricts which operations compile outside the class.

A common misconception is that a library using callbacks is a framework. Actually, the test is who owns the program's flow, and dotnet/iot leaves the flow with you.

A common misconception is that NuGet packages are compiled per CPU. Actually, ordinary C# ships as CPU-neutral IL, and only native pieces are per platform.

A common misconception is that the using directive gives you access to a package. Actually, the package reference in the project file does. Using only shortens names.

A common misconception is that every project in a solution is its own process. Actually, only executable projects become processes, and libraries run inside each process that loads them.

And a common misconception is that a web server plus GPIO means two frameworks. Actually, it's one host running a web server and a background service, and the background service uses dotnet/iot as a library.

## The key ideas to take away

If you remember nothing else, remember these.

One. Ask two questions of any .NET program: where does each piece of code come from, and who calls whom?

Two. A delegate is a function pointer plus its target object. Equals replaces, plus-equals adds, and delegate objects never change.

Three. The event keyword is a private delegate plus public add and remove methods, generated for you. It restricts operations, not identity.

Four. C# compiles to CPU-neutral IL. Packages carry it, the project file references it, using only shortens names, and the JIT makes machine code on the device.

Five. A solution groups projects, a project makes exactly one assembly, and only executables become processes.

Six. A library is something you call. A framework calls you. dotnet/iot is a library with small pockets of inversion called callbacks.

Seven. The host is the framework. The builder decides what exists, build freezes it, the pipeline decides the order requests travel, and run hands over control.

Eight. One host can run a web server and background services together, and a background service is where a Raspberry Pi app uses dotnet/iot.

That's the map. Next time something feels fuzzy, place it on the map: where did this code come from, and who is calling it right now?
