# 01 — Brief: dotnet/yarp#275 (phase 1: remove Autofac)

> Owner: desktop Claude. CLI: read-only. Facts checked 2026-09-29/30 against `main` @ `0cae8ca` (2026-09-11).
> Full desktop briefing: `exercises/scouting/conversation/001-conversation-log.md` entry #1 §2.

## 1. The issue in one paragraph

[dotnet/yarp#275](https://github.com/dotnet/yarp/issues/275) (open, `help wanted`, `Type: Task`, since 2020, 0
comments, no PR ever). YARP's tests use **Autofac.Extras.Moq** (`AutoMock`): it builds the class under test and
auto-creates a loose Moq mock for every constructor parameter. The issue argues this adds dependencies without
making tests shorter, and hides what each class needs. **Phase 1 (our proposal):** remove Autofac only; keep Moq.
It's a refactor: **no test may change what it proves.**

## 2. The cast

| Character | Job |
|---|---|
| Class under test (e.g. `ForwarderMiddleware`) | Takes dependencies via its constructor |
| **Moq** | Makes stand-ins for interfaces: `Setup` (script answers), `Verify` (check calls). Stays |
| **AutoMock** | Builds the class, auto-mocks every ctor parameter, returns the *same* mock later via `Mock<T>()` |
| **Autofac** | DI container underneath AutoMock. Goes |
| **`TestAutoMockBase`** (`test/Tests.Common/TestAutoMockBase.cs`) | Base class exposing `Create<T>()`, `Mock<T>()`, `Provide<T>()`, `ResetMocks()`. Goes |

## 3. Measured scope

| File (under `test/ReverseProxy.Tests/`) | Uses | Effort |
|---|---|---|
| `Forwarder/StreamCopierTests.cs` | inherits only | trivial |
| `Forwarder/ForwarderHttpClientFactoryTests.cs` | `Mock<ILogger<…>>()` | small |
| `LoadBalancing/LoadBalancingPoliciesTests.cs` | `Provide<IRandomFactory>` + 6× `Create<…Policy>()` | small |
| `Forwarder/ForwarderMiddlewareTests.cs` | `Create<ForwarderMiddleware>()` + shared `Mock<IHttpForwarder>()` | medium |
| `Model/ProxyPipelineInitializerMiddlewareTests.cs` | `Provide<RequestDelegate>` + `Create<…>()` | medium |
| `Delegation/HttpSysDelegatorMiddlewareTests.cs` | `Provide`, `Create`, several `Mock<T>()` | medium |
| `Delegation/HttpSysDelegatorTests.cs` | ~13 `Mock<T>()`, mocks wired into mocks | largest |

Then delete `TestAutoMockBase.cs`; remove `Autofac` + `Autofac.Extras.Moq` from `test/Tests.Common/Yarp.Tests.Common.csproj`
and `test/ReverseProxy.Tests/Yarp.ReverseProxy.Tests.csproj`; remove `AutofacVersion` + `AutofacExtrasMoqVersion`
from `eng/Versions.props`. Moq (25 files) stays.

Example constructor: `ForwarderMiddleware(RequestDelegate next, ILogger<ForwarderMiddleware> logger,
IHttpForwarder forwarder, IRandomFactory randomFactory)`, which throws on any null.

## 4. The traps

1. **Shared mocks.** With AutoMock, `Mock<IHttpForwarder>()` in the test *is* the one injected. The rewrite must pass
   that same mock into the constructor, or `Setup`/`Verify` check a different object and the test can pass for the
   wrong reason.
2. **Loose defaults.** AutoMock mocks are loose (unscripted calls return defaults). Hand-built stand-ins must behave
   the same (`new Mock<T>()` default is loose; `NullLogger<T>.Instance` for loggers).
3. **Proof of preservation.** For each rewritten test, temporarily break the product code it guards and confirm it
   goes red; save that as evidence.

## 5. Build and platform

- `global.json`: SDK **11.0.100-rc.1**. `./restore.sh` installs it into `.dotnet/` in the clone; then
  `source activate.sh` (or use `./.dotnet/dotnet`). Works on the Mac; **not** on Timothy's Linux desktop.
- Tests: Microsoft.Testing.Platform; `./test.sh` for all, or `dotnet test test/ReverseProxy.Tests/` for one project.
- **Unverified:** whether `HttpSysDelegator*Tests` run on macOS (HttpSys is Windows-only). WO-1 checks.
- Mac disk is tight (~20 GB free): the repo-local SDK + restore can take a few GB.

## 6. Repo rules

- Read `CONTRIBUTING.md` and the PR template before the PR (WO-1 records what they say in the session report).
- Ask first: the issue is 6 years old. Draft comment in
  `exercises/scouting/002-code_pr_shortlist_week_of_2026-09-29.md`.
