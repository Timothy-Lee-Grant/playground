# NotebookLM customization prompts for ASP.NET Core audio lecture 001

Upload only `001-audio-aspnetcore-from-the-ground-up.md` as the source. **Don't upload this file**; paste one of the
prompts below into the Audio Overview "Customize" box.

The source is ~14,000 words (YARP A001 was ~10,000 as two episodes). It's set up as **three episodes from the same
source**. Generate episode 1, then a new Audio Overview for each of the other two prompts. Choose the "Longer" length
option if it's offered. Listen to this before YARP A001: YARP's episode assumes the ASP.NET Core machinery covered here,
and both use the same character names (Listener, Job Ticket, Assembly Line, Sorter, Supply Room).

---

## Episode 1: parts 1 to 4 (what ASP.NET Core is, where its code comes from, the C# you'll read, startup)

The listener is an engineer who has built ASP.NET Core apps from the outside and is about to contribute to the
dotnet/aspnetcore repository. Cover only parts one to four of the source, in order, and don't skip any. Say clearly
that ASP.NET Core is a framework (it owns the main loop after Run) and bound that statement the way the source does.
In part two, explain the difference between an assembly, a namespace, a NuGet package and the shared framework
slowly, and say plainly that the using directive grants no access. In part three, go through all five C# features and
expand each null operator into its if/else form, as the source does. Spend the most time on part four: what the
builder phase, Build and Run each do, and how the pipeline is built once, backwards, into a single RequestDelegate
that ends in a 404 step. Keep the character names exactly (the Stage Manager) and say the real class name next to each
character the first time. For every "common misconception", state the wrong version, then the correction. Describe
sequences in words and never ask the listener to picture or imagine anything. Don't add facts or numbers that aren't
in the source. End by repeating final-recap ideas one to four.

## Episode 2: parts 5 to 8 (services and lifetimes, the request object and features, one request's journey, async)

The listener already heard an episode on what ASP.NET Core is and how startup builds the pipeline. Open with a
one-minute reminder of that, then cover parts five to eight of the source, in order. Spend the most time on part
seven, the journey of one request: go through all ten numbered steps in order, including the first plain-HTTP request
that the Doorman redirects with a 307 without calling next, and say at each step who hands what to whom. Keep the
character names exactly: the Supply Room, the Settings Board, the Options Packet, the Scribe, the Job Ticket, the
Feature Drawers, the Listener, the Hand-off Desk, the Assembly Line, the Doorman, the Sorter, the ID Checker, the Guard,
the Runner and the Adapter, with the real class name next to each the first time. In part five, explain the three
lifetimes, who disposes what, the captive dependency, and why conventional middleware takes scoped services in Invoke.
In part six, explain why HttpContext is a front over features and why it must not be kept after the request. In part
eight, be precise that await doesn't create threads and that cancellation is cooperative. Read every "common
misconception" as wrong version, then correction. Never ask the listener to picture or imagine anything. Don't add
facts that aren't in the source. End by repeating final-recap ideas five to eight.

## Episode 3: parts 9 to 12 (the feature areas, the repository and build, testing, contributing)

The listener already heard two episodes on ASP.NET Core's core machinery and the journey of one request. Open with a
one-minute reminder of the request journey from part seven, then cover parts nine to twelve, in order. In part nine,
stress the shared pattern (Add for services, Use or Map for the pipeline) and the bound that Blazor's interactivity
doesn't use the request pipeline. In part ten, explain the PublicAPI Shipped and Unshipped files, why projects use
Reference elements, and the build steps in order (clone recursively, restore, source activate, build one area), and
say that these steps haven't been tried on the listener's Mac yet. Spend the most time on part eleven: walk the
SetOptions_SetStatusCodeHttpsPort test object by object (what's real, what's replaced, arrange, act, assert), explain
fakes versus mocks, and the red-then-green rule row by row. In part twelve, explain the help-wanted label, API review
and why behavior fixes are easier first contributions than public API changes, and the branches. Read all the
misconceptions as wrong version, then correction. Never ask the listener to picture or imagine anything. Don't add
facts or numbers that aren't in the source. End by repeating all ten ideas from the final recap.
