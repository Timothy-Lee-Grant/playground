# Lecture 001 · The change, end to end: removing Autofac from YARP's tests

| | |
|---|---|
| **Issue** | dotnet/yarp#275, phase 1 (Autofac only; Moq stays) |
| **Branch / commit** | `remove-autofac-275` @ `1665ced5` (4 commits on top of `upstream/main` @ `2aa3d835`). Local only, not pushed |
| **Date** | 2026-10-07 (written by the CLI, which did the work) |
| **Prerequisites** | None required. Helpful: your iot#2328 Moq experience (`new Mock<T>()`, `.Object`, `Setup`, `Verify`) |
| **How to read this** | **20-minute path:** §0 (run it), §1, §4, §9 checklist. **Full read** (~45 min): everything. §7 is for when you're ready to write the upstream comment |
| **Companion** | Lecture 002 ("Testing it yourself") is the procedure for checking all of this on your own before the PR |

Every number in this lecture comes from an evidence file in `shared/evidence/` (named in brackets, e.g. **[002]**).
Anything not run is marked **unverified**.

---

## §0 · Watch it work (10 minutes)

**One-line rule: before reading about the change, see one rewritten test pass, then see it catch a bug.**

All commands are run **from `yarp/`** (the clone), on branch `remove-autofac-275`.

```bash
cd yarp
git status -sb                      # expect: ## remove-autofac-275...upstream/main [ahead 4]
./.dotnet/dotnet --version          # expect: 11.0.100-rc.1.26420.103
```

**Step 1: green.** Run the 3 tests in `ForwarderMiddlewareTests` (they run twice, once per framework, so 6):

```bash
./.dotnet/dotnet test --project "$PWD/test/ReverseProxy.Tests/Yarp.ReverseProxy.Tests.csproj" \
  --filter-class Yarp.ReverseProxy.Forwarder.Tests.ForwarderMiddlewareTests
```

Expected (last lines):
```
  total: 6
  failed: 0
  succeeded: 6
  skipped: 0
```

**Step 2: break the product code.** Make `ForwarderMiddleware` send the request to the wrong URL (adds `+ "x"`):

```bash
sed -i '' 's/destinationModel.Config.Address, clusterConfig.HttpClient,/destinationModel.Config.Address + "x", clusterConfig.HttpClient,/' src/ReverseProxy/Forwarder/ForwarderMiddleware.cs
git diff --stat                     # expect: 1 file changed, src/ReverseProxy/Forwarder/ForwarderMiddleware.cs
./.dotnet/dotnet build "$PWD/test/ReverseProxy.Tests/Yarp.ReverseProxy.Tests.csproj" --no-incremental
./.dotnet/dotnet test --project "$PWD/test/ReverseProxy.Tests/Yarp.ReverseProxy.Tests.csproj" --no-build \
  --filter-class Yarp.ReverseProxy.Forwarder.Tests.ForwarderMiddlewareTests
```

Expected: `Invoke_Works` fails on both frameworks with:
```
Moq.MockException : Mock<IHttpForwarder:2>:
This mock failed verification due to the following:
   IHttpForwarder h => h.SendAsync(DefaultHttpContext, It.Is<string>(uri => uri == "https://localhost:123/a/b/"), ...):
   This setup was not matched.
  at ...ForwarderMiddlewareTests.Invoke_Works() in .../ForwarderMiddlewareTests.cs:105
```
Line 105 is `_forwarder.Verify();`. Remember that line; §4 explains why it's the heart of the change.

**Step 3: undo, and confirm green again.**

```bash
git restore src/ReverseProxy/Forwarder/ForwarderMiddleware.cs
git status --short                  # expect: nothing except "?? .DS_Store" (Finder's file; ignore it)
./.dotnet/dotnet build "$PWD/test/ReverseProxy.Tests/Yarp.ReverseProxy.Tests.csproj" --no-incremental
./.dotnet/dotnet test --project "$PWD/test/ReverseProxy.Tests/Yarp.ReverseProxy.Tests.csproj" --no-build \
  --filter-class Yarp.ReverseProxy.Forwarder.Tests.ForwarderMiddlewareTests      # expect: total 6, failed 0
```

