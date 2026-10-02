# Lecture 004: CI pipelines: what the YAML file controls, what the service controls, and how dotnet/iot does it

> **Prompted by:** Timothy's questions on 2026-10-01 ("is that one YAML file in control of everything? could someone
> just edit it so everything passes? what's set up on the Azure DevOps side? how do agents, manual runs and artifacts
> work?") · **Date:** 2026-10-01 · **Example repo:** [dotnet/iot](https://github.com/dotnet/iot) at `main` @
> `95384e7` (2026-10-01) · **Audio version:** [`audio/002-audio-ci-pipelines.md`](audio/002-audio-ci-pipelines.md)
> · **Prerequisites:** none. Written for any junior engineer who has used CI without knowing how it's wired.
> **Reading time:** about 35 minutes. Section 0 takes 5 and is worth doing first.

**Verified vs. unverified:** everything quoted from dotnet/iot's files was read at the commit above. The check names
in §0 were read from PR #2608's checks page on 2026-10-01. Azure DevOps project settings (who may approve what) can't
be seen from outside, so anything about them is marked *unverified*.

**The whole thing in six lines**

1. A **pipeline** is a recipe that a CI service runs on a clean machine every time something happens (a push, a PR,
   a schedule, a button click), so "does this change break anything?" is answered the same way every time.
2. The **YAML file in the repo** says *what to do*: which steps, on which kind of machine, in what order.
3. The **CI service's settings** say *who may run it, where, with which secrets, and whether the result is
   allowed to block a merge*. Those live outside the repo, and that's where most of the security lives.
4. An **agent** (Azure) or **runner** (GitHub) is the machine that executes one job. The YAML asks for a *pool* or a
   *label*, not for a specific named agent. dotnet/iot uses Microsoft's hosted machines, so nobody "created" agents.
5. Each job runs on a fresh machine and everything on it is thrown away afterwards. **Artifacts** are the files a job
   deliberately keeps, so a later job (or a human) can use them.
6. dotnet/iot has **both** systems: Azure Pipelines builds and tests the code on Windows, Linux and macOS; GitHub
   Actions checks Markdown, runs an opt-in Arduino build, and locks old issues on a schedule.

---

## 0. Before you read: look at one real run (5 minutes)

Open PR #2608's checks: <https://github.com/dotnet/iot/pull/2608/checks>. You'll see roughly this list
(read 2026-10-01):

```
 Azure Pipelines   dotnet.iot                                   ← the whole run
                   dotnet.iot (Build Windows Build Build_Debug)  ┐
                   dotnet.iot (Build Windows Build Build_Release)│
                   dotnet.iot (Build Linux Build Build_Debug)    │ 6 jobs = 3 operating systems
                   dotnet.iot (Build Linux Build Build_Release)  │          × 2 configurations
                   dotnet.iot (Build MacOS Build Build_Debug)    │
                   dotnet.iot (Build MacOS Build Build_Release)  ┘
 GitHub Actions    Markdown_Checks_CI                            ← a different CI system, same PR
 Policy service    license/cla                                   ← not CI: checks the contributor license agreement
```

Click **Details** on one of the Azure lines. It leaves GitHub and opens a run page on `dev.azure.com`: a list of
stages and jobs, each job's log line by line, and a link to what the run "published" (the artifacts). Keep that tab
open; by §5 you'll be able to point at the line in the YAML that produced each thing you see.

The name pattern itself is a map: `dotnet.iot` (the pipeline) `(Build` (stage) `Linux Build` (job)
`Build_Release)` (matrix entry). Each part comes from the YAML in §5.

---

## 1. What problem does CI solve?

**The rule: CI answers "does this exact commit still build and pass its checks?" on a clean machine, the same way
every time, before anyone merges it.**

Without it, "it works" means "it worked on the author's laptop, with whatever was installed there, if they
remembered to run the tests". On a project with hundreds of contributors that isn't evidence of anything. CI replaces
it with a recorded run that anyone can open: which commit, which machine image, which commands, which output.

Two related terms:

| Term | Means | In dotnet/iot |
|---|---|---|
| **CI** (continuous integration) | Build and test every change, automatically, before and after merge | The `Build` stage (§5) |
| **CD** (continuous delivery/deployment) | Take what passed and ship it (sign it, publish it, deploy it) | The `CodeSign` and `Publish` stages, which only run after a merge to `main` |

