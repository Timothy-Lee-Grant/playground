# IoT Concepts: lecture notes

Lectures on the concepts behind dotnet/iot and IoT on Linux in general: GPIO, drivers, the kernel interfaces,
and the .NET patterns the library is built on. Each one is prompted by an issue but written to teach the concept
**holistically**, so it's still useful after that issue is closed. The issue shows up inside a lecture as a worked
example, not as the subject.

**How lectures are written** (root [`../../CLAUDE.md`](../../CLAUDE.md) §4.4 and [`../../persona.md`](../../persona.md)):
why it exists and what problem it solves → the cast of characters (personified, named components) → how they
interact (diagram) → control flow → the real upstream code (file and type names) → edge cases → common mistakes →
interview relevance → real-world usage → check-yourself questions. Name files `NNN-title.md`.

Concept notes that are only about one issue belong in that issue's conversation log, not here.

---

## Index

| # | Lecture | Prompted by | Status |
|---|---|---|---|
| 001 | [The Big Picture: what dotnet/iot is, how it's built, and how to use it](001-the-big-picture-dotnet-iot.md) | #2403 (orientation before starting) | Written 2026-09-27; behavior claims unverified until samples run |
| 002 | [Delegates, events, callbacks and threads (plus extension methods and `this`)](002-delegates-events-callbacks-and-threads.md) | #2403 (taught from working code: `Button`, Arduino and libgpiod v2 drivers; #2403 as a spot-the-difference) | Written 2026-09-28; includes "Try it" programs to run on the Mac. Revised 2026-09-29 after the first teach-back (`=` vs `+=`, event doorman, library vs framework) + teach-back checklist §18 |
| 003 | [Bindings, OOP architecture, interfaces, `IDisposable` and ownership, UnitsNet, testing without hardware](003-bindings-oop-architecture-disposal-and-testing.md) | Questions after 001 | Written 2026-09-28; includes the failing-test sketch for #2403 |
| 004 | [CI pipelines: what the YAML file controls, what the service controls, and how dotnet/iot does it](004-ci-pipelines-how-they-are-set-up.md) | Questions 2026-10-01 (how pipelines are wired; could someone edit the YAML; agents, triggers, artifacts) | Written 2026-10-01 from dotnet/iot `95384e7`; Azure DevOps settings unverified. Taught as general software engineering, not through firmware |
| 005 | [A tour of dotnet/iot issues: fourteen bugs, what they teach, and which to take next](005-a-tour-of-dotnet-iot-issues.md) | Timothy's request 2026-10-03 (while #2611 awaits review) | Written 2026-10-03 from `95384e7`; hypotheses labeled; ⭐ candidates: #1887 (top), #1328, #2356, #1715 (later), #1469 |

## Candidates (from #2403, entry #1 §12; 002 and 003 cover the first two and most of the fourth)

| Topic | Why it matters here |
|---|---|
| .NET events and delegates: `event` accessors, multicast delegates, delegate identity | The whole #2403 fix hinges on `-=` removing the exact delegate instance that `+=` registered. |
| dotnet/iot architecture: controller, driver, pin, bindings | Explains why the driver, not the pin, ends up calling your handler. |
| GPIO on Linux: sysfs vs libgpiod v1 vs v2, edge events | What the drivers are actually talking to, and why there are three of them. |
| Breaking changes in libraries: binary, source and behavioral breaks; semver | Why a one-line-looking fix "needs a major release". |

## Audio lectures (for NotebookLM)

Written to be **listened to**: loaded into Google NotebookLM to generate a podcast-style Audio Overview. Only written
when Timothy explicitly asks for one. Format: root [`../../CLAUDE.md`](../../CLAUDE.md) §10.7. Files live in
[`audio/`](audio/): `NNN-audio-<topic>.md` (the source to upload) + `NNN-audio-<topic>.prompt.md` (paste into
NotebookLM's "Customize" box; don't upload it).

| # | Audio lecture | Prompted by | Status |
|---|---|---|---|
| A001 | [How a .NET application fits together: from a callback to a running program on a Raspberry Pi](audio/001-audio-how-a-dotnet-app-fits-together.md) ([customize prompt](audio/001-audio-how-a-dotnet-app-fits-together.prompt.md)) | Follow-up to lecture 002; aimed at the application-model layer (library vs framework, IL/packages/`using`, solution/project/process, the host and DI lifetimes, web + GPIO in one host) | Written 2026-09-29 (~4,400 words); not yet listened to |
| A002 | [The Recipe, the Kitchen and the Cooks: how a CI pipeline really works](audio/002-audio-ci-pipelines.md) ([customize prompt](audio/002-audio-ci-pipelines.prompt.md)) | Companion to lecture 004 | Written 2026-10-01 (~4,000 words); not yet listened to |