**Why `--no-incremental` in step 2:** without it, the build can decide nothing changed and leave the old
`Yarp.ReverseProxy.dll` next to the tests. Then the "broken" code never runs and the test stays green. That
happened during the run (§6, trap 4). (Verified: this exact sequence was rehearsed on 2026-10-07.)

---

## §1 · The problem

**One-line rule: the tests asked a tool to build the object under test, and that hid what the object needs.**

**Purpose of a unit test with fakes:** check one class on its own. Its collaborators (the things it calls) are
replaced by **mocks**: stand-in objects (made by the Moq library) that answer as you script them and record what
was called on them.

Before the change, 7 test classes inherited from `TestAutoMockBase` (`test/Tests.Common/TestAutoMockBase.cs`, 99
lines, now deleted). It wrapped a tool called **`AutoMock`** from the package `Autofac.Extras.Moq`. A test looked
like this (`ForwarderMiddlewareTests`, before):

```csharp
public class ForwarderMiddlewareTests : TestAutoMockBase       // ← inherits the helper
{
    ...
        Mock<IHttpForwarder>()                                  // ← "give me THE mock of IHttpForwarder"
            .Setup(h => h.SendAsync(...)) ...;

        var sut = Create<ForwarderMiddleware>();                // ← "build a ForwarderMiddleware for me"
        ...
        Mock<IHttpForwarder>().Verify();                        // ← "check THE mock was called as scripted"
```

(`sut` = "system under test", the object being tested.)

What's wrong with that, according to the issue:

| Complaint | Shown in the code above |
|---|---|
| **You can't see what the class needs.** | `Create<ForwarderMiddleware>()` says nothing about the 4 constructor arguments. A reader must open `ForwarderMiddleware.cs` to learn there's a `next`, a logger, a forwarder and a random factory |
| **The link between "the mock I script" and "the mock it got" is invisible.** | Nothing on screen says the `Mock<IHttpForwarder>()` on the first line is the object `Create<…>` passed into the constructor. It is, but only because `AutoMock` promises to return the same one each time |
| **Two extra packages** for something plain C# can do. | `Autofac` (a dependency-injection container) + `Autofac.Extras.Moq`, in 2 `.csproj` files and `eng/Versions.props` |

**What changed, in one sentence:** each test class now calls the real constructor itself, passing the mocks
explicitly; then the helper and both packages were deleted. **No product code changed. No test checks anything
different.**

---

## §2 · The cast

**One-line rule: three roles: the Subject being tested, the Stand-ins around it, and (before) the Butler that
assembled them.**

The character names are new (this lecture introduces them). The YARP names (the Courier, the Clerk, the
Dispatcher) come from the YARP audio lecture A001, which you haven't listened to yet, so treat them as new too.