What CI does **not** do: decide whether a change is a good idea. It produces evidence. Humans (reviewers) decide.
That distinction is the key to the security question in §9.

---

## 2. The cast of characters

| Character | Job | Lives in the **repo** or the **service**? |
|---|---|---|
| **The Recipe** (pipeline YAML) | Lists stages, jobs and steps; says which machine type each job needs and when the pipeline should start | Repo (`azure-pipelines.yml`, `.github/workflows/*.yml`) |
| **The Kitchen** (CI service: Azure Pipelines or GitHub Actions) | Watches for events, reads the Recipe, books machines, streams logs, stores artifacts, reports results back to GitHub | Service |
| **The Registration** (the pipeline definition) | The service's record that says "this repo, this YAML path, these permissions". Azure needs one created by hand; GitHub Actions creates it automatically | Service |
| **The Cook** (agent / runner) | A machine with the agent program installed. Takes one job, runs its steps, sends logs back, is then wiped (hosted) or reused (self-hosted) | Service (hosted) or your machine (self-hosted) |
| **The Cook pool** (agent pool / runner label) | A named group of interchangeable Cooks. The Recipe asks for a pool, not a specific Cook | Service |
| **The Handoff box** (artifact) | Files a job saves so a later job or a human can get them after the machine is gone | Stored by the service |
| **The Safe** (secrets, variable groups, service connections) | Passwords, signing keys, feed credentials. The Recipe refers to them by name; the values never appear in the repo | Service |
| **The Gate guard** (environment with approvals and checks) | A named target such as "Dotnet Iot". The service can require a human approval before any job deploys to it | Service |
| **The Report card** (status check) | The ✅/❌ line the Kitchen posts on the PR | Posted to GitHub by the service |
| **The Bouncer** (branch protection rules) | GitHub's rules for merging: which checks must pass, how many approving reviews, who may merge | GitHub repo settings |

Notice the right-hand column: only the Recipe is in the repo. Everything that decides *trust* (secrets, approvals,
which checks are required, who may merge) is in a service's settings, which a pull request can't change.

---

## 3. So is the YAML file in control of everything?

**The rule: the YAML controls *what happens during a run*. It doesn't control *whether the run is trusted*.**

| The YAML decides | The service's settings decide |
|---|---|
| Which events start a run (push, PR, schedule, manual, comment) | Whether runs from forks are built at all, and whether a maintainer must approve them first |
| Stages, jobs, steps, their order and conditions | Which secrets exist and which pipelines may use them |
| Which machine image or pool a job asks for | Which pools exist; who may add self-hosted machines |
| Which files are saved as artifacts | How long artifacts and logs are kept |
| Which *environment name* a deployment targets | Whether that environment needs a human approval |
| *What* a check means ("build + unit tests on 3 OSes") | Whether that check is **required** to merge (GitHub branch protection) |

Your instinct that "one file controls everything" is half right, and the half that's right is a feature: because the
recipe is a file in the repo, it's versioned, reviewed and diffable like any other code. The practice is called
**pipeline as code** (more generally, *configuration as code*). Before it, pipelines were configured by clicking
through web forms, and nobody could see who changed what.

---

## 4. How does the service find the file?

**The rule: GitHub Actions finds workflows by folder; Azure Pipelines finds a pipeline by a registration someone
created.**

| | GitHub Actions | Azure Pipelines |
|---|---|---|
| Where the file goes | Any `.yml` in `.github/workflows/` | Any path. `azure-pipelines.yml` at the root is just the default the setup wizard suggests |
| How it's discovered | Automatically: every file in that folder is a workflow | Someone creates a pipeline in an Azure DevOps project: picks the repo, picks the YAML path, connects it through a GitHub App or service connection |
| How many | One workflow per file. dotnet/iot has 3 | One pipeline per registration. dotnet/iot's PR checks show one, named `dotnet.iot` |
| How results reach GitHub | Built in | Through the GitHub connection, as status checks |

