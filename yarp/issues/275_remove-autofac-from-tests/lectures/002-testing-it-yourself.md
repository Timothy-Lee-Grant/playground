# Lecture 002 · Testing it yourself: checking the Autofac removal before the PR

| | |
|---|---|
| **Issue** | dotnet/yarp#275, phase 1 |
| **Branch / commit** | `remove-autofac-275` @ `1665ced5` (local, not pushed); base `upstream/main` @ `2aa3d835` |
| **Date** | 2026-10-07 (CLI). Every command below was run on this Mac on that date; expected output comes from `shared/evidence/` files (named in **[brackets]**) |
| **Prerequisites** | Lecture 001 §0 (you've seen one test go green → red → green) |
| **How to read this** | **It's a procedure: do it, don't just read it.** Fast path (~30 min at the keyboard): §0, §1, §2, §3 (steps C and D), §4 (pick 3 breaks), §6, §9. Full path (~60 min): all of it |
| **Goal** | You can confirm, with no AI in the loop, that this change is correct and ready, and say what each check is evidence *of* |

**Where to run commands:** every command block says **from `yarp/`** (the clone) or **from the project root**
(the folder that contains `yarp/` and `link/`). Copy-paste works as written.

---

## §0 · What "ready for a PR" means for this change

**One-line rule: ready = it builds clean, every test passes with the same counts as `main`, each rewritten test can
still fail, and the diff contains only what it should.**

| # | Done when | Checked in |
|---|---|---|
| 1 | Whole repo builds with 0 warnings, 0 errors | §2 |
| 2 | `ReverseProxy.Tests`: 3,978 passed, 0 failed, 0 skipped (1,989 per framework) | §3 |
| 3 | All 4 test projects: same totals and skips as `main`, in Release (what CI builds) | §3, §5 |
| 4 | Each of the 7 rewritten classes goes red when its product code is broken | §4 |
| 5 | Diff touches only the 7 test files, `TestAutoMockBase.cs`, 2 `.csproj` files, `eng/Versions.props` | §6 |
| 6 | No Autofac anywhere; commit messages have no `#123`, links or `@names` | §6 |
| 7 | You can say what CI will check that you can't (Windows, Linux) | §7 |

---

## §1 · Set up the shell

**One-line rule: always use the repo's own SDK (`yarp/.dotnet/`), never the Mac's system `dotnet`.**

**Why:** `yarp/global.json` pins SDK `11.0.100-rc.1.26420.103`. Your Mac's system `dotnet` is
`11.0.100-preview.5`, a different (older) build. `./restore.sh` downloaded the pinned SDK into `yarp/.dotnet/`
(760 MB, on the SSD) **[002]**. Two ways to use it:

From `yarp/`:
```bash
cd yarp
git status -sb                 # expect: ## remove-autofac-275...upstream/main [ahead 4]
                               #         ?? .DS_Store   ← Finder's file, never committed; ignore it
./.dotnet/dotnet --version     # expect: 11.0.100-rc.1.26420.103
```

Or put the repo SDK first on your `PATH` for this terminal window only:
```bash
source activate.sh             # (zsh: works; verified)
which dotnet                   # expect: …/yarp_project/yarp/.dotnet/dotnet
deactivate                     # back to the system dotnet when you're done
```

This lecture writes `./.dotnet/dotnet` everywhere, so it works whether or not you activated.

If `yarp/.dotnet/` is missing (e.g. a fresh clone): `./restore.sh` from `yarp/` (~45 s here; downloads the SDK
the first time) **[002]**.

---

## §2 · Build from clean

**One-line rule: after switching branches or editing product code, build with `--rebuild` or
`--no-incremental`; a stale build can make a test run old code.**

From `yarp/`:
```bash
./build.sh --rebuild
```

Expected (end of output) **[007]**:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```
Takes ~5–20 s on this Mac (it builds 33 projects; the compiler stays warm between builds).

**What warnings mean here: in `./build.sh` (and in CI), a warning *is* an error.** The Arcade scripts default
to `warn_as_error=true` and pass `/warnaserror` to MSBuild (`eng/common/build.sh:86`, `eng/common/tools.sh:584–622`).
So an unused variable you introduce fails the PR's CI build, not just a code review. Plain
`./.dotnet/dotnet build` does **not** do this: it prints warnings and still succeeds. That's why the final check
uses `./build.sh`, and why "0 Warning(s)" is the bar.

**Why "from clean" matters, concretely:** during the run, a deliberate break in `ForwarderHttpClientFactory.cs`
didn't turn its test red, because the incremental build left the *old* `Yarp.ReverseProxy.dll` next to the tests.
A `--no-incremental` build fixed it **[004]** §2. A green you didn't earn looks exactly like a real one.

---

## §3 · Run the tests, narrowest first

**One-line rule: one method → one class → the project → everything; each step is faster to read when it fails.**

All from `yarp/`. `T` is just a shell variable so the lines stay short; set it once per terminal:
```bash
T="$PWD/test/ReverseProxy.Tests/Yarp.ReverseProxy.Tests.csproj"
```
**It must be an absolute path.** `dotnet test test/ReverseProxy.Tests/` (the form in older notes) fails here with
"Could not find a part of the path '…/test/ReverseProxy.Tests/test/ReverseProxy.Tests/…'" **[002]** §3.

**A. One method** (~2 s):
```bash
./.dotnet/dotnet test --project "$T" --framework net9.0 \
  --filter-method Yarp.ReverseProxy.Forwarder.Tests.ForwarderMiddlewareTests.Invoke_Works
```
Expect `total: 1, failed: 0` **[011]** §D. The name must be **full** (namespace.class.method):
`--filter-method Invoke_Works` silently runs **0** tests.

**B. One class** (~2 s; runs on both frameworks, so counts are ×2):
```bash
./.dotnet/dotnet test --project "$T" --filter-class Yarp.ReverseProxy.Delegation.HttpSysDelegatorTests
```
Expect `total: 34, failed: 0` **[006]**.

The 7 rewritten classes and their expected totals (both frameworks) **[002] [004]–[006]**:

| Full class name (for `--filter-class`) | Total |
|---|---|
| `Yarp.ReverseProxy.Forwarder.Tests.StreamCopierTests` | 24 |
| `Yarp.ReverseProxy.Forwarder.Tests.ForwarderHttpClientFactoryTests` | 46 |
| `Yarp.ReverseProxy.LoadBalancing.Tests.LoadBalancingPoliciesTests` | 12 |
| `Yarp.ReverseProxy.Forwarder.Tests.ForwarderMiddlewareTests` | 6 |
| `Yarp.ReverseProxy.Model.Tests.ProxyPipelineInitializerMiddlewareTests` | 10 |
| `Yarp.ReverseProxy.Delegation.HttpSysDelegatorMiddlewareTests` | 10 |
| `Yarp.ReverseProxy.Delegation.HttpSysDelegatorTests` | 34 |

Note the namespaces aren't uniform (some have `.Tests`, the Delegation ones don't). A wrong name doesn't error: it
runs 0 tests. **Always check `total:` isn't 0.**

**C. The whole test project** (~5 s):
```bash
./.dotnet/dotnet test --project "$T"
```
Expect **[007] [008]**:
```
  total: 3978
  failed: 0
  succeeded: 3978
  skipped: 0
```
`--filter-class` can be repeated to run several classes at once.

**D. Everything, the way CI builds it** (Release; ~20 s, including the build):
```bash
./build.sh --configuration Release --test
```
Expect `0 Warning(s)`, `0 Error(s)`, exit 0. Per-project results are in `artifacts/log/Release/*_arm64.log`; to see
the totals:
```bash
for f in artifacts/log/Release/*_arm64.log; do echo "$(basename $f .log): $(grep -E '^\s+(total|failed|skipped):' $f | tr -s ' ' | tr '\n' ' ')"; done
```
Expected **[011]**:
```
Yarp.Application.Tests_net9.0_arm64:           total: 27   failed: 0  skipped: 0
Yarp.Kubernetes.Tests_net8.0_arm64:            total: 93   failed: 0  skipped: 2
Yarp.Kubernetes.Tests_net9.0_arm64:            total: 93   failed: 0  skipped: 2
Yarp.ReverseProxy.FunctionalTests_net8.0_arm64: total: 269  failed: 0  skipped: 34
Yarp.ReverseProxy.FunctionalTests_net9.0_arm64: total: 269  failed: 0  skipped: 34
Yarp.ReverseProxy.Tests_net8.0_arm64:          total: 1989 failed: 0  skipped: 0
Yarp.ReverseProxy.Tests_net9.0_arm64:          total: 1989 failed: 0  skipped: 0
```
(Exact spacing differs.) The skips are declared in the test source: a known-issue skip, "HTTP/2 over TLS not
supported on macOS", and Windows-only HTTP.sys tests. `main` skips exactly the same ones (§5).

**Don't use `./test.sh` on its own first.** It only *runs* tests (it's `eng/common/build.sh --test`) and doesn't
build them; with nothing built in that configuration it "fails" in 1 second (§8).

**Reading a failure:** look for lines starting `failed <TestName>`, then the exception under it. A `Moq.MockException
… This setup was not matched` means the Subject didn't call a stand-in the way the test scripted (lecture 001 §0).
An `Assert.Equal() Failure` shows `Expected:` and `Actual:`. The `at …Tests.cs:NNN` line shows which line of the
test failed.

---

## §4 · Prove the tests can fail

**One-line rule: a test that can't go red proves nothing, so break the product code it guards, watch it fail,
and undo.**

**Purpose here specifically:** a refactor of tests can silently disconnect a test from the code (e.g. the test
scripts one mock while the Subject got another). Same counts don't catch that; a red run does.

**The loop, for each row below** (from `yarp/`):
1. Open the file at the line, make the change in your editor, save.
2. `git diff --stat`: exactly that one `src/` file should be listed.
3. Build **without** incremental, then run the class:
   ```bash
   ./.dotnet/dotnet build "$T" --no-incremental
   ./.dotnet/dotnet test --project "$T" --no-build --filter-class <class from the table>
   ```
4. Expect the "Red" column. Then undo and confirm clean:
   ```bash
   git restore src/        # throws away your edit (only in src/, only uncommitted changes)
   git status --short      # expect only: ?? .DS_Store
   ```

| # | File:line (in `src/ReverseProxy/`) | Change this… | …to this | Class to run | Red (both frameworks) |
|---|---|---|---|---|---|
| 1 | `Forwarder/StreamCopier.cs:102` | `buffer.AsMemory(0, read)` | `buffer.AsMemory(0, 0)` | `…Forwarder.Tests.StreamCopierTests` | 12 failed |
| 2 | `Forwarder/ForwarderHttpClientFactory.cs:88` | `= newConfig.SslProtocols.Value;` | `= default;` | `…Forwarder.Tests.ForwarderHttpClientFactoryTests` | 2 (`CreateClient_ApplySslProtocols_Success`) |
| 3 | `LoadBalancing/RandomLoadBalancingPolicy.cs:30` | `availableDestinations[random.Next(availableDestinations.Count)]` | `availableDestinations[0]` | `…LoadBalancing.Tests.LoadBalancingPoliciesTests` | 2 (`PickDestination_Random_Works`) |
| 4 | `Forwarder/ForwarderMiddleware.cs:86` | `destinationModel.Config.Address,` | `destinationModel.Config.Address + "x",` | `…Forwarder.Tests.ForwarderMiddlewareTests` | 2 (`Invoke_Works`) |
| 5 | `Model/ProxyPipelineInitializerMiddleware.cs:67` | `? _next(context)` | `? Task.CompletedTask` | `…Model.Tests.ProxyPipelineInitializerMiddlewareTests` | 4 |
| 6 | `Delegation/HttpSysDelegatorMiddleware.cs:55` | `destinations[random.Next(destinations.Count)]` | `destinations[0]` | `…Delegation.HttpSysDelegatorMiddlewareTests` | 2 (`…ProxyChosen…`) |
| 7 | `Delegation/HttpSysDelegator.cs:38` | `= server.Features?.Get<IServerDelegationFeature>();` | `= null;` | `…Delegation.HttpSysDelegatorTests` | 18 |
| 8 | `Delegation/HttpSysDelegator.cs:105` | `requestDelegationFeature.DelegateRequest(queueState.Rule);` | `// requestDelegationFeature.DelegateRequest(queueState.Rule);` | `…Delegation.HttpSysDelegatorTests` | 8 |

(`…` = `Yarp.ReverseProxy.`; the full names are in §3's table. Line numbers are at `1665ced5`; results from
**[004] [005] [006]**.)

**What each break is evidence of** (why these lines, not random ones):

| # | If it goes red, you've shown… |
|---|---|
| 1–2 | The rewritten class still runs the real copier / factory (those files barely changed) |
| 3 | The policy uses the test's `RandomFactory`: the one the rewrite now passes explicitly |
| 4 | `_forwarder` (the field) is the stand-in inside the middleware: the shared-mock wiring |
| 5 | The 418 `next` in `CreateMiddleware()` is the one the middleware calls |
| 6 | `_randomFactory` (the field) is the one the middleware uses |
| 7 | The chain `IServer → IFeatureCollection → _serverDelegationFeature` reaches the delegator |
| 8 | `_requestDelegationFeature` is the one on the `HttpContext` that the delegator calls |

**Syntax note** (row 7): `server.Features?.Get<…>()` uses `?.`, the "null-conditional" operator. Written out:
`if (server.Features == null) result = null; else result = server.Features.Get<…>();`.

**If a break does NOT go red:** first suspect the build: did you use `--no-incremental`? Check `git diff --stat`
shows your edit. Only then suspect the test.

---

## §5 · Compare with `main`

**One-line rule: "all green" means little on its own; "the same as `main`" is the claim.**

**Purpose:** a refactor must not add, lose, or newly skip tests. Comparing totals *and* skips with `main` is how you
show that.

Recipe used for **[011]** (from `yarp/`; needs a clean tree):
```bash
git status --short                               # must show nothing but ?? .DS_Store
git switch --detach upstream/main                # look at main's code without making a branch
./build.sh --configuration Release --rebuild --test
for f in artifacts/log/Release/*_arm64.log; do echo "$(basename $f .log): $(grep -E '^\s+(total|failed|skipped):' $f | tr -s ' ' | tr '\n' ' ')"; done
git switch remove-autofac-275                    # back to the branch
./build.sh --configuration Release --rebuild --test    # rebuild: artifacts/ now holds main's build
```

Expected: the two lists are **identical** (the table in §3 D) **[011]**.

**Why `--detach`:** `git switch --detach <commit>` puts the clone *at* that commit without creating or moving a
branch (git calls it "detached HEAD"). Nothing can be committed by accident (the hook also refuses), and
`git switch remove-autofac-275` brings you back.

**Why `--rebuild` both times:** `artifacts/` is shared by all branches. After a switch, the build output belongs to
the *other* branch until you rebuild.

**Alternative (unverified here):** `git worktree add ../scratch/main-baseline upstream/main` gives a second folder
with `main` checked out, so you can compare without switching. It needs its own `./restore.sh` (another 760 MB),
which is why the switch recipe was used.

**Debug-only quick check** (if you just want the one number): on each side, `./.dotnet/dotnet test --project "$T"`
→ 3,978 both times **[002] [007]**.

---

## §6 · Review the diff yourself

**One-line rule: read the diff the way the reviewer will, before they do.**

From `yarp/`:
```bash
git log --oneline upstream/main..HEAD            # the 4 commits
git diff --stat upstream/main...HEAD             # 11 files, +96 −176
```
Expected **[008]**:
```
1665ced5 Remove TestAutoMockBase and the Autofac test dependencies
f5f16fef Build HttpSysDelegator and its mock graph explicitly in tests
4526c6a5 Construct middleware under test directly instead of via AutoMock
9db5c959 Construct simple test subjects directly instead of via AutoMock
```

(`..` vs `...`: `A..B` in `git log` = "commits in B not in A". `A...B` in `git diff` = "changes on B since it split
from A". For a branch made from `upstream/main`, both mean "my work".)

Then go file by file: `git diff upstream/main...HEAD -- <path>`, or commit by commit: `git show 9db5c959`.

**Checklist (each has a command):**

| Check | Command (from `yarp/`) | Expect |
|---|---|---|
| Only intended files | `git diff --name-only upstream/main...HEAD \| grep -v -E '^test/\|^eng/Versions.props$'` | nothing |
| No Autofac left | `git grep -n -i autofac` | nothing (exit code 1) |
| No whitespace errors | `git diff --check upstream/main...HEAD` | nothing |
| No product code touched | `git diff --stat upstream/main...HEAD -- src/` | nothing |
| Commit messages clean | `git log --format=%B upstream/main..HEAD \| grep -n -E '#[0-9]+\|github\.com\|@[A-Za-z]\|Co-authored-by'` | nothing |
| No debug leftovers | read the diff: no `Console.WriteLine`, commented-out code, `// TODO` you added | — |
| No formatting churn | in each file's diff, every changed line should be part of the change, not re-indentation | — |

All seven passed on 2026-10-07 **[007] [008]** (message check re-run while writing this lecture).

**What to look at hardest:** the `.Object` passed into each constructor. For every mock that a test calls
`Setup`/`Verify`/`Reset` on, the constructor must receive *that field's* `.Object` (lecture 001 §4, the field rule).

---

## §7 · What CI will run that you can't

**One-line rule: CI builds Release and runs every test on Windows, Ubuntu and macOS; you've covered macOS only.**

`azure-pipelines-pr.yml` (runs on every PR, any target branch) has three jobs:

| Job | Runs | You covered it? |
|---|---|---|
| Windows (`windows.vs2022.amd64.open`) | `eng\common\cibuild.cmd -configuration Release -prepareMachine` | **No.** Matters most for the HTTP.sys tests: on Windows the functional `HttpSysDelegationTests` really run (here they're skipped). The unit tests changed here (`HttpSysDelegator*Tests`) use mocks and already pass on macOS **[002]** §6 |
| Ubuntu (`ubuntu-latest`) | `eng/common/cibuild.sh --configuration Release --prepareMachine` | **No.** Your Linux desktop can't run .NET 11. Codespaces could (unverified) |
| macOS (`macOS-latest`) | same as Ubuntu | **Yes, approximately:** §3 D is the same configuration and test run (CI also packs and publishes) |

`cibuild` = restore + build + **test** + pack, with CI settings. Its test step is what `./build.sh --configuration
Release --test` does locally.

**What you can do instead of Windows/Linux:** nothing in this change is platform-specific (only test code; no
`#if`, no OS checks). That's an argument, not a proof. **CI is the proof**, and it runs on your PR before any
review. If a job fails, its logs are downloadable from the PR's "Checks" (the `TestResults` artifacts).

---

## §8 · Troubleshooting (errors actually met during this work)

| You see | Cause | Do |
|---|---|---|
| `Could not find a part of the path '…/test/ReverseProxy.Tests/test/ReverseProxy.Tests/Yarp.ReverseProxy.Tests.csproj'` | `dotnet test` given a relative path (cause inside the SDK not established) | Use `--project "$PWD/test/ReverseProxy.Tests/Yarp.ReverseProxy.Tests.csproj"` **[002]** |
| `total: 0` | Filter name wrong or not full (`--filter-method Invoke_Works`, or a namespace without `.Tests`) | Use the full names from §3's table **[011]** |
| `./test.sh …`: exit 1 in ~1 s; 7× `XUnit : error : Tests failed`; log says `…/Release/net9.0/Yarp.ReverseProxy.Tests: No such file or directory` | `test.sh` doesn't build; nothing built in that configuration yet | `./build.sh --configuration Release --test` **[011]** §D |
| A product-code break doesn't turn the test red | Stale build: old DLL next to the tests | `--no-incremental` build; check `git diff --stat` shows the edit **[004]** §2 |
| `dotnet --version` says `preview.5` | Using the Mac's system SDK | `./.dotnet/dotnet`, or `source activate.sh` (§1) |
| `.DS_Store` in `git status` | macOS Finder writes it when you open the folder | Ignore; never `git add .` (add files by name) |
| `git switch` refuses: "would be overwritten" | Uncommitted edits (e.g. a break from §4 not undone) | `git restore src/` if it's a break; otherwise stop and look at `git status` |
| `Autofac*.dll` files still in `artifacts/bin/Yarp.ReverseProxy.Tests/…` | Left over from builds before the change (a rebuild only cleans files it would produce now) | Harmless: no `.deps.json` mentions them, so nothing loads them **[007]** §3. Deleting `artifacts/` also removes them |

---

## §9 · Go / no-go

**One-line rule: tick every box yourself, then tell desktop or the CLI "G3 is open" (after your teach-back).**

- [ ] `./build.sh --rebuild` → 0 warnings, 0 errors (§2)
- [ ] `ReverseProxy.Tests` → 3,978 / 0 failed / 0 skipped (§3 C)
- [ ] Release, all projects → the table in §3 D (§3 D)
- [ ] Same as `main` (§5), or you accept **[011]** as that evidence
- [ ] At least 3 rows of §4 done by hand, each went red, `git status` clean afterwards
- [ ] §6 checklist: all "nothing"
- [ ] You can say what CI covers that you didn't (§7)
- [ ] You can explain the field rule and the sealed-class finding (lecture 001 §4, §6)
- [ ] One-active-PR rule allows it (your iot PR's state)

Then the order is: comment upstream (G2), teach-back (G3), push (`git push -u origin remove-autofac-275`; the first
push of this branch, which also makes it track your fork), open the PR.

---

## §10 · Teach-back checklist

1. **Repo SDK, not system SDK:** `global.json` pins the version; `./restore.sh` puts it in `yarp/.dotnet/`.
2. **Build from clean after a switch or a product-code edit,** because `artifacts/` is shared and incremental builds
   can leave old DLLs; a stale build can fake a green.
3. **Narrowest first:** method → class → project → `./build.sh --configuration Release --test`; and always check
   `total:` isn't 0.
4. **`./test.sh` doesn't build;** `./build.sh … --test` does.
5. **A test that can't go red proves nothing:** break the product line it guards, see red, `git restore src/`.
6. **The breaks were chosen to test the wiring:** each one can only be caught through a mock the rewrite now
   passes explicitly.
7. **"Same as `main`" is the real claim:** totals *and* skips, compared on both sides with the same command.
8. **Diff review:** only intended files, no Autofac, no whitespace errors, no `src/` changes, clean messages.
9. **CI covers Windows and Ubuntu** in Release; you covered macOS. Nothing here is platform-specific, but CI is
   the proof.