| Character | Real type | Job | Talks to |
|---|---|---|---|
| **The Butler** (gone) | `AutoMock` (package `Autofac.Extras.Moq`), wrapped by `TestAutoMockBase` | Built the Subject for you: looked at its constructor, made a loose mock for each interface parameter, built real objects for concrete ones, and remembered every mock so `Mock<T>()` returned the same one later | Autofac (a DI container) underneath; Moq to make the mocks |
| **The Stand-in Maker** (stays) | Moq: `Mock<T>`, `.Object`, `Setup`, `Verify` | Makes stand-ins for interfaces and abstract classes | The tests, directly |
| **A Stand-in** | `Mock<IHttpForwarder>` etc. | `mock.Object` is the fake the Subject receives; `mock` itself is the remote control you script and inspect | Subject calls `.Object`; the test calls `Setup`/`Verify` on `mock` |
| **The Silent Logger** | `NullLogger<T>.Instance` (`Microsoft.Extensions.Logging.Abstractions`) | A real logger that throws every message away | Subject |
| **The Courier** | `IHttpForwarder` (real one: `HttpForwarder`) | Sends the request to the backend. In the tests: always a Stand-in | `ForwarderMiddleware` calls `SendAsync` |
| **The Last Stop** | `ForwarderMiddleware` | Picks the destination and hands the request to the Courier | Courier, `IRandomFactory` |
| **The Clerk** | `ProxyPipelineInitializerMiddleware` | First step of YARP's per-route pipeline: sets up `IReverseProxyFeature`, then calls `next` | `next` (a `RequestDelegate`) |
| **The Dice Maker** | `IRandomFactory` | Gives out a `Random` for "pick one at random" | Load balancing policies, both middlewares |
| **The HttpSys Handoff** | `HttpSysDelegator` (sealed class) + `HttpSysDelegatorMiddleware` | Windows-only feature: hands a request to another process's HTTP.sys queue instead of proxying it | `IServer` → `IServerDelegationFeature`; `IHttpSysRequestDelegationFeature` on the request |

**Who builds the Subject in production vs in a test** (this is the dependency-injection question, made concrete):

```
PRODUCTION                                              TEST (after the change)
app.MapReverseProxy()                                   new ForwarderMiddleware(
  └─ proxyAppBuilder.UseMiddleware<ForwarderMiddleware>()     _ => Task.CompletedTask,
       (src/ReverseProxy/Routing/                             NullLogger<ForwarderMiddleware>.Instance,
        ReverseProxyIEndpointRouteBuilderExtensions.cs:48)    _forwarder.Object,
  └─ ASP.NET Core asks the DI container for each              new Mock<IRandomFactory>().Object)
     constructor argument:
       IHttpForwarder → HttpForwarder   (registered in ReverseProxyServiceCollectionExtensions.cs:34)
       IRandomFactory → RandomFactory   (IReverseProxyBuilderExtensions.cs:47)
       ILogger<T>     → the app's logger
```

The constructor is the same in both columns. Production lets the container fill it in; the test fills it in by
hand. **Before** the change, the test also used a container (Autofac, via the Butler). That's the part that went.

---

## §3 · The change, file by file

**One-line rule: 4 commits, grouped by difficulty, then one deletion commit.**

```
9db5c959 Construct simple test subjects directly instead of via AutoMock      3 small files
4526c6a5 Construct middleware under test directly instead of via AutoMock     3 medium files
f5f16fef Build HttpSysDelegator and its mock graph explicitly in tests        1 large file
1665ced5 Remove TestAutoMockBase and the Autofac test dependencies            helper + 2 csproj + Versions.props
```

11 files, +96 −176 lines, all under `test/` plus `eng/Versions.props` **[008]**. Full diff: `evidence/010-diff.patch`.

**Why this order:** each commit compiles and passes on its own. The deletion comes last, so it's pure removal
that a reviewer can check with one command (`git grep -i autofac` → nothing) **[007]**.

**Why `NullLogger` instead of a logger mock, everywhere:** no test in the 7 files looks at a logger (checked:
**[003]** §4 / Stage 5, Step 3, finding 3). `NullLogger<T>.Instance` says "logging doesn't matter here" in the
code itself, and sibling tests already use it (e.g. `LimitsMiddlewareTests`). Both a loose logger mock and
`NullLogger` answer "is logging enabled?" with `false`, so the log calls do nothing either way.

**Syntax you'll meet in the diff, expanded:**

| Compact form | Means |
|---|---|
| `private readonly Mock<IHttpForwarder> _forwarder = new();` | "target-typed `new`": the compiler takes the type from the left side. Same as `= new Mock<IHttpForwarder>();` |
| `_ => Task.CompletedTask` | A **lambda**, here a tiny function used as a `RequestDelegate`: "given a context (named `_` because it's ignored), return an already-finished `Task`", i.e. do nothing |
| `context => { context.Response.StatusCode = 418; return Task.CompletedTask; }` | A lambda with a body: sets status 418 and finishes |
| `new Mock<IRandomFactory>().Object` | Make a stand-in, and pass only its fake object. Nobody keeps the remote control, so nobody can script or check it: it's **inert** |