So for dotnet/iot, a maintainer once did a one-time setup on the Azure DevOps side: create the pipeline, point it at
`dotnet/iot` and `azure-pipelines.yml`, connect GitHub, create the variable group and environment it uses. After
that, editing the YAML is enough to change what runs. Nobody has to touch the Azure side for an ordinary change.
*(Which Azure DevOps organization and project host it isn't visible from the files; the publish step names a feed
in a project called `IoT`.)*

---

## 5. The real recipe: dotnet/iot's `azure-pipelines.yml`, top to bottom

The file is 218 lines. Here's its shape, then each piece.

```
azure-pipelines.yml
├── trigger:   run on pushes to main and release/3.0            (CI after merge)
├── pr:        run on PRs into main and release/3.0             (CI before merge)
├── variables: a few settings, some only for official builds
└── stages:
    ├── Build          3 jobs (Windows, Linux, MacOS) × matrix (Release, Debug) = 6 jobs   ← PRs stop here
    ├── CodeSign       depends on Build; skipped for PRs; Windows; uses secrets
    └── Publish        depends on Build + CodeSign; skipped for PRs; deploys to environment "Dotnet Iot"
```

### 5.1 Triggers

```yaml
trigger:            # pushes (i.e. merges) to these branches
  batch: true       # if several merges land while a run is going, combine them into one next run
  branches:
    include: [main, release/3.0]
pr:                 # pull requests targeting these branches
  branches:
    include: [main, release/3.0]
```

Your PR targets `main`, so the `pr:` trigger fires on every push to your PR branch.

### 5.2 Stage `Build`: three jobs and a matrix

```yaml
- stage: Build
  jobs:
  - job: Linux
    displayName: Linux Build
    pool:
      vmImage: ubuntu-latest          # "give me one of Microsoft's hosted Ubuntu machines"
    strategy:
      matrix:
        Build_Release: { BuildConfiguration: Release }
        Build_Debug:   { BuildConfiguration: Debug }
    steps:
    - script: ./build.sh --ci --configuration $(BuildConfiguration) --prepareMachine
      displayName: Build
```

- **Job** = a unit of work that runs on **one machine**. The `Windows_NT`, `Linux` and `MacOS` jobs run in parallel
  on three separate machines.
- **Matrix** = "run this same job once per entry, with different variables". Two entries × three jobs = the six
  checks you saw in §0.
- **Step** = one command or task inside a job, run in order, on the same machine.

**What does `./build.sh --ci` actually do?** It's the same script you'd run locally. It calls the shared .NET build
tooling (`eng/common/build.sh`, part of Microsoft's "Arcade" build infrastructure), which builds `build.proj`. That
file lists everything: the product projects in `src/`, the tools, the samples, and, importantly:

```xml
<UnitTestProjects Include="src\devices\**\*.Tests.csproj" />
...
<ProjectReference Condition="'$(BuildTests)' == 'true'" Include="@(UnitTestProjects)" Targets="VSTest" />
```

`Targets="VSTest"` means "build these test projects **and run them**". So in dotnet/iot the step called "Build"
also runs all 26 device test projects, including the `Button` tests you added for #2328. (Read from the files; you
can confirm it in §0's run by searching a Linux job's log for the test summary lines.) There's no separate "Test"
step in the YAML; the tests are part of the build. That's a project choice, not a rule: many repos have explicit
`dotnet build` and `dotnet test` steps.

The design point worth keeping: **the pipeline calls a script that also works on your machine.** The YAML stays thin
and the real logic is in `build.sh`, so a failure in CI can be reproduced locally with the same command.

### 5.3 Steps that keep files: artifacts

```yaml
    - task: PublishBuildArtifacts@1
      displayName: Publish Build logs
      condition: always()                       # run this step even if the build failed
      inputs:
        pathToPublish: $(Build.SourcesDirectory)/artifacts/log/$(BuildConfiguration)/
        artifactName: BuildLogs-Linux-$(BuildConfiguration)
```

`condition: always()` is deliberate: logs matter most when the build *failed*. Without it, a failed step skips the
rest of the job and the logs would vanish with the machine. §8 covers artifacts properly.

### 5.4 A disabled step: hardware tests on real Raspberry Pis

