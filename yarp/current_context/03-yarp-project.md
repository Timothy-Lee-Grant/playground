# 03 · The upstream project: dotnet/yarp

> Facts checked 2026-10-07 against `dotnet/yarp` `main` @ `0cae8ca` (2026-09-11) unless dated otherwise. Re-check
> anything version-like (SDK pin, package versions, CI files) at the start of each issue; update this file and the
> date when something changed.

## 1. What YARP is, in five lines

1. **YARP** ("Yet Another Reverse Proxy") is a **library** for building reverse proxies on ASP.NET Core. NuGet
   package `Yarp.ReverseProxy`. Latest stable **2.3.0** (2025-02-27); `main` is versioned **3.0.0-preview.1**.
2. You host it in your own ASP.NET Core app: `builder.Services.AddReverseProxy().LoadFromConfig(...)`, then
   `app.MapReverseProxy()`. ASP.NET Core calls into it; YARP doesn't own a server loop or listen on sockets itself.
3. Configuration is **routes** (match incoming requests) → **clusters** (groups of interchangeable backends +
   backend settings) → **destinations** (the actual servers).
4. `MapReverseProxy` adds one endpoint per route; each endpoint runs YARP's own small pipeline:
   `ProxyPipelineInitializerMiddleware` → session affinity → load balancing → passive health → `LimitsMiddleware`
   → `ForwarderMiddleware`, which calls `IHttpForwarder.SendAsync` (`HttpForwarder`) to copy the request out and
   the response back.
5. Almost all services are registered as **singletons** (`src/ReverseProxy/Management/*Extensions.cs`), so
   per-request data lives on `HttpContext` (the `IReverseProxyFeature`), and shared state must be thread-safe.

Deeper: `link/yarp_concepts/audio/001-audio-yarp-from-the-ground-up.md` (whole-system tour, with the character
names Timothy knows: Clerk, Dispatcher, Courier, Pump, Watchdog…) and `link/yarp_concepts/001-the-gatekeeper-in-the-middle.md`
(connections, WebSockets, timeouts).

## 2. Source map

| Path | What's there |
|---|---|
| `src/ReverseProxy/` | The library. Subfolders by concern: `Configuration` (RouteConfig, ClusterConfig, providers, validation), `Management` (ProxyConfigManager + **DI registration: start reading here**), `Routing` (MapReverseProxy, endpoint factory), `Model` (RouteModel, ClusterState, DestinationState, the pipeline initializer), `SessionAffinity`, `LoadBalancing`, `Health`, `Limits`, `Forwarder` (ForwarderMiddleware, HttpForwarder, StreamCopier, client factory), `Transforms`, `Delegation` (HTTP.sys, Windows-only), `ServiceDiscovery` (DNS), `WebSocketsTelemetry` |
| `src/TelemetryConsumption/`, `src/Kubernetes.Controller/`, `src/Application/` | Typed telemetry listeners; the Kubernetes ingress controller; the prebuilt JSON-configured container app |
| `test/ReverseProxy.Tests/` | Unit tests; folders mirror `src/ReverseProxy/` |
| `test/ReverseProxy.FunctionalTests/` | End-to-end tests with real in-process servers (WebSockets, headers, cancellation, passive health…) |
| `test/Tests.Common/` | Shared test helpers, incl. `TestAutoMockBase.cs` (Autofac.Extras.Moq; issue #275 removes it) |
| `test/Kubernetes.Tests/`, `test/Application.Tests/` | Tests for those two components |
| `samples/` | One runnable sample per feature (`BasicYarpSample`, `ReverseProxy.Config.Sample`, `…Transforms…`, `…Direct…`, …) |
| `docs/` | Design notes, operations notes, roadmap. **User docs are not here**: they live in `dotnet/AspNetCore.Docs` |
| `eng/` | Arcade build infrastructure; `eng/Versions.props` = every dependency's version |
| `global.json` | Pins the SDK (below) |
| `azure-pipelines-pr.yml` | PR CI on Azure DevOps (runs for PRs to any branch). `.github/workflows/` only has markdown lint and a Docker build |
| `.github/CODEOWNERS` | `* @MihaZupan`: auto-requested reviewer for every path |

## 3. Build and test

| | |
|---|---|
| SDK | `global.json` pins **11.0.100-rc.1.26420.103** (plus runtimes 8.0.13 and 9.0.2 for multi-targeted tests). Test runner: **Microsoft.Testing.Platform** |
| Machines | **Mac: yes.** The Linux desktop **can't** run .NET 11 (CPU lacks x86-64-v2). Codespaces works |
| First time in a clone | `cd yarp && ./restore.sh` installs that SDK into `yarp/.dotnet/` (about 760 MB; on the SSD now). Then `source activate.sh` puts it on `PATH` for that shell, or call `./.dotnet/dotnet` directly |
| Build everything | `./build.sh` |
| All tests | `./build.sh --test` (or `./build.sh --configuration Release --rebuild --test`). `./test.sh` only runs tests and doesn't build first: on a fresh tree it "fails" in about 1 s. `./build.sh` passes `/warnaserror`; plain `dotnet build` doesn't |
| One test project | `./.dotnet/dotnet test --project "$PWD/test/ReverseProxy.Tests/Yarp.ReverseProxy.Tests.csproj"`: an **absolute** `--project` path. The relative/positional form (`dotnet test test/ReverseProxy.Tests/`) fails with a doubled path (verified #275 `002`; cause unverified) |
| One test class / method | `--filter-class <Namespace.Class>` or `--filter-method <Namespace.Class.Method>` (no `--` needed). Names must be **full**: a short name silently matches 0 tests and still "passes", so check the test count (verified #275 `002`) |
| Build output | `yarp/artifacts/`, shared by every branch: build with `--no-incremental` after switching |
| HTTP.sys tests | Windows-only feature, but `HttpSysDelegator*Tests` (mocked) **run** on macOS (verified #275 `002`) |

## 4. Contribution rules (from `CONTRIBUTING.md`, checked 2026-10-07)

- **Almost all contributions start with an issue.** Features and substantial changes: agree on the design in the
  issue first. Small fixes (typos, bugs): fine to start directly. "Help wanted" and "good first issue" labels are
  up for grabs; **comment on the issue if you want to do the fix**.
- Extensibility first: many feature requests should be a custom module, not a core change; changes that *enable*
  extensibility are welcome.
- **CLA**: the .NET Foundation CLA. Timothy already signed it (2026-10-02, for dotnet/iot); it covers all .NET
  Foundation projects.
- The PR must build and pass all tests. Expect thorough review; the team may retarget the merge branch.
- No PR template in `.github/` and no AI-use policy file found in the repo (2026-10-07). Timothy discloses AI use
  in his PR description anyway.
- Security issues go to MSRC by email, never to a public issue.

## 5. People seen so far

| Who | Role (observed) |
|---|---|
| MihaZupan | CODEOWNERS for everything; main YARP maintainer |

Add rows as people show up in your issues (name, role, what they cared about). Don't @mention anyone in commits.

## 6. Issues so far

| Issue | Type | State |
|---|---|---|
| #1764 WebSocket idle timeout | docs (PR in dotnet/AspNetCore.Docs#37747) | Waiting for review. Folder: `link/1764_websocket_idle_timeout/` (legacy) |
| #275 Remove Autofac from tests | test refactor | Building privately; nothing posted upstream. Folder: `link/issues/275_remove-autofac-from-tests/` |

Branches: `04-branches.md`.