The small files, in detail (the medium and large ones are in §4):

**`StreamCopierTests`**: it inherited `TestAutoMockBase` but never called it. Change: `: TestAutoMockBase` removed.
That's the whole diff (1 line).

**`ForwarderHttpClientFactoryTests`**: it already called `new ForwarderHttpClientFactory(...)`; only the logger
came from the Butler: `Mock<ILogger<ForwarderHttpClientFactory>>().Object` (11×) → `NullLogger<…>.Instance`. The
`using` lines were adjusted (`Microsoft.Extensions.Logging` → `…Logging.Abstractions`; `Yarp.Tests.Common` dropped).

**`LoadBalancingPoliciesTests`**: before, the test-class constructor *registered* a fake with the Butler:
`Provide<IRandomFactory>(RandomFactory);`. Each test then said `Create<RandomLoadBalancingPolicy>()`, and the Butler
passed that fake in. After: the `Provide` line is gone, and the tests say what they mean:

```csharp
var loadBalancer = new RandomLoadBalancingPolicy(RandomFactory);   // was: Create<RandomLoadBalancingPolicy>()
var loadBalancer = new FirstLoadBalancingPolicy();                 // no constructor arguments at all
```

`PickDestination_Random_Works` asserts the exact sequence that `RandomInstance` (inside `RandomFactory`) produces,
so it can only pass if the policy really uses *this* factory: the wiring proves itself.

**Rejected here:** a private `CreatePolicy()` helper. Each construction is one short expression, so a helper would
add a layer and hide nothing worth hiding. (The plan proposed helpers in general; this is a recorded deviation.)

---

## §4 · The pattern once, in depth: `ForwarderMiddlewareTests`

**One-line rule: a mock the test touches *after* handing it over must be one object, kept in a field and passed
into the constructor.**

**Before** (simplified):

```
 test code                         the Butler (AutoMock)              the Subject
 ─────────                         ─────────────────────              ───────────
 Mock<IHttpForwarder>()  ───────►  "do I have one? no → make M1,
   .Setup(SendAsync ...)            remember it"  ── returns M1
 Create<ForwarderMiddleware>() ──► reads the constructor:
                                    next      → makes a delegate mock
                                    logger    → makes a logger mock
                                    forwarder → "IHttpForwarder? I have M1" ──► new ForwarderMiddleware(…, M1.Object, …)
                                    random    → makes a mock
 Mock<IHttpForwarder>().Verify() ─► "do I have one? yes → M1"  ── returns M1
```

The test works **only because** the Butler hands back M1 every time. Nothing in the test code shows that.

**After** (`test/ReverseProxy.Tests/Forwarder/ForwarderMiddlewareTests.cs` at `1665ced5`):

```csharp
20  public class ForwarderMiddlewareTests                              // no base class
21  {
22      private readonly Mock<IHttpForwarder> _forwarder = new();      // ONE stand-in, made once per test
 …
74          _forwarder
75              .Setup(h => h.SendAsync(httpContext, It.Is<string>(uri => uri == "https://localhost:123/a/b/"), …))
 …              .Verifiable();                                     // "Verify() should check this setup"
93          var sut = CreateMiddleware();
 …
105         _forwarder.Verify();                                       // checks the SAME object
 …
171     private ForwarderMiddleware CreateMiddleware()
172     {
173         return new ForwarderMiddleware(
174             _ => Task.CompletedTask,                               // next: this middleware never calls it
175             NullLogger<ForwarderMiddleware>.Instance,              // logging: irrelevant
176             _forwarder.Object,                                     // ← the field's fake: the shared stand-in
177             new Mock<IRandomFactory>().Object);                    // inert: only used with 2+ destinations
178     }
```