```yaml
    # Disabled due to offline devices - see issue #2406
    - script: ./eng/common/msbuild.sh ... eng/sendToHelix.proj /t:Test /p:TestOS=Unix ...
      displayName: Run Helix Tests
      condition: false  # Temporarily disabled - devices offline (issue #2406)
```

**Helix** is .NET's own test-distribution service: it sends test work to queues of real machines. For dotnet/iot,
`sendToHelix.proj` sends `System.Device.Gpio.Tests` to a queue named `Raspbian.11.Arm32.IoT`: actual Raspberry Pis,
because those tests toggle real GPIO pins. The devices are offline, so the step is switched off with
`condition: false` and the PR builds run only the tests that need no hardware. That's why your #2328 tests use a
fake driver: it's the only kind of test CI runs here today.

### 5.5 Stage `CodeSign`: only after a merge, with secrets

```yaml
- stage: CodeSign
  dependsOn: Build
  condition: and(succeeded('Build'), not(eq(variables['build.reason'], 'PullRequest')))
  jobs:
  - job: CodeSign
    pool: { vmImage: windows-latest }
    variables:
    - group: SignClientV2          # a variable group with secrets in it (defined in Azure DevOps, not here)
    steps:
    - download: current            # get the artifacts the Build stage published
      artifact: BuildPackages
    ...
    - pwsh: .\sign code azure-key-vault ... --azure-key-vault-client-secret '$(SignClientSecret)' ...
    - publish: $(Pipeline.Workspace)/BuildPackages
      artifact: SignedPackages
```

Three things to notice:

1. **`condition: ... not(eq(variables['build.reason'], 'PullRequest'))`**: for a PR this whole stage is skipped.
   Signing happens only for runs triggered by a merge to `main`.
2. **`group: SignClientV2`**: the YAML names the secret group. The secret *values* live in Azure DevOps (and Azure
   Key Vault), not in the repo.
3. **`download: current`**: this stage runs on a different machine from Build, so it has to fetch Build's
   artifacts. Nothing else carries over.

### 5.6 Stage `Publish`: a deployment to an environment

```yaml
- stage: Publish
  condition: and(succeeded('Build'), succeeded('CodeSign'), not(eq(variables['build.reason'], 'PullRequest')))
  jobs:
  - deployment: Publish
    environment: Dotnet Iot          # a named target in Azure DevOps; can require approvals (unverified whether it does)
    ...
          - task: NuGetCommand@2
            inputs:
              command: push
              publishVstsFeed: 'IoT/nightly_iot_builds'
              packagesToPush: '$(Pipeline.Workspace)/SignedPackages/*.nupkg'
```

The signed `.nupkg` files are pushed to a nightly NuGet feed. That's the "CD" half: every merge to `main` produces
signed nightly packages.

### 5.7 The whole flow, for a PR vs. for a merge

```
 YOUR PR (pr: trigger)                         A MERGE TO main (trigger:)
 ─────────────────────                         ─────────────────────────
 Build ── 6 jobs, parallel                     Build ── 6 jobs, parallel
   ├─ build + unit tests                         ├─ build + unit tests
   ├─ publish BuildLogs (always)                 ├─ publish BuildLogs, BuildPackages, config
   └─ report 6 checks to the PR                  │
 CodeSign  skipped (it's a PR)                 CodeSign  downloads BuildPackages, signs them
 Publish   skipped (it's a PR)                   │       publishes SignedPackages
                                               Publish   pushes SignedPackages to the nightly feed
```

---

## 6. Agents and pools: correcting the "create an agent with the same name" model

**The rule: the YAML asks for a *kind* of machine (a pool or image); the service picks any free machine that
matches. You only create agents yourself if you run self-hosted ones.**

Your model was: "I create an agent, which is an operating system; the YAML names the agent; the service runs the
file there." The first half is right (an agent is a machine with an OS and tools, and it's where the steps run).
Two corrections:

| Your model | What actually happens |
|---|---|
| The YAML names a specific agent | The YAML names a **pool** (`pool: name: MyPool`) or a **hosted image** (`vmImage: ubuntu-latest`). Any free agent that matches takes the job. Self-hosted pools can add `demands` ("must have Docker") that agents' declared capabilities must satisfy |
| Someone has to create the agent | **Microsoft-hosted** agents already exist. Asking for `ubuntu-latest` gets you a fresh virtual machine with the .NET SDK, git, Node and more preinstalled; it's **discarded after the job**. dotnet/iot uses only these |

