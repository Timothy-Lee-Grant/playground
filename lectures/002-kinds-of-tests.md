# Lecture 002: The kinds of tests a professional engineer should know

> **Prompted by:** Timothy, 2026-10-01: "I've heard of scenario, integration, unit, end-to-end and smoke tests. What
> are all the different things I can test, and which should I know to be a professional engineer at a company like
> Microsoft?" · **Date:** 2026-10-01 · **Examples from:** dotnet/iot (`main` @ `95384e7`), your #2328 change, and
> yarp#275 · **Pairs with:** [`../iot/iot_concepts/004-ci-pipelines-how-they-are-set-up.md`](../iot/iot_concepts/004-ci-pipelines-how-they-are-set-up.md)
> (where these tests run) · **Reading time:** about 30 minutes.

Written for any junior engineer. The names below are industry vocabulary, and **teams use them loosely**: one team's
"integration test" is another's "component test". What matters is being able to say *what's real, what's fake, and
what question the test answers*. Microsoft's own level names (L0–L4) are quoted from its published guidance.

**The whole thing in six lines**

1. Every test is a **question** about the software. The kinds of test differ in *which question* and *how much of the
   real system* is involved.
2. **Axis 1, scope:** unit → component → integration → contract → end-to-end. More real parts means more
   confidence that it works for real, but slower, flakier and harder to diagnose.
3. **Axis 2, purpose:** does it work (functional), does it still work (regression), is it alive (smoke), is it fast
   enough (performance), does it survive failures (resilience), is it safe (security), does it stay compatible.
4. Some checks aren't tests but do the same job: the compiler, analyzers, linters, code review.
5. Large companies also test **after** shipping: canaries, rings, feature flags, monitoring.
6. A test is only worth what it would catch. A test that passes for the wrong reason, or fails at random, costs more
   than it gives.

---

## 0. You've already written three kinds