Line by line, why:

| Line | Why this and not something else |
|---|---|
| 22 field | The test scripts the stand-in (74), builds the Subject (93), then checks the stand-in (105). All three must be the same object. A field makes that visible. **xUnit creates a new test-class instance for every test**, so each test gets a fresh `_forwarder`: no leftovers between tests |
| 174 `_ => Task.CompletedTask` | `ForwarderMiddleware` ends the pipeline: it forwards or returns 503 and never calls `next` (checked in `ForwarderMiddleware.cs`). Before, the Butler gave a loose delegate mock, which also just returns a finished `Task`. Same behavior, plainer code |
| 176 `_forwarder.Object` | **The** fix for the shared-mock trap. If this were `new Mock<IHttpForwarder>().Object`, the Subject would call a different stand-in, line 105 would check an untouched one, and the test would fail. Worse: in a test that only checks "was *not* called", it would *pass* for the wrong reason |
| 177 inline mock | The random factory is only used when there are 2+ destinations; these tests have 0 or 1. No one scripts or checks it, so it isn't kept in a field. Inline says "inert" |
| 171 `CreateMiddleware()` | Two tests build the Subject (`Constructor_Works` and two `sut`s). The same shape already exists in this project: `LimitsMiddlewareTests.CreateMiddleware()` |

**The rule that decided every field vs inline choice in all 7 files:**

```
Does the test touch this mock AFTER handing it to the Subject (Setup / Verify / Reset)?
   yes → field, and pass field.Object to the constructor
   no  → inline new Mock<T>().Object  (or NullLogger / a lambda when that says it more plainly)
```

**The other files, as a table:**

| File | Tests (per framework) | What was special | How it's handled |
|---|---|---|---|
| `StreamCopierTests` | 12 | Inherited the helper, never used it | `: TestAutoMockBase` removed |
| `ForwarderHttpClientFactoryTests` | 23 | Only the logger came from the Butler | `NullLogger` ×11 |
| `LoadBalancingPoliciesTests` | 6 | `Provide<IRandomFactory>` registered a hand-made fake | pass `RandomFactory` to the 2 policies that take one |
| `ForwarderMiddlewareTests` | 3 | Shared Courier stand-in (above) | field `_forwarder` + `CreateMiddleware()` |
| `ProxyPipelineInitializerMiddlewareTests` | 5 | `Provide<RequestDelegate>(…418…)`: "next" sets status 418, and tests assert 418 to prove the Clerk called `next` | the 418 lambda moves into `private static CreateMiddleware()`; `IOptionsMonitor<RequestTimeoutOptions>` stays an inert mock (never read in these tests: they use a number of milliseconds, not a named policy) |
| `HttpSysDelegatorMiddlewareTests` | 5 | (1) The Butler built a **real** `HttpSysDelegator` (§6, trap 3). (2) The random stand-in is scripted *after* the Subject is built (`SetupRandomToReturn`) | `new HttpSysDelegator(new Mock<IServer>().Object, NullLogger…)`; fields `_delegationFeature`, `_randomFactory` |
| `HttpSysDelegatorTests` | 17 | Four stand-ins wired into each other | drawn below |

**`HttpSysDelegatorTests`: the mock graph, now spelled out in the constructor** (lines 27–38):

```
server (local)            serverFeatures (local)             _serverDelegationFeature (FIELD)
Mock<IServer>  ──.Features──► Mock<IFeatureCollection> ──.Get<IServerDelegationFeature>()──► Mock<IServerDelegationFeature>
     │                                                                    ▲ tests: Setup, Verify(CreateDelegationRule), Reset
     ▼
_delegator = new HttpSysDelegator(server.Object, NullLogger<HttpSysDelegator>.Instance)     ← REAL object (the Subject)

_context.Features ──► _requestDelegationFeature (FIELD)  Mock<IHttpSysRequestDelegationFeature>
                          ▲ tests: SetupGet(CanDelegate), Verify(DelegateRequest)
```