The two kinds side by side:

| | Microsoft-hosted (`vmImage:`) / GitHub-hosted (`runs-on: ubuntu-latest`) | Self-hosted |
|---|---|---|
| Who owns the machine | Microsoft / GitHub | You |
| Setup | None | Install the agent (runner) program on your machine, register it with a token into a pool, give it labels/capabilities |
| State between jobs | Fresh every job | Persists (faster caches; but a job can leave junk or secrets behind) |
| When you need it | Most of the time | Special hardware (GPUs, a Raspberry Pi with real sensors), private network access, bigger machines |

Helix (§5.4) solves the same "special hardware" problem at .NET scale: a separate service with queues of real
devices, which the pipeline hands test work to.

**What the agent doesn't do:** it doesn't decide anything. It downloads the repo at the exact commit, runs the steps
it's given, streams the output back, and uploads what it's told to upload.

---

## 7. Triggers: all the ways a run can start

**The rule: a trigger is just "which event starts this pipeline", written in the YAML (with on/off switches in the
service settings).**

| Trigger | Azure Pipelines | GitHub Actions | dotnet/iot example |
|---|---|---|---|
| Push / merge to a branch | `trigger:` | `on: push` | `azure-pipelines.yml`: `main`, `release/3.0` |
| Pull request | `pr:` | `on: pull_request` | Both systems; `markdown-checks.yml` runs on every PR |
| Path filter: only if certain files changed | `paths: include:` | `on: pull_request: paths:` | `arduino.yml` auto-runs only if `global.json`, `eng/ArduinoCsCI.cmd` or `tools/ArduinoCsCompiler/**` changed |
| Schedule | `schedules: - cron:` | `on: schedule: - cron:` | `locker.yml`: `'0 9 * * *'`, once a day, locks stale issues |
| **Manual** | the **Run pipeline** button on any pipeline, optionally with `parameters:` | `on: workflow_dispatch:` (adds a **Run workflow** button on the Actions tab), optionally with `inputs:` | `locker.yml` takes two inputs (`daysSinceClose`, `daysSinceUpdate`); `arduino.yml` and `markdown-checks.yml` allow plain manual runs |
| A PR comment | `/azp run` (a built-in command) | `on: issue_comment` + your own check of the text | `arduino.yml`: a collaborator comments `/run-arduino-tests` |

The Arduino workflow is worth reading as a small program. It has three jobs: `gate` (is this a trigger we accept? if
it came from a comment, does the commenter have write or admin permission? which commit should we build?), `build`
(Windows; runs `eng\ArduinoCsCI.cmd`), and `report` (posts ✅/❌ back on the PR). Its `report` job uses
`if: always() && ...` so it runs even when `build` failed, which is the only time a ❌ comment is needed.

Why is it opt-in? The comment at the top says it: the Arduino build is "narrow in scope and has been flaky", so it no
longer runs on every PR. A flaky check that runs everywhere teaches people to ignore red, which is worse than not
running it.

---

## 8. Artifacts: the only thing that survives a job

**The rule: when a job ends, its machine and everything on it is gone. An artifact is a file the job explicitly
uploaded so it survives.**

Why they're needed:

1. **Between jobs and stages.** Build and CodeSign run on different machines. The only bridge is
   `publish` (upload) in one and `download` in the other.
2. **For humans.** When a build fails, you download `BuildLogs-Linux-Release` and open the binary log
   (`.binlog`) locally to see exactly what MSBuild did.
3. **As the release itself.** The `.nupkg` files that get signed and published are artifacts all the way through.

dotnet/iot's artifact chain:

| Artifact | Made by | When | Used by |
|---|---|---|---|
| `BuildLogs-<OS>-<Config>` | every Build job | always (even on failure) | humans debugging |
| `BuildPackages` (`.nupkg` files) | Windows job, Release only | Release config | `CodeSign` (download) |
| `config` (`filelist.txt`: what to sign) | Windows job, Release only | Release config | `CodeSign` (download) |
| `SignedPackages` | `CodeSign` | after merges only | `Publish` → nightly feed |

