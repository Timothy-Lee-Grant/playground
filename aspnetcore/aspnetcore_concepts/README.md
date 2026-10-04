# ASP.NET Core concepts

Lectures on ASP.NET Core (dotnet/aspnetcore) and the .NET concepts behind it, written to stay useful across issues.
Style and template: root [`CLAUDE.md`](../../CLAUDE.md) §4.4 and §8.5.

| # | Lecture | Prompted by | Status |
|---|---|---|---|
| — | *(no reading lectures yet)* | | |

## Audio lectures (for NotebookLM)

Written to be **listened to**: loaded into Google NotebookLM to generate a podcast-style Audio Overview. Only written
when Timothy explicitly asks for one. Format: root [`../../CLAUDE.md`](../../CLAUDE.md) §10.7. Files live in
[`audio/`](audio/): `NNN-audio-<topic>.md` (the source to upload) + `NNN-audio-<topic>.prompt.md` (paste into
NotebookLM's "Customize" box; don't upload it).

| # | Audio lecture | Prompted by | Status |
|---|---|---|---|
| A001 | [ASP.NET Core from the ground up: the framework, its repository, and the .NET ideas underneath](audio/001-audio-aspnetcore-from-the-ground-up.md) ([customize prompts](audio/001-audio-aspnetcore-from-the-ground-up.prompt.md)) | Orientation for all ASP.NET Core work: framework vs library, shared framework vs packages vs assemblies vs namespaces, extension methods/lambdas/null operators/async, startup and how the pipeline is built, DI lifetimes and ownership, options/config/logging, HttpContext over features, one request step by step (worked example: `HttpsRedirectionMiddleware`), async and cancellation, feature-area tour, repo layout and build, testing (TestServer, fakes vs mocks, red/green), contribution process (help wanted, API review, PublicAPI baselines, branches). Checked against `main` @ `dc8b384` (2026-10-03) | Written 2026-10-03 (~14,000 words, three episodes via the prompt file); not yet listened to. Listen before YARP A001 (same character names) |