`server` and `serverFeatures` are locals because nothing touches them after the constructor; they're plumbing
that gets `_serverDelegationFeature` into the delegator. `_serverDelegationFeature.Reset()` (used in two tests)
clears setups and recorded calls on the object the delegator already holds, exactly as the Butler's
`Mock<IServerDelegationFeature>().Reset()` did.

---

## §5 · The tests: what's real, what's fake, and what they prove

**One-line rule: a refactor of tests is proven by two things together: the same tests still pass, and each
rewritten test can still fail.**

**Object graph for `ForwarderMiddlewareTests.Invoke_Works`** (real = actual YARP/ASP.NET objects; fake = Moq):

| Object | Real or fake | Made where |
|---|---|---|
| `ForwarderMiddleware` (Subject) | **real** | `CreateMiddleware()` |
| `DefaultHttpContext`, `ClusterState`, `DestinationState`, `RouteModel`, `ReverseProxyFeature` | **real** | in the test body |
| `IHttpForwarder` | fake, **shared** (`_forwarder`) | field |
| `HttpMessageInvoker` wrapping a `Mock<HttpMessageHandler>` | real wrapper around a fake | test body (unchanged by this PR) |
| logger | real but silent (`NullLogger`) | `CreateMiddleware()` |
| `next`, `IRandomFactory` | lambda / inert fake | `CreateMiddleware()` |

**Arrange / act / assert:**
1. **Arrange:** build the context and cluster; script the Courier: "when `SendAsync` is called with *this* URL and
   *these* settings, pause, then return success" (`.Verifiable()`).
2. **Act:** `sut.Invoke(httpContext)`.
3. **Assert:** `_forwarder.Verify()` (the Courier was called exactly as scripted); concurrency counters went 0 → 1 →
   0; the proxied destination is `destination1`; a telemetry event was written.

**The proofs, per file** (each break was made in `src/`, the test went red, then undone **[004] [005] [006]**):

| File | Break in product code | Result | What the break proves about the rewrite |
|---|---|---|---|
| `StreamCopierTests` | `StreamCopier` writes 0 bytes | 12 red | Test still exercises the copier |
| `ForwarderHttpClientFactoryTests` | `EnabledSslProtocols = default` | 2 red | Test still reads the factory's handler |
| `LoadBalancingPoliciesTests` | Random policy always picks `[0]` | 2 red | The policy uses *our* `RandomFactory` |
| `ForwarderMiddlewareTests` | wrong URL to `SendAsync` | 2 red | `_forwarder` **is** the injected Courier |
| `ProxyPipelineInitializerMiddlewareTests` | Clerk doesn't call `next` | 4 red | The 418 `next` **is** the one passed in |
| `HttpSysDelegatorMiddlewareTests` | random choice ignored | 2 red | `_randomFactory` **is** the injected one |
| `HttpSysDelegatorTests` (a) | delegator ignores `IServer.Features` | 18 red | The server → features → delegation-feature chain reaches the verified stand-in |
| `HttpSysDelegatorTests` (b) | delegator never calls `DelegateRequest` | 8 red | `_requestDelegationFeature` **is** the one on the context |

(Counts are failures summed over both frameworks.)

**And the counts, before vs after:**

| | Before (main) | After (branch) | Evidence |
|---|---|---|---|
| ReverseProxy.Tests, Debug | 3,978 passed, 0 skipped | 3,978 passed, 0 skipped (×3 runs) | **[002] [007] [008]** |
| The 7 classes | 12 / 23 / 6 / 3 / 5 / 5 / 17 per framework | same | **[002] [004]–[006]** |
| All 4 test projects, Release (as CI builds) | identical totals, passes and skips | identical | **[011]** |

**What all this does NOT prove:**