Test results are a close cousin: in the Windows job, `PublishTestResults@2` uploads the test result files so Azure DevOps shows a
**Tests** tab with pass/fail per test, instead of you scrolling through the log.

**What artifacts are not:** a cache. Caches (restored NuGet packages, for example) are a separate feature for speed;
artifacts are outputs you want to keep or pass on.

---

## 9. "Couldn't someone just edit the YAML so everything passes?"

**The short answer: yes, a PR can change what its own run does, and that's by design. The protection is that
"passing" was never the thing that lets code in.**

For a PR build, the service runs the pipeline as defined *in that PR's code* (that's how you'd test a change to the
pipeline itself). So a PR could, in principle, delete the test step, and its checks would go green. Here's why that
doesn't get bad code into `main`:

| Layer | What it does | Where it's configured |
|---|---|---|
| **1. The diff is reviewed** | A pipeline change is a code change. A reviewer sees `azure-pipelines.yml` in the diff the same way they'd see a deleted test in a `.cs` file. Deleting tests to go green is equally possible in C#; review is the defense against both | Humans |
| **2. Green ≠ merged** | Branch protection requires approving reviews **and** the required checks. Checks are evidence for the reviewer, not a gate on their own | GitHub settings |
| **3. Forks get no secrets** | Builds of PRs from forks run without secrets and with reduced permissions by default (Azure: "Make secrets available to builds of forks" is off unless turned on; GitHub: "workflows from forks do not have access to sensitive data such as secrets"). So a malicious PR can't steal the signing key | Service settings |
| **4. Outsiders may need approval to run at all** | GitHub: "By default, all first-time contributors require approval to run workflows." Azure has a similar "require a team member's comment" option *(whether dotnet/iot uses it: unverified)* | Service settings |
| **5. Dangerous stages don't run for PRs** | `CodeSign` and `Publish` have `not(...PullRequest)` conditions and run only after a merge, i.e. after review. They also depend on secrets and an environment defined in Azure DevOps, which can require a human approval | YAML + service |
| **6. Triggers check permissions** | The Arduino comment trigger checks that the commenter has write/admin before building | YAML |

Two real-world weak spots worth knowing (these are how CI actually gets attacked):

- **Third-party steps.** `uses: some/action@v1` runs someone else's code with your job's permissions. If that tag is
  moved to malicious code, you run it. dotnet/iot's `locker.yml` pins a **commit SHA**
  (`ref: cd16cd2aad6ba2da74bb6c6f7293adddd579a90e # locker action commit sha`), which can't be moved. Pinning is the
  professional default.
- **`pull_request_target`** (GitHub): a trigger that runs with the *base* repo's secrets. If such a workflow checks
  out and runs the PR's code, a fork can steal secrets. dotnet/iot doesn't use it.

**Bounded:** "can't be bypassed" would be wrong. A careless reviewer who approves a YAML change without reading it
defeats layer 1. The system makes cheating visible and keeps secrets out of reach; it doesn't make it impossible.

---

## 10. Not every YAML file is a pipeline

dotnet/iot has YAML files that configure **bots**, not CI:

| File | Read by | Does |
|---|---|---|
| `.github/dependabot.yml` | GitHub's Dependabot | Weekly (Wednesday) PRs that bump NuGet package versions, grouped (e.g. all `System.*` together); ignores Arcade packages |
| `.github/policies/*.yml` | Microsoft's GitHub policy bot | Adds `untriaged` to new issues; marks PRs stale and closes them after no author activity |

Same idea as pipelines (configuration as code, reviewed in PRs), different reader. YAML is just a data format; what a
file *does* depends entirely on which program reads it.

---

## 11. Vocabulary map: Azure Pipelines ↔ GitHub Actions

