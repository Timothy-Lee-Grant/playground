# Lecture 3: Reading CI logs — how to find out why a check went red

> Cross-cutting: the method applies to any repo's CI (Azure Pipelines, GitHub Actions, others).
> Written 2026-10-18; updated the same day with the binlog result (§6.3), the "one failure, many red marks"
> section (§2.1) and the large-file warning (§6.2).
> Case study: dotnet/iot PR 2627, Azure Pipelines build **133095** (`20261018.4`), where the first CI run
> showed "2 failing, 8 successful". Everything quoted below is real output from that build.

---

## 0. The one-line rules (read these even if you read nothing else)

1. **The red annotation GitHub shows you is usually a symptom, not the cause.** Find the *first* step that failed.
2. **A red check is a tree.** Pipeline → stage → job → step. Walk down to the leaf that failed first.
3. **Compare legs.** If the same command passes on one machine and fails on another, the difference between the two is where the cause lives.
4. **"Build FAILED" with "0 Error(s)" means MSBuild stopped without saying why.** The text log can't tell you more; the binary log (binlog) can.
5. **Check history before blaming your code.** If an unrelated PR failed the same way, it's the CI, not you.
6. **Contributors can't re-run the CI; maintainers can.** Ask for it politely, with your evidence.

---

## 1. What CI is doing on your PR

When you open or push to a PR on dotnet/iot, two separate systems run checks:

| System | What it runs | Where you see it |
|---|---|---|
| **GitHub Actions** | `Markdown_Checks_CI`: markdownlint and link checking on `.md` files | GitHub's Checks tab, logs on github.com |
| **Azure Pipelines** (Microsoft's CI, at dev.azure.com) | `dotnet.iot`: builds and tests the whole repo on Windows, Linux and macOS, in Debug and Release | GitHub shows a summary; the real logs live on dev.azure.com |
| **Dotnet Policy Service** | `license/cla`: checks that you signed the CLA | GitHub only |

GitHub doesn't run the Azure build. It only shows a **summary that Azure reports back**. That's why the
GitHub page shows just two short error lines and a "View more details on Azure Pipelines" link.

**What CI does NOT do here:** it doesn't run every test project. In this repo the CI run executes about 9 test
projects (Arduino, Mcp25xxx, Nmea0183, Rtc, Gui, CharacterLcd, Common, Bmxx80, Bmm150). `Bmp3xx.Tests` is
*compiled* but not *run*. Your 79 tests run on your Mac, not in CI.

---

## 2. The tree: pipeline → stage → job → step

Azure Pipelines organizes one run like this (names from build 133095):

```
Build 133095  (the pipeline run, "dotnet.iot", number 20261018.4)
└── Stage "Build"
    ├── Phase "Linux Build"
    │   ├── Job "Linux Build Build_Debug"      ✔
    │   └── Job "Linux Build Build_Release"    ✔
    ├── Phase "MacOS Build"
    │   ├── Job "MacOS Build Build_Debug"      ✔
    │   └── Job "MacOS Build Build_Release"    ✔
    └── Phase "Windows Build"
        ├── Job "Windows Build Build_Debug"    ✔
        └── Job "Windows Build Build_Release"  ✘
            ├── 1 Initialize job               ✔
            ├── 2 Checkout …/pull/2627/merge   ✔
            ├── 3 Build                        ✘   ← the first failure: the real one
            ├── 4 Publish Build logs           ✔
            ├── 5 CopyFiles                    ✘   ← symptom: nothing to copy
            ├── 6 PublishTestResults           (skipped)
            ├── 7 Publish Build Artifacts      ✘   ← symptom: nothing to publish
            ├── 8 Publish Signing Scripts      ✔
            └── …
```

- A **job** runs on one machine (one "agent": a fresh Windows, Linux or macOS VM).
- A **step** (Azure calls it a *task*) is one command inside that job.
- When a step fails, a **parent turns red** too. That's why GitHub shows two red checks: the job
  `dotnet.iot (Build Windows Build Build_Release)` and the overall `dotnet.iot`. **They are one failure, not two.**

### Why the GitHub annotations misled you

GitHub showed:

```
Path does not exist: D:\a\1\a\Packages
Error: Not found SourceFolder: D:\a\1\s\artifacts\packages\Release\Shipping
```

These come from steps 5 and 7. Their job is to **copy the NuGet packages the build made** and upload them.
Step 3 failed before any package was made, so the folder didn't exist. These steps are set to run even after
a failure (so logs still get uploaded), and they fail loudly because their input is missing.

**Rule:** when an error says "not found" or "does not exist" about an *output* folder, ask "which earlier
step was supposed to create this?" and go look at *that* step.

### 2.1 One failure, many red marks

In this case the GitHub page showed **two red checks** and **two error lines**, and the Azure job showed
**three red steps**. It was all **one** failure, spreading downwards:

```
Tca955x.Tests test run fails silently              (the cause, found in §6.3)
  → step "Build" fails: no NuGet packages made      7m 35s   no error line printed
      → step "CopyFiles" fails: nothing to copy     <1s      "Not found SourceFolder …"     ← GitHub error 1
          → step "Publish Build Artifacts" fails    <1s      "Path does not exist …"        ← GitHub error 2
  → job "Windows Build Build_Release" red                                                   ← GitHub check 1
      → pipeline "dotnet.iot" red                                                           ← GitHub check 2
```

**How to tell symptom from cause, by eye:**
- **Order:** the cause is the *first* red step. Later red steps usually fail *because of* it.
- **Duration:** a step that fails in **under a second** almost never did real work. It found its input missing.
  The cause is the long-running step before it (here: Build, 7m 35s).
- **Wording:** "not found", "does not exist", "no files matched" about an *output* folder point backwards.

Why only these two lines made it to GitHub: Azure turns lines that a step marks as errors into
"annotations" (`##[error]…`). Step 3 printed no error line at all (see §4), so it had nothing to annotate.

---

## 3. How to get to the logs (clicks)

1. On the PR, open the **Checks** tab. Click the red job, then **"View more details on Azure Pipelines"**.
2. On dev.azure.com you land on the build page. The left side lists the jobs; click the red one.
3. The steps list appears. **Click the first red step** (here: "Build"). Its full log opens.
4. Useful controls in that log view:
   - the search box (top right of the log) to find words like `error`, `FAILED` or your project name;
   - "View raw log" to get plain text you can save and search on your Mac.
5. On the build **summary** page, the **"published"** / **Artifacts** link lists files the pipeline
   uploaded. dotnet/iot uploads one log bundle per job: `BuildLogs-Windows-Release`,
   `BuildLogs-Windows-Debug`, `BuildLogs-Linux-Release`, and so on. Each should contain the MSBuild binlog
   for that job (the build log names it: `artifacts\log\Release\Build.binlog`).

These pages are public; you don't need to sign in to read them.

---

## 4. Reading the failing step: what the Build log said

The Windows Release "Build" step runs one command:

```
build.cmd -ci -configuration Release -prepareMachine /p:BuildPackages=true
```

That is the repo's own build script (Arcade, Microsoft's shared .NET build tooling). It restores packages,
compiles every project, runs some test projects, then packs NuGet packages (`BuildPackages=true`).