| Thing you've seen | Its kind | Why |
|---|---|---|
| Your #2328 tests: real `GpioButton` over a fake GPIO driver | **Unit** tests (with a test double), and now **regression** tests | One class is real; its hardware dependency is faked. They also guard a bug that was fixed, which is what "regression test" means |
| `System.Device.Gpio.Tests` on Raspberry Pis via Helix (disabled, #2406) | **Integration** tests against real hardware ("hardware-in-the-loop") | The real library talks to a real Linux GPIO driver and real pins |
| `Markdown_Checks_CI` (markdownlint, link check) | **Static checks**, not tests | Nothing runs; files are inspected |

Hold those three as anchors. Everything else in this lecture is a variation on one of them.

---

## 1. The idea: a test is a question with a price

**The rule: name a test by what's real in it and what question it answers.**

Every test costs something to write, to run (seconds or hours, on every PR or once a night), and to keep (it breaks
when code changes, sometimes for no good reason). It pays back by catching a problem earlier than a user would.
Choosing tests is choosing which questions are worth that price, and asking each question at the **cheapest level
that can actually answer it**.

```
                  what the test asks about
                ┌───────────────────────────────────────────────────────────────┐
                │ correct?  still correct?  alive?  fast?  survives failure?    │
                │ secure?  compatible?  accessible?                             │
                └───────────────────────────────────────────────────────────────┘
                                         ×
   how much is real:  one class ── a component ── several parts ── the whole deployed system
                      (unit)      (component)     (integration)   (end-to-end)
```

The two axes are independent. "A performance test" says nothing about scope: you can benchmark one method (unit
scope) or load-test a whole service (end-to-end scope).

---

## 2. Axis 1: scope (how much of the system is real)

| Kind | What's real | What's faked | Speed | Example |
|---|---|---|---|---|
| **Unit** | One class or function | Everything it depends on that's slow, external or nondeterministic (hardware, network, disk, clock) | Milliseconds | `GpioButton` + fake driver (#2328) |
| **Component** | One module or service, in-process | Its external dependencies (database, other services) | Fast | An ASP.NET Core app run in memory with `WebApplicationFactory`, its database replaced by an in-memory one |
| **Integration** | Two or more real parts talking for real | Usually only what's out of scope | Seconds to minutes | GPIO library + real Linux driver + real pins (Helix); a repository class against a real SQL Server in a container |
| **Contract** | The *agreement* between two services | The other service | Fast | "The orders API still returns the fields the billing service reads." Catches breaking a consumer without running both |
| **End-to-end (E2E) / system** | The whole deployed system, through its real entry points | Nothing (or only third parties) | Minutes | A browser test that logs in, places an order and checks the confirmation email |

**What "unit" does *not* mean:** "tests one method". A unit test can exercise several methods or classes; the point
is that it runs **in memory, fast and deterministically**, with nothing external. Your #2328 tests use the real
`GpioButton` *and* the real `ButtonBase` and `GpioController`; only the driver is fake. Still unit tests.

### 2.1 Why not test everything end-to-end?

| | Unit | Integration | End-to-end |
|---|---|---|---|
| Confidence it works for real | Lowest | Middle | Highest |
| Speed | ms | s–min | min |
| When it fails, how obvious is the cause? | Very: one class | Somewhat | Often not: anything along the path |
| Flakiness (fails at random) | Rare | Some (timing, containers) | Common (network, UI timing, test data) |
| Can it reach rare edge cases? | Easily (fake anything) | Hard | Very hard |

Hence the **test pyramid**: many unit tests at the bottom, fewer integration tests, a handful of E2E tests at the
top. **Bound it:** the pyramid is a guideline about cost, not a law. Some teams, especially for web services, argue
for a "trophy" with more component/integration tests, because those catch the bugs that matter most for them.
What's universal is the principle: ask each question at the cheapest level that can answer it.

### 2.2 Microsoft's level names

Microsoft's published DevOps guidance ("Shift testing left", learn.microsoft.com) uses levels instead of the
words above:

| Level | Definition (quoted) | Target |
|---|---|---|
| **L0** | "a broad class of fast, in-memory unit tests" | average under 60 ms per test |
| **L1** | unit tests that "depend on code in the assembly under test and nothing else" (may need more setup) | average under 400 ms; none over 2 s |
| **L2** | "functional tests that might require the assembly plus other dependencies, like SQL or the file system" | |
| **L3** | "functional tests [that] run against testable service deployments", possibly with stubs | |
| **L4** | "a restricted class of integration tests that run against production" | |

The same guidance says to write more unit tests, favor tests with the fewest external dependencies, and discourage UI
tests "because they tend to be unreliable". Knowing these level names is useful in a Microsoft interview.

---

## 3. Axis 2: purpose (what question the test answers)

### 3.1 Does it work?

| Kind | Question | Notes |
|---|---|---|
| **Functional** | Does it do what the spec says? | Most tests you write. Any scope |
| **Acceptance** | Does it meet the requirement as the customer/product owner stated it? | Often written as "Given … when … then …" |
| **Scenario** | Does a realistic user journey work, start to finish? | Usually E2E or integration scope. "User connects a sensor, reads a temperature, unplugs it: no crash" |

### 3.2 Does it still work?

| Kind | Question | Notes |
|---|---|---|
| **Regression** | Did a change break something that used to work? | Not a separate scope: **any test becomes a regression test once it guards a fixed bug**. Your #2328 tests are exactly this. The whole suite run on every PR is "the regression suite" |
| **Smoke** | Is it alive at all? | A few fast, shallow checks right after a build or deployment: does it start, does the health endpoint answer, does the homepage load? If smoke fails, don't bother running anything else. (Name from hardware: power it on; does smoke come out?) |
| **Sanity** | Does the specific thing we just fixed look right? | A narrow, quick check after a small change. Used loosely; often merged with smoke |

### 3.3 Is it fast enough, and does it hold up?

| Kind | Question | Example |
|---|---|---|
| **Benchmark** (micro) | How fast is this piece of code, precisely? | BenchmarkDotNet on one method (.NET's standard tool) |
| **Load** | Does it meet its targets at expected traffic? | 1,000 requests/s for 10 minutes; p99 latency under 200 ms |
| **Stress** | Where does it break, and how? | Raise traffic until it fails; check it fails gracefully and recovers |
| **Spike** | Does it survive a sudden burst? | 10× traffic for 30 seconds |
| **Soak / endurance** | Does it degrade over time? | Normal load for 24 hours: finds memory leaks, growing queues, handle exhaustion |
| **Resilience / chaos** | Does it survive its dependencies failing? | Kill a database replica, add network latency, fill the disk (fault injection). Netflix's "Chaos Monkey" made this famous |

"p99 latency" means: 99% of requests were faster than this. Large services care about the slowest 1%, because at
scale, that's thousands of users.

### 3.4 Is it safe?

| Kind | What it does |
|---|---|
| **Static analysis for security (SAST)** | Reads source code for dangerous patterns (SQL built from strings, weak crypto). Example: CodeQL |
| **Dependency scanning** | Flags packages with known vulnerabilities. dotnet/iot's `dependabot.yml` keeps versions current |
| **Secret scanning** | Finds passwords/keys committed by accident |
| **Dynamic analysis (DAST)** | Attacks a running app from outside (malformed requests, injection attempts) |
| **Fuzzing** | Feeds huge numbers of random or mutated inputs to a parser or API to find crashes. Very effective for file formats and protocols |
| **Penetration test** | Humans (or red teams) try to break in |

### 3.5 Does it stay compatible?

| Kind | Question | Why it matters at Microsoft scale |
|---|---|---|
| **API / binary compatibility** | Did a public signature change in a way that breaks existing callers? | .NET libraries have millions of users; .NET uses tooling ("ApiCompat") to compare public APIs between versions |
| **Platform matrix** | Does it work on every OS, CPU and runtime we support? | dotnet/iot's pipeline: Windows, Linux, macOS × Debug, Release |
| **Upgrade / migration** | Does data or config from the old version still work in the new one? | Database schema migrations; settings files |
| **Backward/forward compatibility** | Can old clients talk to new servers and vice versa? | Services deployed gradually run two versions at once |

### 3.6 Is it usable by everyone?

**Accessibility** (screen readers, keyboard-only, contrast; legally required in many places, and Microsoft takes it
seriously), **localization/globalization** (translations, date and number formats, right-to-left text; the classic
bug is parsing `"1,5"` in a German culture), and **usability** testing with real users.

---

## 4. Checks that aren't tests but do the same job

| Check | What it catches | dotnet/iot |
|---|---|---|
| Compiler + type system | A whole class of mistakes, for free, before anything runs | Every build |
| **Analyzers** (Roslyn analyzers, StyleCop) with warnings as errors | Style, likely bugs, API misuse | `eng/Analyzers.props`, `StyleCop.Analyzers.ruleset`; your #2328 hygiene step checked "0 warnings" |
| **Linters** | Format/style in non-code files | `markdownlint`, link checker |
| **Code review** | Design, intent, things no tool knows | Every PR |
| **Code coverage** | Which lines the tests executed | A map of what's *untested*, **not** proof that what's tested is tested well |

**What coverage doesn't tell you:** a test that runs a line without checking its result still "covers" it. 100%
coverage with weak assertions catches nothing.

---

## 5. Techniques: how tests are built (not what kind they are)

### 5.1 Test doubles

"Mock" is used for all of these in conversation, but they're different tools:

| Double | What it does | Example |
|---|---|---|
| **Dummy** | Fills a parameter; never used | `null!` or an empty object for an unused constructor argument |
| **Stub** | Returns canned answers | "When asked for the pin value, return Low" |
| **Fake** | A working but simplified implementation | An in-memory database; a fake GPIO driver that stores pin values in a dictionary |
| **Mock** | Records calls so the test can **verify** they happened | Moq's `Verify(x => x.Write(pin, High), Times.Once)` |
| **Spy** | A real object that also records calls | Wraps the real thing |

Your #2328 harness uses Moq around a mockable driver: partly stub (scripted reads), partly mock (verified calls).

**What doubles don't do:** prove the real dependency behaves like the double. That's why integration tests exist.
yarp#275's "shared-mock trap" is a double-specific failure: the test configures one mock while the class under test
was given a different one, so the test checks nothing and passes.

### 5.2 Other techniques worth knowing

| Technique | Idea | Example |
|---|---|---|
| **Data-driven** | One test body, many inputs | xUnit `[Theory]` with `[InlineData(...)]` rows: pull-up/Low, pull-down/High, … |
| **Property-based** | State a rule; the framework generates hundreds of inputs trying to break it | "Serializing then deserializing returns the original, for any object" (FsCheck in .NET) |
| **Snapshot / approval** | Save the output once; future runs must match it | Generated code, large JSON |
| **Mutation testing** | Deliberately break the product code; a good test must go red. Measures test *quality* | Stryker.NET does it automatically. yarp#275's plan does it by hand ("break the code each test guards → red → revert") |
| **Arrange / Act / Assert** | The three moves of every test | Script the fakes / call the code / check the result or verify a call |

---

## 6. Testing after shipping ("shift right")

At large scale, some problems only appear with real traffic, real data and real hardware variety. So big companies
keep testing in production, carefully:

| Practice | What it does |
|---|---|
| **Canary release** | Ship to a small slice (say 1%) first; compare its error rate with the rest; roll back automatically if worse |
| **Ring deployment** | Ship in widening rings: the team → the company → early-adopter customers → everyone. Microsoft uses rings for Windows and Azure |
| **Feature flags** | Ship the code turned off; turn it on for some users; turn it off instantly if it misbehaves |
| **A/B testing** | Two versions to two groups; measure which one is better (a product question, not a correctness one) |
| **Synthetic monitoring** | A robot runs a scenario against production every few minutes and alerts if it fails |
| **Health checks + observability** | Logs, metrics and traces that show whether the system is healthy right now |

The phrase you'll hear is **shift left** (catch problems earlier, with cheaper tests, ideally before merge) *and*
**shift right** (learn from production safely). Both, not one or the other.

---

## 7. What makes tests bad

| Problem | What it looks like | Why it's expensive |
|---|---|---|
| **Flaky** | Passes or fails without code changes (timing, order, shared state, network) | People learn to rerun and ignore red. Microsoft's guidance: "An unreliable test is organizationally expensive to maintain." dotnet/iot made its flaky Arduino build opt-in for this reason |
| **Passes for the wrong reason** | Asserts on the wrong object, or nothing at all | False confidence. Your #2328 red step checked each new test failed *for the stated reason*, not a crash |
| **Over-mocked** | Tests mirror the implementation line by line | Every refactor breaks them although behavior didn't change |
| **Slow** | Minutes for a unit suite | Developers stop running them locally |
| **Coverage as a target** | Tests written to touch lines, not to check behavior | High number, low protection |

A useful self-check: *if I broke the code this test is about, would it go red, and would the failure message tell me
why?*

---

## 8. Where each kind runs

| When | What usually runs | Why there |
|---|---|---|
| On your machine, while coding | Unit tests for what you're touching | Seconds of feedback |
| **Every PR** (CI) | All unit tests, fast integration tests, compiler/analyzers/linters, security scans | Must be fast and reliable: it blocks merging |
| After merge / nightly | Slow integration tests, full platform matrix, longer security scans | Too slow for every PR |
| Before a release | E2E/scenario suites, performance, compatibility, accessibility | Expensive; done on release candidates |
| Right after deployment | Smoke tests | "Is it alive?" before sending traffic |
| In production | Canary/ring metrics, synthetic monitoring | Real traffic, real hardware |

dotnet/iot today: unit tests on every PR (inside `build.sh`), analyzers as errors, Markdown lint, the Arduino build
when relevant or on request, hardware integration tests **disabled**, dependency updates weekly.

---

## 9. Common misconceptions

| Misconception | Actually |
|---|---|
| "A unit test tests one method" | It tests a unit of behavior, in memory, fast, with external dependencies faked. Several real classes is fine |
| "Regression testing is a separate kind of test" | It's a *use*: rerunning existing tests to catch breakage. A test written for a bug fix is a regression test |
| "Smoke test = quick unit test" | Smoke checks the *deployed or built thing is alive*, shallowly; it's about breadth, not a unit |
| "More E2E tests = more safety" | More confidence per test, but slow, flaky and hard to diagnose. Most bugs should be caught lower |
| "100% coverage means well tested" | It means every line ran. Whether anything was checked is a separate question |
| "Mocks prove the dependency works" | They prove your code works *if* the dependency behaves like the mock |
| "Testing ends at release" | At scale, canaries, rings, flags and monitoring are testing too |

---

## 10. Interview relevance

Expect: "How would you test this service?" Answer by axis: unit tests for logic with doubles; a few integration tests
for the database and HTTP edges; contract tests if other teams consume it; smoke after deploy; load test before
launch; canary in production. Then name the trade-off you're making. Also expect: "What's the difference between a
stub and a mock?", "What's a flaky test and what do you do about one?" (quarantine it, fix the cause, don't just
retry), and "How do you test code that talks to hardware/time/network?" (put it behind an interface, fake it in unit
tests, test the real thing in fewer integration tests: exactly #2328 and Helix).

---

## 11. Teach-back checklist

1. A test is a question with a price; ask each question at the cheapest level that can answer it.
2. Two independent axes: **scope** (what's real) and **purpose** (what's asked).
3. Scope ladder: unit → component → integration → contract → E2E; confidence goes up, speed and diagnosability go
   down, flakiness goes up. Hence the pyramid (a guideline, not a law).
4. Microsoft's L0–L4: L0/L1 unit (in memory, fast), L2 with real dependencies like SQL or files, L3 against a test
   deployment, L4 restricted tests against production.
5. Regression is a use of tests, not a scope; smoke means "is it alive?" after a build or deployment.
6. Performance family: benchmark, load, stress, spike, soak; plus resilience/chaos.
7. Security family: SAST, dependency and secret scanning, DAST, fuzzing, pen testing.
8. Compatibility (API/binary, platforms, upgrades) matters most for widely used libraries like .NET.
9. Stub vs fake vs mock; doubles only prove your code works *if* the dependency behaves like the double.
10. Bad tests (flaky, wrong-reason passes, over-mocked) cost more than they give; the check is "would it go red if I
    broke the code, and would it tell me why?"
