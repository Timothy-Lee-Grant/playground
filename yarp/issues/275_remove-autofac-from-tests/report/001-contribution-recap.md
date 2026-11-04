# Contribution recap: removing Autofac from YARP's tests

> Source document for resume lines, interview stories and LinkedIn. Facts as of **2026-11-03**.
> Status: **Merged** into dotnet/yarp `main` on 2026-10-26 (squash commit `72f6bdf`), approved and merged by the
> code owner and main maintainer, MihaZupan, with no changes requested.

| | |
|---|---|
| Repository | [dotnet/yarp](https://github.com/dotnet/yarp): YARP ("Yet Another Reverse Proxy"), Microsoft's open-source reverse-proxy library built on ASP.NET Core |
| Issue | [dotnet/yarp#275](https://github.com/dotnet/yarp/issues/275): *Clean out usage of Autofac and Moq from tests*. Opened 2020-06-29 by a YARP team member, labeled `help wanted`; open for six years with no PR before this one |
| Pull request | [dotnet/yarp#3063](https://github.com/dotnet/yarp/pull/3063): *Remove Autofac From Tests* |
| Size | 11 files, +96 / −176 (net −80 lines), 4 commits (squash-merged). Test code and build files only; no product code changed |
| Language / stack | C#, .NET 8 and .NET 9 (multi-targeted tests), .NET 11 SDK, xUnit on Microsoft.Testing.Platform, Moq, Autofac.Extras.Moq (removed), the .NET Arcade build, Azure Pipelines CI on Windows, Linux and macOS |
| Timeline | Proposal comment on the issue 2026-10-09 → PR opened 2026-10-22 → approved and merged 2026-10-26 (**4 days**, one review round, "Thanks") |
| Label | `Type: Infrastructure` (added by the maintainer) |
| Context | Third merged code contribution in October 2026, and the first in YARP. Earlier: [dotnet/iot#2611](https://github.com/dotnet/iot/pull/2611) (bug fix, merged 2026-10-08) and [dotnet/iot#2627](https://github.com/dotnet/iot/pull/2627) (new Bmp3xx device binding, merged 2026-10-22) |

---

## 1. One-paragraph summary

I removed the **Autofac** dependency-injection container from the test suite of YARP, Microsoft's reverse-proxy
library. Seven test classes used a shared base class, `TestAutoMockBase`, which used Autofac's `AutoMock` to build
the class under test and automatically create a mock for every constructor parameter. That hid what each class
actually depends on and pulled two extra packages into the test projects. I rewrote the seven classes to construct
the class under test **directly through its constructor**, passing each Moq mock explicitly, then deleted the base
class and the `Autofac` and `Autofac.Extras.Moq` packages. It was a **pure refactor**: I proved behavior was
unchanged with identical test counts before and after (3,978 tests in the main test project, every other test
project equal to `main`), a 0-warning Release build, and "break-it" checks showing each rewritten test class still
fails when the code it covers is broken. The maintainer approved and merged it four days after it was opened.

---

## 2. Why this contribution

- **A real, long-standing request from the team.** The issue was opened by a YARP team member in 2020 and labeled
  `help wanted`; nobody had picked it up in six years.
- **Scoped deliberately.** The issue asks to remove both Autofac and Moq. Moq is used in ~25 test files and
  replacing it is a design choice (hand-written fakes vs. keeping a mocking library) the maintainers should make, so
  I proposed a bounded first step on the issue: **Autofac only**, which fully removes one of the two dependencies.
  The PR says "Contributes to #275", leaving the issue open for the Moq part.
- **Why it matters for me:** a change in a Microsoft-maintained ASP.NET Core project, centered on dependency
  injection, constructor design and test quality, all core backend .NET skills. It also shows I can work in a large,
  unfamiliar codebase without breaking anything.

---

## 3. What changed

| Before | After |
|---|---|
| Test classes inherit `TestAutoMockBase`; `Create<T>()` asks Autofac's `AutoMock` to build the subject, and `Mock<TDep>()` fetches the mock it made behind the scenes | Each test builds the subject with `new T(dep1.Object, dep2.Object, …)`, so every dependency is visible in the test |
| `Autofac` and `Autofac.Extras.Moq` referenced by two test projects, versions pinned in `eng/Versions.props` | Both packages and their version entries removed; `TestAutoMockBase.cs` (99 lines) deleted |
| Moq used through AutoMock | Moq used directly (unchanged library, explicit use) |

The seven rewritten classes: `StreamCopierTests`, `ForwarderHttpClientFactoryTests`, `LoadBalancingPoliciesTests`,
`ForwarderMiddlewareTests`, `ProxyPipelineInitializerMiddlewareTests`, `HttpSysDelegatorMiddlewareTests`,
`HttpSysDelegatorTests` (71 tests per target framework).

---

## 4. Technical details worth telling

- **A hidden real object.** `HttpSysDelegator` is `internal sealed`, and Moq can't mock a sealed class, so AutoMock had
  silently been **constructing a real one** instead of a mock. A naive rewrite would have changed what the tests
  exercise. I made it explicit (`new HttpSysDelegator(mockServer.Object, NullLogger…)`), so the middleware tests
  still run the same real code, and called it out for the reviewer.
- **Proving the mocks behave the same.** I checked by experiment that AutoMock's mocks are loose with
  `DefaultValue.Empty`, exactly what `new Mock<T>()` gives, so replacing one with the other can't change test
  behavior.
- **A test that checked nothing.** While rewriting, I found that one test (`ForwarderMiddlewareTests.NoDestinations_503`)
  calls Moq's `Verify()` without any setup marked `Verifiable()`, so the assertion is a no-op. I kept it out of
  scope (the PR is a pure refactor) and flagged it in the PR description as a possible follow-up.
- **Dependency removal done cleanly.** Package references removed from both test `.csproj` files and the central
  `eng/Versions.props`; confirmed no remaining Autofac reference anywhere in the repo.

---

## 5. Verification: proving a refactor changes nothing

| Check | Result |
|---|---|
| Main test project (`Yarp.ReverseProxy.Tests`), net8.0 + net9.0 | 3,978 passed before and after (1,989 per framework); the 7 classes' per-class counts unchanged |
| Full Release build + every test project, branch vs. `main` | Identical totals in all 4 test projects on both frameworks (incl. Kubernetes and functional tests); **0 warnings** under the repo's warnings-as-errors build |
| Break-it proofs | 8: for each rewritten class, I broke one line of the product code it covers and confirmed the class went red, then reverted |
| CI on the PR | Green on Windows, Ubuntu and macOS; warning counts identical to `main`'s; CLA check passed |
| Formatting / hygiene | `git diff --check`, `dotnet format whitespace` on changed files, upstream-style commit messages |

---

## 6. Engineering process

- **Read the repo's rules and etiquette first:** CONTRIBUTING (comment on help-wanted issues before taking them;
  build and all tests must pass), merged-PR style (short imperative titles, short factual descriptions,
  squash merges), CODEOWNERS (reviewer auto-requested).
- **Proposed before building publicly:** a short comment on the issue stating the bounded scope and asking whether
  it was still wanted. After two weeks with no reply, opened the PR, which is normal for a help-wanted issue.
- **Baseline first:** recorded test counts on `main` (per project, per framework, per class) before changing
  anything, so "unchanged" could be shown, not claimed.
- **Small, reviewable commits:** simple subjects → middleware → `HttpSysDelegator` graph → delete the base class
  and packages, each step built and tested.
- **Caught my own tooling traps:** one break-it check first stayed green because of a **stale incremental build**;
  every proof was rerun with forced rebuilds. A proposed "unused `using`" cleanup turned out to be wrong, because the
  `Yarp.Tests.Common` namespace spans two test projects, and the build caught it before it reached the PR.
- **Pre-PR checklist** before pushing: up to date with `upstream/main`, issue thread re-checked, scope matches the
  plan, Release build + all tests vs. `main`, reviewer-style read-through of the full diff, PR text checked
  against the evidence.

---

## 7. Skills demonstrated

- C# / .NET: constructor injection, dependency-injection containers vs. explicit wiring, `internal sealed` types,
  `NullLogger<T>`, multi-targeted test projects
- Testing: Moq (loose vs. strict mocks, `Setup`/`Verify`/`Verifiable`), xUnit, behavior-preserving refactoring,
  mutation-style "break-it" checks, test-count baselines
- Build and CI: .NET Arcade build scripts, central package versioning (`Versions.props`), warnings-as-errors,
  Azure Pipelines CI across three operating systems
- Open-source practice: scoping a large issue into a mergeable first step, proposing on the issue, matching a
  repo's conventions, writing a reviewer-friendly PR description

---

## 8. Resume line drafts (pick and edit; keep them true)

- *Removed the Autofac DI container from the test suite of Microsoft's YARP reverse proxy (dotnet/yarp#3063),
  rewriting 7 test classes to use explicit constructor injection; merged by the maintainer in 4 days with no
  changes requested.*
- *Delivered a behavior-preserving refactor in a production .NET codebase (3,978 tests in the main test project alone): identical test counts across all
  projects and frameworks, 0-warning Release build, and per-class "break-it" checks proving each rewritten test
  still catches regressions.*
- *Scoped a six-year-old "help wanted" issue (remove Autofac and Moq) into a bounded first PR that fully removed one
  dependency and 80 net lines, leaving a clear path for the follow-up.*
- *Found that the old test helper silently constructed a real `internal sealed` class instead of a mock, and kept
  that behavior explicit in the rewrite so test coverage stayed identical.*

**Combined line for October 2026** (if one line has to cover all three): *Three merged code contributions to
Microsoft .NET open-source repositories in one month (dotnet/iot ×2, dotnet/yarp): a bug fix with tests, a new
sensor driver with 89 hardware-free tests, and a test-infrastructure refactor removing a third-party dependency.*

---

## 9. Interview story (STAR, ~1 minute)

- **Situation:** YARP's tests built objects through an Autofac "auto-mocking" helper, which hid each class's
  dependencies; the team had asked for its removal since 2020.
- **Task:** remove it without changing what any test checks, in a codebase I hadn't worked in.
- **Action:** baselined every test count; proved the old and new mocks behave identically; rewrote 7 classes to
  explicit constructors in small commits; discovered the helper had been building a real sealed class and preserved
  that; proved each rewritten class still fails when its code breaks; matched the repo's conventions and ran a full
  CI-style check before the PR.
- **Result:** merged in 4 days with no requested changes; two packages and 80 net lines removed; Moq removal left as a
  clearly scoped follow-up.

Likely follow-up questions: *Why not remove Moq too?* (scope and a design choice for the maintainers) · *How did you
know nothing changed?* (counts + break-it proofs + CI) · *What's wrong with auto-mocking?* (hides dependencies,
couples tests to a container, surprises like the sealed class).

---

## 10. Links

- PR: https://github.com/dotnet/yarp/pull/3063 · merge commit: https://github.com/dotnet/yarp/commit/72f6bdf
- Issue: https://github.com/dotnet/yarp/issues/275 (remains open for the Moq follow-up)
- Proposal comment: https://github.com/dotnet/yarp/issues/275#issuecomment-6073544881