### The log, as a story with timestamps

| Time (UTC) | What the log shows | State after this line |
|---|---|---|
| 21:41:19 | the command above starts | nothing built |
| 21:42:32 | `Restored …\Bmp3xx\Bmp3xx.csproj` (and the tests and samples projects) | packages restored, still nothing compiled |
| 21:43:17 | `Bmp3xx -> …\Release\net8.0\Bmp3xx.dll` | **your binding compiled in Release on Windows, with no warnings** |
| 21:43:18 | `Bmp3xx.Samples -> …Bmp3xx.Samples.dll` | your sample compiled |
| 21:45–21:48 | test projects compile; some are run: `Passed! - Failed: 0 … Bmxx80.Tests.dll` | everything green so far |
| 21:48:43 | `Bmp3xx.Tests -> …Bmp3xx.Tests.dll` | your tests compiled (CI doesn't run them) |
| 21:48:45 | `Amg88xx.Tests -> …Amg88xx.Tests.dll` | another test project compiled |
| 21:48:48 | `Build FAILED.` / `0 Warning(s)` / `0 Error(s)` | **stopped. `Iot.Device.Bindings` never built, nothing packed** |
| 21:48:49 | `Build failed with exit code 1. Check errors above.` | the step reports failure |

Note what is **missing**: no `error CS…` (C# compiler), no `error SA…` (StyleCop), no `error NU…` (NuGet
packing), no `Failed!` test summary. Normally MSBuild prints every error twice: once where it happens and
once in the summary at the end. Here the summary says **0**.

### What "Build FAILED" with 0 errors means

MSBuild decides "failed" from the result of each task it runs. A task can **return failure without logging
an error**. Typical reasons:

- a worker process (MSBuild runs several "nodes" in parallel) **crashed or was killed**;
- a tool started by the build (for example a test host) **exited non-zero without printing anything**;
- a resource problem on the CI machine (disk, memory, a file locked by another process, which happens a lot on Windows).

None of these are about your C#. All of them are invisible in the console log, because the console log only
shows messages, and in these cases nobody wrote a message.

**What "0 errors" does NOT tell you:** that nothing went wrong in your project. It means the cause wasn't
*logged*. To see which task returned failure you need the **binlog** (§6).

---

## 5. Differential diagnosis: compare legs and history

Like debugging firmware that fails on one board but not another, you look for **what differs** between the
passing and failing runs.

### Compare the legs of the same build

| Leg | Command | Your code | Result |
|---|---|---|---|
| Windows **Debug** | same `build.cmd`, **also** with `BuildPackages=true` | same | ✔ built and packed `Iot.Device.Bindings` (with Bmp3xx inside) |
| Windows **Release** | `build.cmd … Release … BuildPackages=true` | same | ✘ silent failure before packing |
| Linux / macOS Release | Release | same | ✔ |

If your code broke **packing**, Windows Debug would fail too (it packs). If your code broke **Release
compilation**, Linux and macOS Release would fail too. Only one leg failed, and it failed with no error. That
points away from your code, but it's not proof.

### Compare with other PRs (history)

The pipeline's run history (pipeline page → *Runs*) shows every recent build:

| Build | PR | Result | What failed |
|---|---|---|---|
| 133087 | main | ✔ | — |
| 133092 | 2624 | ✔ | — |
| 133088 | 2624 | ✘ | Linux: "Free disk space … lower than 5%" (a CI machine problem) |
| 133019 | 2624 | ✘ | real errors: `CP0001`/`CP0002` API compatibility (that PR's own code) |
| **133018** | **2581** | ✘ | **Windows Debug: `Build FAILED.` / `0 Error(s)` right after `Amg88xx.Tests -> …`** |
| **133095** | **2627 (yours)** | ✘ | **Windows Release: `Build FAILED.` / `0 Error(s)` right after `Amg88xx.Tests -> …`** |

Build 133018 is a **different PR, three days earlier, with no Bmp3xx code**, and it failed the same way at
the same point. That's the strongest evidence so far: **this is a known intermittent failure in the CI (a
"flake")**, and it happens around the last test projects on Windows.

Notice also what the history teaches about a *real* failure (133019): **real failures have error codes and
file names.** `CP0002: Member … was removed` tells you exactly what to fix. Silent ones don't.

---

## 6. The binlog: the full record

A **binlog** (`.binlog`, binary log) is MSBuild's complete recording of a build: every project, target,
task, property, and the result of each. Unlike the console log, it shows **which task returned false**, even
when nothing was printed.

### 6.1 Getting it

1. Build summary page → **Artifacts** (or "N published"); or, in the job's log view, the line
   "**2 artifacts produced**" under the job summary. Pick **BuildLogs-Windows-Release** → download (a zip).
2. Unzip it. In this case it held `Build.binlog` (25 MB, the main build) and `ToolsetRestore.binlog`
   (8 KB, only the SDK download at the start; ignore it).

### Reading it: two ways

**A. Structured Log Viewer** (a GUI by Kirill Osenkov; there are Windows and cross-platform builds, see
msbuildlog.com). Open the binlog, then:
- search `$error` for errors, or look for the **red** nodes in the tree;
- for a silent failure, expand the tree to the first node marked as failed and read which **task** it was.

**B. Replay it to text on your Mac** (no extra tools; MSBuild can "replay" a binlog):

```sh
cd ~/Downloads/BuildLogs-Windows-Release        # wherever you unzipped it
dotnet msbuild Build.binlog -noconlog -flp:"v=diag;logfile=replay-diag.log"
grep -n -i -E "error|failed|exited with code|crash" replay-diag.log | head -50
```

`-noconlog` means "no console output", so **the command prints nothing** and simply returns after a while.
That's success, not a hang. Check with `ls -lh` that `replay-diag.log` appeared.

### 6.2 Size warning

`v=diag` means diagnostic verbosity: everything is in the text file. In this case the **25 MB binlog became a
2.0 GB text log** (about 26 million lines). Search it, don't open it in an editor. And **never put it inside a git
repo**: GitHub rejects files over 100 MB, so a commit containing it breaks your next push. Keep it in Downloads
and save a small excerpt of the lines that matter as your evidence.

### 6.3 Searching it: what this case found

MSBuild writes a line for every target and task it finishes. Failed ones end in `-- FAILED`. So search for
those, plus the special message MSBuild uses for a task that failed silently:

```sh
grep -n -E "returned false but did not log|-- FAILED|^Build FAILED" replay-diag.log | cut -c1-300
```

(`grep` took about 9 seconds on 2 GB. `cut` shortens very long lines.)

Result in build 133095, in order:

```
The "VSTestTask" task returned false but did not log an error.
Done executing task "VSTestTask" -- FAILED.
Done building target "_VSTestConsole" in project "…\src\devices\Tca955x\tests\Tca955x.Tests.csproj" -- FAILED.
Done building target "VSTest" in project "…\Tca955x.Tests.csproj" -- FAILED.
Done Building Project "…\Tca955x.Tests.csproj" (VSTest target(s)) -- FAILED.
Done executing task "MSBuild" -- FAILED.
Done building target "Build" in project "D:\a\1\s\build.proj" -- FAILED.
… (the Arcade wrapper project) -- FAILED.
Build FAILED.
```

**Read it bottom-up as a call stack and top-down as the cause.** The first `-- FAILED` is the leaf: the
`VSTestTask` (the task that runs a test project's tests) for **Tca955x.Tests**, a different binding.
Every line below it is a parent passing the failure up. Its timestamps (21:47:37.9 → 21:47:43.4) show the test run
died about 6 s after it started, without printing a result.

Then **check your own code explicitly**, so you can say so with evidence:

```sh
grep -c -E "Bmp3xx.*FAILED|FAILED.*Bmp3xx" replay-diag.log      # → 0
```

So the answer to "was it my code?" went from "probably not" (§5) to **"no: the only failing task is another
project's test run"**. The excerpt kept as evidence is 71 lines.

Note what the binlog could *not* tell us: **why** the Tca955x test host died. It printed nothing. That's for
the maintainers (or a re-run) to find out; it isn't your PR's problem to solve.

Other things to look for in a binlog, in order of likelihood:
- `MSB4166: Child node … exited prematurely` → a crashed MSBuild node (flake);
- a test task (`VSTest`, `Exec` running `dotnet test`) with `exited with code` → a test host problem;
- anything that names `Bmp3xx` near a failure → would mean it **is** ours; then read that part closely.

---

## 7. Deciding: flake or real?

| Evidence | Points to |
|---|---|
| Error codes with file paths (`CS1591 …Bmp3xx\Bmp390.cs(22,16)`) | **Real.** Fix it |
| Fails on every leg, or on every re-run | **Real** |
| `0 Error(s)`, one leg only, other legs that run the same thing pass | Probably a flake |
| Same symptom on unrelated PRs | Flake |
| CI machine messages: disk space, "agent lost communication", time-out | Flake |
| Re-run passes | Flake (confirmed) |

**Scenario recipes:**

- **Real failure in your code** → reproduce locally with the same command, fix it, commit, push to the PR
  branch. CI re-runs on every push.
- **Looks like a flake** → don't push a dummy commit. Post a short comment with the evidence and ask a
  maintainer to re-run. (`/azp run` comments trigger Azure Pipelines but only work for people with write
  access.)
- **Flake** (any time) → get the binlog (§6), find the failing task, and name it in the comment. A comment
  that says *which* project's task failed is far more useful to maintainers than "looks flaky". That's what
  the PR 2627 comment did.
- **Flake that happens twice** → also check whether the failing project is the same each time.
  The maintainers may already know about it, and a binlog finding helps them fix their CI.
- **Unsure** → run the exact CI command locally (`./build.sh -ci -configuration Release /p:BuildPackages=true -bl`).
  It runs on macOS, so it can only show problems that aren't Windows-specific, but it rules out the obvious ones.

---

## 8. The habits this case teaches

- Start at the **first** red step and read upward from its last lines.
- Say the **state** after each step in your head ("compiled, not packed"); the missing line tells you
  more than the error lines.
- When the log is silent, **compare** (legs, history) before guessing.
- Collect evidence **in the order a reviewer would want it**: the failing step, what the log says, what
  passes elsewhere, the history.

## 9. Check yourself

1. Why did GitHub show two red checks for one failure?
2. Which step created `artifacts\packages\Release\Shipping`, and why didn't it exist?
3. What does `0 Error(s)` *not* tell you?
4. Which single fact from §5 most strongly says "not your code"?
5. If a re-run fails again with `0 Error(s)`, what do you download, and what's the first thing you search for?
6. Two steps failed in under a second, one after 7 minutes. Which one do you open first, and why?
7. The replay command printed nothing. How do you know whether it worked?