| Not proven | Why |
|---|---|
| That *every* assertion still bites | One (or two) breaks per file is a sample. A specific assertion could have been weakened without any of these breaks noticing. (The diff shows no assertion lines changed, which is the other half of the argument) |
| Windows and Linux | Only macOS (arm64) was run. CI runs Windows, Ubuntu and macOS (lecture 002 §7) |
| That the tests are *good* | The refactor kept them exactly as strong as before, including a weak spot (§6, trap 5) |

---

## §6 · The traps met on the way

**One-line rule: every trap was about the same thing: making sure the new test builds the same object graph the
Butler built.**

| # | Trap | How it showed up | How it was handled |
|---|---|---|---|
| 1 | **Shared mock** (expected, from the brief) | Tests script/verify a mock after the Subject is built | The field rule (§4); each O2 break chosen to be detectable *only* through the shared mock |
| 2 | **Loose defaults** (expected) | Would `new Mock<T>()` answer unscripted calls the same as the Butler's mocks? | **Tested, not assumed:** a throwaway program outside the fork (`scratch/…/automock-defaults`) printed `Behavior=Loose DefaultValue=Empty` for both, and `Features is null` for both **[003]** |
| 3 | **The Butler doesn't mock everything** (found) | `HttpSysDelegatorMiddleware` takes a `HttpSysDelegator`: a **sealed concrete class**, not an interface. Moq can't fake a sealed class; the Butler built a real one | The rewrite builds a real one too. Faking it would have quietly tested less. Found by reading the constructor, confirmed by the same experiment |
| 4 | **Stale build** (found) | First break-it run: the SslProtocols break did **not** go red. A forced rebuild did | All break-it builds use `--no-incremental`. Lesson: a green from code that didn't run looks exactly like a real green |
| 5 | **A test that checks less than it seems** (found, left alone) | `ForwarderMiddlewareTests.NoDestinations_503` calls `_forwarder.Verify()`, but its only setup isn't `.Verifiable()`, so that line checks nothing. Its other asserts (503 + error feature) do guard the behavior | Kept as is: changing what a test checks is out of scope for a refactor. A possible PR-description note (§8) |
| 6 | **Documented test command fails** (found) | `dotnet test test/ReverseProxy.Tests/` → "could not find a part of the path …/test/ReverseProxy.Tests/test/ReverseProxy.Tests/…" | Use `--project "<absolute path to .csproj>"` (lecture 002) |
| 7 | **Transitive dependencies** (checked) | `Tests.Common` is also used by `Kubernetes.Tests` and `FunctionalTests`; dropping a package there could break them | Built the whole repo, ran all 4 test projects; same results as `main` **[007] [011]** |

**Rejected alternatives, with reasons:**

| Alternative | Why not |
|---|---|
| A new, smaller base class (e.g. `CreateMock<T>()`) | It would hide the dependencies again: the issue's whole point |
| Strict mocks (`MockBehavior.Strict`) | Unscripted calls would throw: tests would change what they check |
| Fixing the empty `Verify()` (trap 5) | Same: changes a test's meaning. Separate PR if wanted |
| Removing Moq too | Phase 2+, folder by folder, if maintainers want it (decision U2) |

---

## §7 · The upstream side

**One-line rule: the comment asks one question ("still wanted?") and describes a finished, bounded change; it
commits you to nothing more than answering follow-ups.**

CONTRIBUTING.md says, for `help wanted` issues: "Comment on an issue if you want to create a fix", and for
substantial changes, agree on the design first **[002]** §7. This change is mechanical, but the issue is from 2020
with 0 comments, so asking first is still right (decision D3: build first, comment after you understand it).

**Draft comment v2** (v1 is in `plan.md` Stage 4; this version says the change is ready). Rewrite it in your own
words before posting (G2 is yours):