| Concept | Azure Pipelines | GitHub Actions |
|---|---|---|
| The file | `azure-pipelines.yml` (any path, registered) | `.github/workflows/*.yml` (auto-discovered) |
| Top-level grouping | `stages:` | (none; jobs only) |
| Unit on one machine | `job:` | `jobs: <name>:` |
| One command | `- script:` / `- task: Name@N` | `- run:` / `- uses: owner/action@ref` |
| Machine | `pool: vmImage:` / `pool: name:` | `runs-on:` |
| Ordering | `dependsOn:` | `needs:` |
| Run if | `condition:` | `if:` |
| Matrix | `strategy: matrix:` | `strategy: matrix:` |
| Manual run | Run pipeline button, `parameters:` | `workflow_dispatch:`, `inputs:` |
| Keep files | `publish:` / `PublishBuildArtifacts` | `actions/upload-artifact` |
| Secrets | variable groups, Key Vault, `$(Name)` | repo/org secrets, `${{ secrets.NAME }}` |
| Deployment gate | `environment:` + approvals | `environment:` + protection rules |

---

## 12. What this means for your #2328 PR

When you open it:

1. Azure Pipelines starts `dotnet.iot` (the `pr:` trigger). Six Build jobs run your branch's code on Windows, Linux
   and macOS, Debug and Release, and run all device unit tests, including your new Button tests. CodeSign and
   Publish are skipped.
2. GitHub Actions runs `Markdown_Checks_CI` (every PR). The Arduino workflow won't run (you didn't touch its paths).
3. The CLA bot asks you to sign the contributor license agreement once.
4. As a first-time contributor, some runs may wait for a maintainer's approval before starting *(unverified for this
   repo)*.
5. If a job goes red: click **Details** → the failing step's log → search for `error` or `Failed`. Reproduce with
   the same command locally (`./build.sh --ci --configuration Release`). Download `BuildLogs-*` if you need the
   binary log.

Hardware tests (Helix) won't run; they're disabled for every PR until #2406 is fixed.

---

## 13. Common misconceptions

| Misconception | Actually |
|---|---|
| "The YAML file is the whole CI setup" | It's the recipe. Registration, secrets, pools, approvals and required checks live in the service and GitHub settings |
| "The YAML names the agent to use" | It names a pool or image; any matching machine takes the job |
| "Someone had to create the agents for dotnet/iot" | It uses Microsoft-hosted machines, created per job and thrown away |
| "Files from the Build job are available to CodeSign" | Only if published as artifacts and downloaded. Every job starts on an empty machine |
| "Green checks mean the code is safe to merge" | Green means "the checks this pipeline defines passed". Review decides what the checks should be |
| "A PR from a fork can use the signing secrets" | Fork builds get no secrets by default, and the signing stage doesn't run for PRs at all |
| "There's a Test step somewhere in dotnet/iot's YAML" | Tests run inside `build.sh` via `build.proj`'s `Targets="VSTest"` |

---

## 14. Interview relevance

Questions you can now answer: "What happens between `git push` and a green check?" · "How would you keep secrets
safe from fork PRs?" · "Hosted vs self-hosted agents: when would you choose each?" · "How do jobs share files?" ·
"Why pin a third-party action to a commit SHA?" · "Why should the pipeline call a script you can run locally?"

---

## 15. Teach-back checklist

1. CI's purpose: answer "does this exact commit build and pass?" on a clean machine, the same way every time, as
   evidence for reviewers. CD ships what passed.
2. The YAML says *what happens during a run*; the service and GitHub settings say *who may run it, with which
   secrets, where, and whether it's required to merge*.
3. GitHub Actions auto-discovers `.github/workflows/*.yml`; Azure Pipelines needs a registered pipeline pointing at
   a repo and a YAML path.
4. Stage → job → step: a job is one machine; steps run in order on it; a matrix runs a job once per variable set
   (dotnet/iot: 3 OSes × 2 configurations = 6 checks).
5. The YAML asks for a pool or image, not a named agent. Hosted machines are fresh and discarded; self-hosted ones
   are yours and persist.
6. Triggers: push, PR, path filter, schedule, manual (`workflow_dispatch` / Run pipeline), comment; dotnet/iot uses
   all of them somewhere.
7. Artifacts are the only files that outlive a job; they connect stages (Build → CodeSign → Publish) and give humans
   logs.
8. A PR can change its own pipeline; the protections are review, required checks, no secrets for forks, approval for
   outsiders, and dangerous stages that run only after merge.
9. In dotnet/iot, `build.sh --ci` builds *and* runs the unit tests; the hardware (Helix) tests are disabled.
