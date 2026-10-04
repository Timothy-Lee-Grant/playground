# NotebookLM customization prompts for YARP audio lecture 001

Upload only `001-audio-yarp-from-the-ground-up.md` as the source. **Don't upload this file**; paste one of the
prompts below into the Audio Overview "Customize" box.

The source is ~10,000 words, well above the 2,000–4,000 range used so far. One episode would have to skip a lot, so
it's set up as **two episodes from the same source**. Generate episode 1, then a second Audio Overview with the
episode 2 prompt. Choosing the "Longer" length option (if offered) should help each one cover its parts fully.

---

## Episode 1: parts 1 to 6 (what YARP is, the ASP.NET Core machinery, DI, the Rulebook, one request's journey)

The listener is an engineer who has used YARP from the outside (ran WebSocket experiments through it) and is about
to contribute code to it. Cover only parts one to six of the source, in order, and don't skip any of them. Spend the
most time on part six, the step-by-step journey of one request: go through every numbered step in order and say who
hands what to whom. Keep the character names from the source exactly: the Listener, the Job Ticket, the Assembly
Line, the Sorter, the Supply Room, the Rulebook, the Librarian, the Clerk, the Proxy Note, the Loyalty Desk, the
Dispatcher, the Inspector, the Courier, the Editor, the Outbound Line, the Pump and the Watchdog. Always say the
real class name next to the character the first time. Say clearly that YARP is a library that plugs into the
ASP.NET Core framework, not a framework or a separate server, and that routes point at clusters, not servers. For
every "common misconception" in the source, state the wrong version, then the correction. Describe sequences in
words and never ask the listener to picture or imagine anything. Don't add facts or numbers that aren't in the
source. End by repeating ideas one to seven from the final recap.

## Episode 2: parts 7 to 11 (health checks, the toolbox, the repository tour, testing and issue 275, misconceptions)

The listener already heard an episode on YARP's request pipeline (the Clerk, the Dispatcher, the Courier and so on).
Open with a two-minute reminder of the request journey from part six, then cover parts seven to eleven of the
source, in order. Spend the most time on part ten: walk the Invoke_Works test object by object (which objects are
real, which is fake, and the arrange, act, assert moves), explain what AutoMock does underneath, and explain both
traps (the shared-mock trap and the loose-default trap) slowly, with the consequence of each. Keep the character
names from the source exactly, including the Patrol and the Inspector, and say the real class name next to each.
Say clearly that Autofac is only used in YARP's tests, never in the product code. Read out every "common
misconception" in part eleven as wrong version, then correction. Describe sequences in words and never ask the
listener to picture or imagine anything. Don't add facts or numbers that aren't in the source. End by repeating all
ten key ideas from the final recap.
