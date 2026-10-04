# dotnet/aspnetcore: project orientation

> Shared context for every ASP.NET Core issue folder. Started 2026-10-03. No issue folders yet.

## 1. What's in this folder

| Path | What it is |
|---|---|
| [`aspnetcore_concepts/`](aspnetcore_concepts/) | Lecture notes on ASP.NET Core and the .NET ideas behind it (A001: full orientation, audio) |

## 2. What ASP.NET Core is

A **framework** (it owns the main loop after `app.Run()`): Kestrel and the other servers, hosting, HTTP abstractions,
routing, middleware, security, and the programming models (minimal APIs, MVC, Razor Pages, Blazor, SignalR). Ships
mostly as the **shared framework** `Microsoft.AspNetCore.App`, not as NuGet packages. **Not here:** DI, configuration,
logging, options, Generic Host, `HttpClientFactory` (all `Microsoft.Extensions.*` → dotnet/runtime); EF Core
(dotnet/efcore); the Razor compiler (dotnet/razor); core gRPC (grpc/grpc-dotnet); docs (dotnet/AspNetCore.Docs); YARP.

## 3. Upstream facts (checked 2026-10-03, `main` @ `dc8b384`)

| | |
|---|---|
| Version on `main` | 12.0 alpha 1. .NET 11 is on `release/11.0` (RC phase); older: `release/10.0`, `release/9.0`, `release/8.0` |
| `global.json` SDK | `11.0.100-rc.1.26420.103` (installed into `.dotnet/` by the restore script) |
| Setup | Clone fork with `--recursive` (submodules: MessagePack-CSharp, googletest) → `./restore.sh` → `source activate.sh` |
| Build/test one area | `src/<Area>/build.sh` (`-test` to run tests), or `dotnet build` / `dotnet test --filter …` on one `.csproj` after activation. Don't build the whole repo |
| Needs | Node.js for JS areas (Blazor, SignalR); Mac or Codespaces (the Linux desktop can't run .NET 11, per yarp/CLAUDE.md) |
| Not yet tried | None of the above has been run on Timothy's machines yet |
| Project conventions | `<Reference>` (not `PackageReference`) in product projects; `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` per shipping project (RS0016 if missing); `IsAspNetCoreApp=true` ⇒ in the shared framework |
| Tests | xUnit v3, Moq; `*.Tests` (MVC: `*.Test`), `*.FunctionalTests`, `*.E2ETests`; `TestServer` / `WebApplicationFactory`; flaky → `[QuarantinedTest]` |
| CI | Azure Pipelines (`.azure/pipelines/ci-public.yml`), tests on Helix queues |
| Conventions to read first | `.github/copilot-instructions.md` (minimal diffs, `is null`, red/green rule), area `AGENTS.md` (Mvc, Components, ProjectTemplates), `ARCHITECTURE.md` (Components, SignalR) |

## 4. Contribution rules (from CONTRIBUTING.md and docs/)

- Pick `help wanted` / `good first issue`; otherwise agree on the issue first. Bigger changes: `design proposal` issue.
- Any new/changed public API → PublicAPI.Unshipped entry **and** a separate API proposal (`api-ready-for-review` → weekly review → `api-approved`) before RTM. Prefer behavior-only fixes for first PRs.
- No new `InternalsVisibleTo` / `[UnsafeAccessor]` across framework assemblies.
- PR template: checklist, summary < 80 chars, `Fixes #NNNN`. CLA: one per .NET Foundation project (already signed for dotnet/iot).
- Stale after 2 weeks without activity, closed 4 days later. Squash merges. Target `main`; servicing to `release/*` goes through Shiproom (team handles it; never changes public API).
- Area labels and owners: `docs/area-owners.md` (e.g. `area-middleware`, `area-networking`, `area-minimal`, `area-mvc`, `area-blazor`).