> 1. *Is this still wanted?*
> 2. *I've prepared a first, bounded step that removes Autofac only.*
> 3. *The seven test classes that use `TestAutoMockBase` now build the class under test through its constructor,
>    with Moq mocks passed in explicitly; `TestAutoMockBase` and the `Autofac` / `Autofac.Extras.Moq` references
>    are deleted. Moq stays, and no product code changes.*
> 4. *The test counts are unchanged, and for each rewritten class I checked that the tests still fail when the
>    code they cover is broken.*
> 5. *If that sounds useful, I'll open a PR. Removing Moq could follow folder by folder later, if you'd like.*

| Sentence | What it does | What it commits you to |
|---|---|---|
| 1 | The real question; respects that the issue is old | Accepting "no" gracefully |
| 2 | Says the scope up front: Autofac only | Not expanding scope in this PR |
| 3 | Describes the pattern, so a maintainer can judge the style (decision U3 is theirs) | Being able to explain the field rule (§4) if asked |
| 4 | Shows you checked more than "it compiles" | Being able to show it: lecture 002 §4 |
| 5 | Offers, doesn't push; names the obvious next step without promising it | Nothing until they say yes |

**Likely answers, and what each would mean:**

| Answer | Meaning | Next |
|---|---|---|
| "Sure, send a PR" | Approved in principle | After G3, and once the one-active-PR rule allows: push `remove-autofac-275`, open the PR (plan Step 14) |
| "We'd prefer style X" (e.g. a helper base class) | U3 is theirs to decide | Adjust; a new branch row if it's a different approach |
| "Remove Moq too, in one go" | Scope change | Discuss phasing; don't silently grow the PR |
| No answer for 1–2 weeks | Common for old issues | Ask desktop/me to draft a short polite follow-up; or open the PR and link it (your call) |
| "Not wanted" | The work stops here | The cost was AI time, not yours; the lectures still count |

---

## §8 · Small calls left for you

None of these block anything. Defaults are what's on the branch now.

| Call | Options | Default |
|---|---|---|
| Mention trap 5 (the empty `Verify()`) in the PR description? | (a) one line under "Notes"; (b) not at all; (c) separate small PR later | (a): it shows care, and it's honest about what the refactor kept |
| Naming: `_delegationFeature` (middleware tests) vs `_requestDelegationFeature` (delegator tests) | Rename for consistency, or leave | Leave: each name is unambiguous in its own file |
| Inline `new` vs `Create…()` helpers in the small files | Both are defensible; reviewers may have a taste | Leave until a reviewer asks |

---

## §9 · Teach-back checklist

Say these back in your own words:

1. **What `AutoMock` did:** it built the class under test by reading its constructor, made a loose mock for each
   interface argument (a real object for a concrete one), and returned the *same* mock every time you asked for
   that type.
2. **Why the issue wants it gone:** `Create<T>()` hides what the class needs, and the "same mock" link between
   scripting and injecting is invisible; and it costs two packages.
3. **The new pattern:** call the real constructor yourself; pass `.Object` of each mock; use `NullLogger` where
   logging doesn't matter.
4. **The field rule:** a mock becomes a field when the test touches it *after* handing it over (Setup, Verify,
   Reset); otherwise it's inline. xUnit makes a fresh test-class instance per test, so fields don't leak.
5. **The shared-mock trap:** if the test scripts one mock and the Subject got another, `Verify` checks the wrong
   object: the test fails, or worse, passes for the wrong reason.
6. **The sealed-class finding:** `HttpSysDelegator` is sealed, so `AutoMock` built a real one; the rewrite must too.
7. **How the change is proven:** same test counts as `main`, plus a deliberate product-code break per file that
   turns the rewritten test red. Together: nothing lost, and the wiring still reaches the code.
8. **Why `--no-incremental`:** a stale build can run old code, so a break doesn't show and green means nothing.
9. **What isn't proven:** every single assertion (breaks are a sample), and Windows/Linux (CI covers those).
10. **What the comment asks:** one question (still wanted?), a bounded scope (Autofac only), and an offer, not a
    PR out of nowhere.
