# Lecture 002 (iot#2328): The contribution process, end to end

> **Prompted by:** opening [dotnet/iot#2611](https://github.com/dotnet/iot/pull/2611) for
> [#2328](https://github.com/dotnet/iot/issues/2328) · **Date:** 2026-10-03 · **Builds on:** lecture 001 (the code
> change itself). This one is about everything *around* the code: the repositories, the commits, the conversation,
> the hunt for the bug, the evidence, and working next to someone else's PR.
> **Reading time:** ~60–75 minutes. It's in seven parts; each stands on its own, so it's fine to read one per sitting.

**Why this lecture exists.** This week went fast, and some steps you did on trust ("the tests pass", "it combines
with #2608", "the commit references the issue"). Now there's time to open each box. The goal is that next time, you
could do every one of these steps yourself, and know why.

| Part | Question it answers |
|---|---|
| 1 | Where does the code live? Three repositories, and how a branch on your Mac becomes a PR on Microsoft's repo |
| 2 | What is a commit, and how did yours end up linked to the issue? |
| 3 | What happened this week, in order, and why in that order? |
| 4 | How do you read an issue or a maintainer's reply and map it onto the code? |
| 5 | How do you get from an issue's text to the bug's location (the hunt), and a practice issue to try it |
| 6 | How do you *know* the change works, and how do you package the proof for others? |
| 7 | How did we check we weren't colliding with #2608, and what happens when it merges? |

---

## 0. Before you read: look at the real thing (5 minutes)

```bash
cd ~/Desktop/projects/oss-work/iot-2328/develop/iot
git remote -v                                   # the two remote repositories this clone knows about
git log --oneline -3                            # your two commits on top of dotnet/iot's main
git branch -vv                                  # your branches and what each one tracks
git log --oneline -1 upstream/main              # the newest dotnet/iot commit your clone knows
```

Keep that terminal open; Part 1 explains every line it printed.

---

## Part 1. Three repositories, one change

### 1.1 The rule

**There are three copies of the dotnet/iot repository, and code only moves between them when someone explicitly
moves it.** Nothing syncs on its own.

| Copy | Where | Who can write to it | Its name from your Mac |
|---|---|---|---|
| **Upstream**: `dotnet/iot` | GitHub | Maintainers only | `upstream` |
| **Your fork**: `Timothy-Lee-Grant/iot` | GitHub (public) | You | `origin` |
| **Your clone** | `~/Desktop/projects/oss-work/iot-2328/develop/iot` | You (and the CLI) | — (it's "here") |

```
        dotnet/iot  (upstream)                         Timothy-Lee-Grant/iot  (origin, your fork)
        ┌───────────────────────┐   "Fork" button     ┌──────────────────────────────┐
        │ main                  │ ─── (one-time, on ──► │ main (a snapshot, then stale)│
        │ refs/pull/2608/head   │      GitHub)          │ fix/2328-gpiobutton-...      │
        │ refs/pull/2611/head ◄─┼──── PR #2611 ─────────┤   (you pushed this)          │
        └──────────▲────────────┘                       └──────────────▲───────────────┘
                   │ git fetch upstream                                │ git push -u origin <branch>
                   │ (read only: you can't push here)                  │
                   └────────────────────┐            ┌─────────────────┘
                                   ┌────┴────────────┴─────┐
                                   │ your clone (your Mac)  │
                                   │ develop/iot            │
                                   └────────────────────────┘
```

Firmware analogy, bounded: think of upstream as the vendor's reference SDK, your fork as your company's copy of that
SDK on your own server, and your clone as the working tree on your laptop. Where it stops: a fork is a GitHub-level
idea. Git itself only knows "remotes" (named URLs); GitHub adds the link that lets a fork send PRs back to upstream.

### 1.2 What a **remote** is

**A remote is a name for another copy of the repository, plus a URL.** `git remote -v` showed two:

- `origin` → `https://github.com/Timothy-Lee-Grant/iot.git` (where `git clone` came from: your fork)
- `upstream` → `https://github.com/dotnet/iot.git` (added by `setup.sh` so you can read the real project)

`git fetch upstream` copies upstream's new commits into your clone as **remote-tracking branches** (`upstream/main`).
It never changes your own branches. That's how the CLI saw the new commit `95384e77` on Friday without anything
being merged into your work.

One detail from `setup.sh`: the clone was made with `--filter=blob:none` (a **partial clone**). Git downloaded the
full *history* (all commits) but fetches file contents only when they're needed. It saves disk on the Mac; it
behaves like a normal clone otherwise.

### 1.3 What a **branch** is, and why your first push was refused

**A branch is a movable name that points at one commit.** When you commit, the name moves forward to the new commit.

Your branch `fix/2328-gpiobutton-initial-state` was created by the CLI from `upstream/main`, and git set it to
**track** `upstream/main`. Tracking means "the default place to pull from and push to". So on Wednesday night, a
bare `git push` meant "push this branch to `upstream`, into its `main`". Git's default safety setting
(`push.default=simple`) refuses to push when the local and remote branch names differ, so it stopped. That was
lucky, and you'd also have lacked permission on dotnet/iot anyway.

The fix the CLI applied: `git branch --unset-upstream` (remove the tracking), then on Friday you ran
`git push -u origin fix/2328-gpiobutton-initial-state`: push to **origin** (your fork), into a branch of the
**same name**, and `-u` sets that as the new tracking, so later `git push` alone goes to the right place.
`git branch -vv` now shows `[origin/fix/2328-gpiobutton-initial-state]` next to your branch.

### 1.4 What a **pull request** actually is

**A PR is a request, stored on upstream, to merge one branch (in your fork) into one branch (upstream's `main`).**
It isn't a copy of your code. When you opened #2611:

- GitHub created a hidden ref in **dotnet/iot**: `refs/pull/2611/head`, pointing at your branch's latest commit.
  That's how maintainers (and CI) get your code: `git fetch upstream pull/2611/head`. It's exactly how the CLI
  fetched pgrawehr's #2608 (Part 7).
- Every new commit you push to your branch updates the PR automatically. That's why the PR template says **don't
  force-push** (Part 2.4): reviewers lose track of what changed since they last looked.
- "Allow edits by maintainers" lets maintainers push small fixes directly onto your branch.

### 1.5 Recap of Part 1

Three copies; remotes are named URLs; `fetch` reads, `push` writes; a branch is a moving pointer that may track a
remote branch; a PR is a request from a fork branch to an upstream branch, and GitHub keeps a ref to it upstream.

---

## Part 2. Commits, messages, and how a commit "finds" an issue

### 2.1 A commit is a snapshot with a name derived from everything in it

**Rule: a commit's hash is computed from its content, its parent commit, its author, its date and its message.
Change any of those and you get a different commit with a different hash.**

That's why Friday's rebase changed your hashes:

| Commit | Before the rebase (on `1eb0b2f6`) | After (on `95384e77`) |
|---|---|---|
| Tests | `bd01e163` | `4c8682ce` |
| Fix | `395b9fbf` | `5f676802` |

The *changes* inside are identical (same 3 files, +98); only the **parent** changed (the newer `main`), so the
hashes changed. Firmware analogy: a CRC over (payload + previous block's CRC). Change the previous block and every
CRC after it changes, even if your payload didn't. Where it stops: a git hash is a cryptographic hash (SHA-1 here),
not an error-detection checksum, and it covers metadata (author, message), not just bytes.

### 2.2 Why two commits

Commit 1 adds **only the tests**; commit 2 adds **only the fix**. A reviewer can check out commit 1, run the tests,
watch 4 fail, then check out commit 2 and watch them pass. The commit history itself is evidence (Part 6).

### 2.3 How a commit on *your* fork appears on *their* issue

**Rule: when a commit whose message contains `#2328` is pushed to any public repository in the same GitHub
network (a fork counts), GitHub adds "… referenced this issue" to #2328's timeline.** GitHub scans commit messages
on push; it doesn't care which repository in the fork network the commit landed in.

What it does **not** do: close the issue. Closing keywords (`Fixes #2328`, `Closes`, `Resolves`) only act when:

1. a commit containing them lands on the **default branch of the repository that owns the issue** (dotnet/iot's
   `main`), i.e. when the PR is merged; or
2. they're in a **PR description**: that links the PR to the issue now ("Development" sidebar) and closes the issue
   when the PR merges.

So `Fixes #2328` appears twice on purpose: first line of the PR description (the template asks for it) and in
commit 2's message (so the history says why the commit exists). The reason we waited to push until after your
comment: the "referenced this issue" line would otherwise have appeared before you'd asked whether you could take
the issue. Harmless technically, but it reads as skipping the question.

### 2.4 Rebase, and why "no force-push" during review

- **Rebase** = replay your commits on top of a newer base. New parents → new hashes (2.1).
- If the old commits were already pushed, the remote branch no longer matches your local one, and a normal push is
  refused. **Force-push** (`git push --force`) overwrites the remote branch with your new commits.
- Before anything was public (Friday), rebasing was free: nobody had seen `bd01e163`. **After** opening the PR,
  reviewers may have commented on specific commits; a force-push replaces them and scrambles the review. So during
  review you **add** commits instead, and if you need `main`'s new changes you **merge** `main` into your branch
  (that creates a merge commit, rewrites nothing).

### 2.5 Recap of Part 2

Hash = content + parent + author + message. Two commits so the history proves the bug. `#2328` in a pushed commit
creates a reference anywhere in the fork network; closing happens only on merge (or via the PR description link).
Rebase rewrites hashes, so it's safe only before anything is public.

---

## Part 3. The week, in order, and why that order

| Day | What | Why it came at that point |
|---|---|---|
| Tue 9/29 | Scouted issues; chose #2328 (checked: open, no PR, nobody claiming) | The previous two issues were already being solved by others. Check *before* investing |
| Wed 9/30 | Folders + CLI workspace; forked dotnet/iot; `setup.sh` cloned the fork, added `upstream` | You need a fork to push anywhere; you need `upstream` to read the real project |
| Wed 9/30 | Session 001 (M0): built, ran the 8 existing tests (baseline, evidence 001) | Prove the repo builds and the tests pass **before** changing anything, so later failures are yours |
| Wed night | Switched to mode P. Plan v1 → v2 (code first, comment after understanding) | You didn't want to post what you couldn't explain |
| Thu 10/1 00:30 | CLI: Steps 0–7. Plan check, harness, **red** tests, fix, **green**, hygiene, 2 commits, summary | Red before green proves the tests detect the bug (Part 6) |
| Thu | Lecture 001; your written teach-back; corrections | Understanding before anything public |
| Thu | #2608 got a new commit; impact check; comment v3 | Don't post a comment that's already out of date |
| Thu eve | **Posted the comment** (G2) | Ask before code goes public; the maintainers own the design questions |
| Thu eve | CLI: tested a *copy* of your branch on top of #2608 (25/25, evidence 007) | Turn "should be fine" into "tested" before you claim it |
| Fri 10/2 | Maintainer replied; decoded it; posted a short reply | Their answer decides U1/U2 |
| Fri | CLI: rebased onto newest `main`, final run (evidence 008); you pushed; **PR #2611** | Rebase while nothing is public; push only after the comment |

The pattern underneath: **check → prove the baseline → change → prove the change → understand → communicate →
publish.** Each step makes the next one safe.

---

## Part 4. Reading issues and comments, and mapping them onto the code

### 4.1 The rule

**Read a technical comment sentence by sentence and label each one before reacting: is it a *fact*, an
*opinion/concern*, a *question*, a *request*, or a *condition*? Then map each label onto your own list of decisions.**
Reacting to the whole message at once is how one worried-sounding sentence makes everything feel wrong.

### 4.2 Worked example: the reply you got on Friday

Your first reading: "I am afraid it can also depend on the board" → "our solution is wrong". Here it is labeled:

| Sentence | Label | Which of your questions it answers | What it means for the code |
|---|---|---|---|
| "Hi @Timothy-Lee-Grant great points." | Social | — | Positive tone: your analysis was taken seriously |
| "About point 1, I am afraid it can also depends on the board." | **Concern** about point 1 (timing / settle) | Q1 | "I'm afraid" is English hedging ("unfortunately"), not "your fix is wrong". He's saying the settle time **varies by board**, so no single delay value is right. |
| "We should definitely avoid send bogus notifications" | **Requirement** | Q1 | Don't raise fake events. Your fix raises **none**: it already meets this |
| "but delays usually cause a lot of issues (very often when running tests…)" | **Opinion → decision** | Q1 | Rules out option (b), the settle delay. Option (a), what you built, stays |
| "On point 2, it is very use-case depent." | **Opinion** | Q2 (ordering) | No single right answer |
| "Not entirely sure how to solve this but would be nice" | **Not a request** | Q2 | He isn't asking you to close the window. Leave it, document it |
| "You are more than welcome to submit a PR" | **Permission** | "OK to take this?" | Go-ahead |
| "just be aware that @pgrawehr is working on the other PR and avoid collisions." | **Condition** | — | Your PR must not make pgrawehr's life harder. Already true (Part 7) |
| "What do you think @pgrawehr ?" | **Question to someone else** | — | pgrawehr may add a view; watch for it |

Mapped onto your decision table: U1 → (a) confirmed, U2 → as built, permission granted, one condition already
satisfied. Nothing in the code changes.

**Two habits that make this work:**
1. **Keep your own numbered questions** in what you post (1. Timing, 2. Ordering). Replies then come back as "About
   point 1… On point 2…", which maps cleanly.
2. **Separate "concern" from "request".** Maintainers often think out loud. "Would be nice" and "not sure how to
   solve this" are not instructions. When unsure, ask: "Just to confirm, you'd like X in this PR, or as a follow-up?"

### 4.3 Worked example: the original issue and its 2024 triage comment

The issue (2024): "After initializing a GpioButton, the IsPressed property is always false, even if the logical
state of the pin is true", with a workaround (`input.IsPressed = pin.Read() == PinValue.Low;`).
Labeled: a **fact** (observed symptom) plus a **hint at the mechanism** (the workaround reads the pin, so the
library doesn't). krwq's triage reply ("can you try with LibGpiodDriver, SysFsDriver and RaspberryPi3Driver…") is a
**question** testing a hypothesis: *is this driver-specific?* The code shows it isn't: `GpioButton` never reads the
pin with any driver. That's a useful thing your comment implicitly answered: you located it above the driver layer.

### 4.4 How your own comment was built, and why each part is there

| Part of your comment | Purpose |
|---|---|
| "I'd like to pick this up if it's still wanted" | Ask, don't announce. The issue had an owner (raffaeler) |
| The cause in one sentence, plus the swallowed release | Show you understand it, and add something new they didn't know |
| "I have a small change ready… four fail on `main` and pass with the change" | Evidence, stated precisely |
| Numbered questions with options | Make the maintainer's answer easy and mappable (4.2) |
| The #2608 paragraph | Coordination: show you've checked you won't collide |
| "@raffaeler, OK for me to take this?" | One clear question to the one person who decides |

### 4.5 What @-mentions and `#numbers` do

- `@name` notifies that person directly.
- `#2608` in a comment creates a **link on #2608's page** ("mentioned this"), so everyone watching #2608 (pgrawehr,
  joperezr) can see your comment without being @-mentioned. Polite: visible, not pushy.

### 4.6 Recap of Part 4

Label each sentence before reacting. Map labels onto your numbered questions. "I'm afraid", "would be nice", "not
sure" are usually concerns, not requests. Structure your own comments so the answers map back.

---

## Part 5. The hunt: from an issue's words to the bug's location

### 5.1 The rule

**Turn the symptom into a question about one piece of state, then find every place that writes and reads that
state.** Don't start by reading the whole codebase (it's the "unbounded descent" trap); start from the noun in the
symptom.

### 5.2 The method, as it was applied to #2328

| Step | Question | What it found in #2328 |
|---|---|---|
| 1. Restate | What exactly is wrong, as a sentence about state? | "`IsPressed` is `false` at construction when the pin level means pressed" |
| 2. Locate the state | Where is that state declared? | `ButtonBase.cs`: `public bool IsPressed { get; set; } = false;` |
| 3. Find every **writer** | Who assigns it? (`grep -n "IsPressed" src/devices/Button/*.cs`) | Only `HandleButtonPressed` (true) and `HandleButtonReleased` (false) |
| 4. Find what **triggers** the writers | Who calls those? | `GpioButton.PinStateChanged`, only on Falling/Rising **edges** |
| 5. Check the gap | Is there a path where the state should be set but isn't? | Construction: the ctor opens the pin and subscribes, but never reads. **Root cause** |
| 6. Find every **reader** | Who depends on the state being right? | The release guard: `if (_debounceTime.Ticks > 0 && !IsPressed) return;` → **the swallowed release** (the extra finding) |
| 7. Look sideways | Linked issues? Open PRs on the same files? | #1715 (settle time, same binding) → Q1; #2608 (same files) → coordination |
| 8. Turn the hypothesis into a failing test | Can I make it fail on demand? | Fake driver says "Low at startup" → assert `IsPressed` → fails on `main` |

Step 6 is the one that turns a reporter's observation into a contributor's insight: **after finding the cause, ask
what else trusts the broken value.** That's how "property is wrong" became "a click is lost".

Firmware analogy: chasing a wrong register value. You find the variable, then every write to it (ISR, init, main
loop), then ask which path should have written it and didn't. Where it stops: in C# a "write" can hide behind a
property setter or an event handler, so grep for the property name, not just `=`.

### 5.3 Tools for each step

| Need | Command or place |
|---|---|
| Find all uses of a name | `grep -rn "IsPressed" src/devices/Button/` |
| Who calls a method | `grep -rn "HandleButtonReleased" src/` |
| History of a file (why is it like this?) | `git log --oneline -- src/devices/Button/ButtonBase.cs` |
| Who wrote a line, and in which commit | `git blame src/devices/Button/ButtonBase.cs` |
| Open PRs touching a file | GitHub: PRs tab, search `is:pr is:open Button`; or read a PR's "Files changed" |
| Linked issues | The issue's sidebar and timeline ("mentioned this") |

### 5.4 Practice: try the hunt yourself on [dotnet/iot#1663](https://github.com/dotnet/iot/issues/1663)

The issue: "**Mcp23017 initialization resets pin values.** Every time a new instance of Mcp23017() is being created
the pins are being reset to low. This is an issue when the written software reboots." A maintainer replied: "try not
to break existing APIs (that means that you probably have to add an extra bool argument to the ctor)", pointing at a
similar earlier fix (#1479).

Work through the eight steps in your clone (no CLI). Write your answers down before checking the key.

1. Restate it as a sentence about state. *(Hint: which state is lost: in C#, or in the chip?)*
2. Find the constructor: `src/devices/Mcp23xxx/Mcp23xxx.cs`. Which registers does it write, with what values?
3. For each register write, look up what that register does in the MCP23017 datasheet (you know how to read these).
   Which write actually changes the output pins?
4. What does the maintainer's reply rule out, and what does it ask for? Label each sentence (Part 4).
5. Look sideways: open #1479. What pattern did that fix use?
6. How would you test it with a fake? *(Hint: look at `src/devices/Mcp23xxx/tests/` for how they fake I2C.)*

**Answer key (check after):** (1) The chip's output latch/pin state is overwritten by the driver's constructor, so
a restarted program wipes the outputs it should leave alone. (2) Inside `if (!_disabled)` the ctor writes `IODIR`
(all inputs, `0xFF`/`0xFFFF`), `GPIO` (`0x00`), `IPOL` (`0x00`). (3) Writing `IODIR` to all-inputs drops the outputs
(the relays release); writing `GPIO` to 0 sets the output latch to low. (4) "Don't break existing APIs" = a
**requirement**; "extra bool argument" = a **suggested design** (a new optional ctor parameter defaulting to today's
behavior). (5)–(6): yours to find. That's the point of the exercise. Bring your notes here and we'll compare.

You don't have to fix #1663. The skill being practiced is steps 1–8.

### 5.5 Recap of Part 5

Symptom → one piece of state → every writer → what triggers them → the missing path → every reader (what else
breaks) → look sideways → a failing test.

---

## Part 6. Verification: how you know it works, and how you prove it to others

### 6.1 The rule

**A passing test proves something only if you've also seen it fail for the right reason.** A test that has never
failed might not be testing anything.

### 6.2 The evidence chain, and what each link proves

| Evidence | What it shows | What it rules out |
|---|---|---|
| `001` baseline: 8/8 on untouched `main` | The repo builds and its tests pass on your machine | "Your change broke something that was already broken" |
| `002` harness smoke: 9/9 | The fake driver is really wired into `GpioButton` (it saw OpenPin, SetPinMode, AddCallback) | Tests that pass because the fake isn't connected |
| `003` **red**: 4 fail on assertions, tests only | The new tests **detect the bug**, for the stated reason, not a crash | Tests that pass no matter what |
| `004` **green**: 15/15 with the fix | The fix makes exactly those tests pass, and the old 8 still pass | Regressions in existing behavior |
| `005` hygiene: 0 warnings, 5 runs × 15/15, diff = 3 files / +98 | No warnings (this repo fails the build on any warning), not flaky, and the change is only what you claim | Hidden extra changes; timing-dependent passes |
| `007` on top of #2608: 25/25 | Your change and pgrawehr's combine | "It works alone but breaks theirs" |
| `008` final on newest `main`: 15/15, 0 warnings | What you actually pushed builds and passes | Stale evidence from an older base |
| CI (`dotnet.iot` check, pending maintainer approval) | An **independent** machine and OS builds and tests it | "Works on my Mac" |

### 6.3 Verify it yourself (do this; 10 minutes)

```bash
cd ~/Desktop/projects/oss-work/iot-2328/develop/iot
git switch --detach 4c8682ce                 # tests only, no fix
dotnet test src/devices/Button/tests/        # expect: Failed 4, Passed 11. Read WHICH tests and WHICH asserts
git switch fix/2328-gpiobutton-initial-state # back to the real branch
dotnet test src/devices/Button/tests/        # expect: Passed 15
git diff --stat upstream/main...HEAD         # expect: 3 files, 98 insertions, 0 deletions
```

Then one more, the strongest check a reviewer could do (**mutation testing** by hand): break the fix on purpose and
confirm the tests notice. In `GpioButton.cs`, change `initialValue == PinValue.Low` to `initialValue ==
PinValue.High`, run the tests, watch them fail, then undo it with `git checkout -- src/devices/Button/GpioButton.cs`.
If a deliberately broken fix still passed, the tests would be too weak.

### 6.4 Why the evidence files have headers

Every evidence file starts with: command, date, `dotnet --version`, branch and commit, machine. That makes each run
**reproducible**: anyone can rerun the same command on the same commit and expect the same result. A test result
without the commit hash is a claim; with it, it's evidence. (One honest blemish: evidence 007's last line `Exit code:
0` came from an `echo`, not from `dotnet test`. The CLI noted it instead of editing the file. Evidence files are
never edited after capture.)

### 6.5 What is **not** verified, and how it could be

| Not verified | Why | How you could |
|---|---|---|
| Real hardware | Every test uses a fake driver | A 20-line console app on your Raspberry Pi: hold a button, start the app, print `IsPressed`. Save the output as evidence 009. A strong "verified on hardware" line for the PR |
| Settle time on real boards | The fake answers instantly | Only hardware, and it varies by board (as the maintainer said) |
| The ordering window | The fake fires callbacks on the test's own thread | Argued in words, not testable reliably |

### 6.6 Packaging the proof for other people

Each claim in the PR description should point at something a reviewer can check:

| PR claim | Backed by |
|---|---|
| "Four of the new tests fail on `main` and pass with the change" | Commit 1 vs commit 2 (they can run it); evidence 003/004 |
| "checked it on top of #2608's latest commit… all Button tests pass together" | Evidence 007 |
| "Verified on macOS 26.3 arm64, .NET SDK 10.0.302: 15/15, no new warnings" | Evidence 008 |

The evidence files live in your public `exercises` repo, so you could link them from the PR if a reviewer asks.

### 6.7 Recap of Part 6

Baseline → red (for the right reason) → green → hygiene → combined → final → independent CI. Run the red/green
yourself, and try one deliberate break. State what isn't verified. Every claim points at evidence.

---

## Part 7. Working next to someone else's PR (#2608)

### 7.1 The situation

pgrawehr's PR #2608 (with a commit from joperezr) rewrites the button's timing in `ButtonBase.cs`, and also touches
`GpioButton.cs` (one line), `Button.Tests.csproj`, `ButtonTests.cs`, `TestButton.cs`. Your PR touches
`GpioButton.cs`, `Button.Tests.csproj`, and adds `GpioButtonTests.cs`. Two PRs, two authors, overlapping files.

**Rule: overlapping *files* are fine; overlapping *lines* cause conflicts. Find out which you have, prove it, and say
so publicly.**

### 7.2 How the check was done, step by step

```
step 1  git fetch upstream pull/2608/head:pr-2608-latest   # get their code as a local branch (Part 1.4's hidden ref)
step 2  git diff 1eb0b2f6 pr-2608-latest --stat            # which files do THEY change?
step 3  compare with your 3 files → overlap: GpioButton.cs, Button.Tests.csproj
step 4  read their hunks in those 2 files → GpioButton.cs: their change is on line 40 (the short ctor);
        yours is after line 91. Different lines. csproj: both add a line in the SAME spot
step 5  git branch try/2328-on-2608 fix/2328-...           # a THROWAWAY copy of your branch
        git switch try/2328-on-2608
        git rebase pr-2608-latest                          # replay your 2 commits on top of their code
step 6  conflict in Button.Tests.csproj only → resolve by keeping both lines → continue
step 7  dotnet build (0 warnings) + dotnet test → 25/25 (their 18 + your 7)  → evidence 007
step 8  your real branch was never touched; the copy can be deleted
```

This is a **merge simulation**: you find out *now* what will happen *later* when both PRs land, without changing
either one. Firmware analogy: building two feature branches together on a bench before either goes to production.

### 7.3 What the conflict looked like, and why "keep both" is right

```xml
<<<<<<< theirs (#2608)
    <PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" Version="8.0.0" />
=======
    <Compile Include="..\..\..\System.Device.Gpio.Tests\MockableGpioDriver.cs" Link="MockableGpioDriver.cs" />
  </ItemGroup>

  <ItemGroup>
>>>>>>> yours
```

Git marks a conflict when both sides changed the same region and it can't tell which you want. Here the two lines
are **independent additions** (they need a test-time library; you need the fake driver file), so the right answer
is both. Not all conflicts are this easy: when two sides change the *same logic*, you have to understand both
intentions.

### 7.4 How it was communicated

- In your issue comment: "it doesn't change the initial-state behavior, and my change only touches `GpioButton.cs`
  and the test project, so the two should combine with at most a one-line merge". (Written after reading their diff
  and simulating, before the 25/25 run existed: "should" was the honest word then.)
- In the PR description, after evidence 007: "I checked it on top of #2608's latest commit (6a2f897): the only
  overlap is one line in `Button.Tests.csproj` (keep both), and all Button tests pass together". Now a tested claim.
- The `#2608` mention puts a link on their PR, so they see it without being chased.

### 7.5 What happens next

| If… | Then |
|---|---|
| #2608 merges first | GitHub shows your PR "has conflicts" in `Button.Tests.csproj`. Fix by **merging `main` into your branch** (`git fetch upstream; git merge upstream/main`), keeping both lines, re-running the tests, pushing a normal (non-force) commit. Ask before doing it; maintainers sometimes prefer to resolve trivial conflicts themselves |
| Yours merges first | pgrawehr resolves the same one-line conflict on his side; your note in the PR already told him what it is |
| pgrawehr asks you to wait | Wait; it's his area right now |

### 7.6 Recap of Part 7

Fetch their PR as a branch; compare files, then lines; simulate the combination on a throwaway copy; resolve; build;
test; save evidence; tell them what you found, with the right level of certainty ("should" before testing,
"pass together" after).

---

## Cheat sheet

| Want to… | Command |
|---|---|
| See remotes | `git remote -v` |
| Get upstream's new commits (read only) | `git fetch upstream` |
| See what each branch tracks | `git branch -vv` |
| Get someone's PR as a local branch | `git fetch upstream pull/<N>/head:<local-name>` |
| What would my PR contain? | `git diff --stat upstream/main...HEAD` |
| First push of a new branch to your fork | `git push -u origin <branch>` |
| Later pushes (while the PR is under review) | `git push` (never `--force`) |
| Bring in upstream's new `main` during review | `git fetch upstream && git merge upstream/main` |
| Look at an old commit, then come back | `git switch --detach <hash>` … `git switch <branch>` |
| Who wrote this line | `git blame <file>` |

---

## Teach-back checklist

1. The three copies (upstream, fork, clone): who can write where, and how code moves between them.
2. What `origin` and `upstream` are, and why your first `git push` was refused.
3. What a PR physically is (a request plus a ref on upstream), and why new pushes update it.
4. Why the rebase changed your commit hashes, and why force-pushing during review is avoided.
5. How a commit on your fork showed up on dotnet/iot's issue, and what does (and doesn't) close the issue.
6. How to read a maintainer's reply: label each sentence; "I'm afraid…" in Friday's reply, decoded.
7. The eight hunt steps, and the one that found the swallowed release.
8. The evidence chain: why red-before-green matters, and what 002, 005 and 007 each rule out.
9. How the #2608 check was done (fetch, compare files then lines, throwaway rebase, resolve, test) and what you'd do
   if #2608 merges first.
