# NotebookLM customization prompts for OpenTelemetry audio lecture 001

Upload only `001-audio-opentelemetry-from-the-ground-up.md` as the source. **Don't upload this file**; paste one of
the prompts below into the Audio Overview "Customize" box.

The source is ~13,000 words, so it's set up as **three episodes from the same source** (parts 1–4, 5–8, 9–13). Generate
episode 1, then a second and third Audio Overview with the other prompts. Choose the "Longer" length option if it's
offered.

---

## Episode 1: parts 1 to 4 (the problem, what OpenTelemetry is and isn't, the signals, the .NET twist)

The listener is an engineer who knows ASP.NET Core from the outside and is about to contribute to the OpenTelemetry
.NET repositories. Cover only parts one to four of the source, in order, without skipping any. Spend the most time on
part four: the translation from OpenTelemetry terms to .NET types (ActivitySource is the tracer, Activity is the span,
Meter is the meter, ILogger is the logs API), why those types live in the .NET runtime, and the three sources of code
in the app's process (the runtime shared framework, the ASP.NET Core shared framework, and NuGet packages). Say
clearly that OpenTelemetry is not a backend or dashboard, that the using directive grants no access, and that
libraries reference only the API package while applications add the SDK. Keep the character names exactly: the
Charter, the Dictionary, the Sorting Office, the Archive, the Sticky Note and the Return Address, and say what each
one really is the first time. For every "common misconception" in the source, state the wrong version, then the
correction. Describe sequences in words and never ask the listener to picture or imagine anything. Don't add facts
or numbers that aren't in the source. End by repeating the recap of part four.

## Episode 2: parts 5 to 8 (the cast, startup, one span's life, one trace across two processes)

The listener already heard an episode on what OpenTelemetry is and how .NET maps its concepts to Activity and
ActivitySource. Open with a one-minute reminder of that mapping, then cover parts five to eight of the source, in
order. Spend the most time on part seven: go through every numbered step of the span's journey in order, saying who
hands what to whom and what the Clipboard holds after each step, then explain the full-queue case ("drop the newest,
never block") slowly. In part eight, go through the propagation steps in order and explain the four parts of the
traceparent header. Keep the character names exactly: the Reporter, the Timecard, the Case File, the Clipboard, the
Bureau Chief and its phone line, the Selector, the Desk Clerk, the Mailroom, the Out-Tray, the Mail Carrier, the
Packer, the Stamper, the Stamp and the Annotator, and say the real class name next to each the first time. Say
clearly that OpenTelemetry is a subscriber, not middleware, and that StartActivity can return null. Read out every
"common misconception" as wrong version, then correction. Say plainly that the experiments' results are predictions
that haven't been run. Never ask the listener to picture or imagine anything. Don't add facts or numbers that aren't
in the source. End with the recap of part eight.

## Episode 3: parts 9 to 13 (AsyncLocal and the baggage bug, metrics and cardinality, logs/sampling/Collector/conventions, the repo and contributing, misconceptions)

The listener already heard two episodes: what OpenTelemetry is, and how a span travels through the .NET SDK and
across services (the Reporter, the Timecard, the Clipboard, the Mailroom, the Stamper). Open with a two-minute
reminder of that journey, then cover parts nine to thirteen of the source, in order. Spend the most time on part
nine: explain AsyncLocal's two rules (values flow down, changes don't flow back up), then walk the four-step baggage
leak slowly, saying what the parent and child each point at and what each one reads after every step, and make clear
the leak is caused by the shared mutable box, not by AsyncLocal. In part ten, explain cardinality with the raw-path
example and the 2000 limit. In part twelve, walk the CheckIfBatchIsExportingOnQueueLimit test object by object:
which objects are real, what the in-memory exporter is, what's absent, and the arrange, act, assert moves. Keep all
character names from the source. Read every misconception in part thirteen as wrong version, then correction.
Never ask the listener to picture or imagine anything. Don't add facts or numbers that aren't in the source. End by
repeating all ten key ideas from the final recap.
