# Lecture 002 (iot#2328): The contribution process, end to end

> **Prompted by:** opening [dotnet/iot#2611](https://github.com/dotnet/iot/pull/2611) for
> [#2328](https://github.com/dotnet/iot/issues/2328) · **Date:** 2026-10-03 · **Builds on:** lecture 001 (the code
> change itself). This one is about everything *around* the code: the repositories, the commits, the conversation,
> the hunt for the bug, the evidence, and working next to someone else's PR.
> **Reading time:** ~90 minutes. It's in nine parts; each stands on its own, so it's fine to read one per sitting.

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
| 8 | How do you review your own change before anyone else sees it? |
| 9 | How do the two Claudes, the config files, the symlinks and `/handoff` fit together? |

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

## Part 8. Reviewing your own change *before* anyone else does

### 8.1 The rule

**Read your own diff, line by line, before it becomes public.** The reviewer's first look shouldn't also be yours.
This week you read the change in lecture 001, but you hadn't looked at the actual diff on screen before pushing.
It turned out fine, but on a different week it's how a stray debug line or a wrong file reaches a maintainer.

### 8.2 A correction: using a PR page to see your diff isn't wrong

What you did on personal projects (open a PR to see what changed) is a real, common technique. GitHub's "Files
changed" view is a good diff viewer, and teams review that way every day. What was missing this time is only the
**timing**: on someone else's repository, a PR is public the moment you open it. So the review has to happen
**before** the PR, with tools that don't publish anything:

| Tool | What it shows | Public? |
|---|---|---|
| `git diff --stat upstream/main...HEAD` | Which files changed, and how many lines | No |
| `git diff upstream/main...HEAD` | Every changed line: exactly what the PR will contain | No |
| `git log -p upstream/main..HEAD` | Each commit with its message and its own diff (what reviewers see commit by commit) | No |
| `git show 4c8682ce` | One commit | No |
| VS Code / Rider "Source Control" or "Compare with branch" | The same diff, side by side, with syntax colors | No |
| Your fork's compare page: `github.com/Timothy-Lee-Grant/iot/compare/main...fix/2328-gpiobutton-initial-state` | The GitHub diff view on **your fork only**, after pushing, before opening the PR | Your fork is public, but this creates no PR and notifies nobody |
| A **draft PR against your own fork's `main`** | Your old habit, exactly, but aimed at your fork | Same as above |

The three dots in `upstream/main...HEAD` mean "changes on my branch since it split from `upstream/main`", which is
exactly what a PR shows. (Two dots in `git log A..B` means "commits in B that aren't in A".)

### 8.3 What to check, in this order

1. **Scope:** `--stat` lists only the files you meant to change (here: `GpioButton.cs`, `Button.Tests.csproj`,
   `GpioButtonTests.cs`). No stray files, no build output, no changes to unrelated lines (whitespace, reformatting).
2. **Every product line:** can you say what it does and why it's there? (Lecture 001 §2 is this step for #2328.)
3. **Every test:** what does it prove, and did you see it fail first? (Part 6.)
4. **Commit messages:** a clear subject line; the body says why; `Fixes #NNNN` where it belongs.
5. **Identity:** `git log --format='%an <%ae> | %cn'` shows **your** name and email as author and committer.
6. **The PR description matches the diff:** every claim points at a line or at evidence.
7. **The repo's own checklist:** the PR template, `CONTRIBUTING.md`, coding guidelines (for dotnet/iot: warnings
   are errors, `Fixes #` as the first line, no force-push).

### 8.4 Do it now, on the live PR

The PR is open, so the review costs nothing now and it's good practice:

```bash
cd ~/Desktop/projects/oss-work/iot-2328/develop/iot
git diff --stat upstream/main...HEAD
git log -p upstream/main..HEAD          # space = next page, q = quit
```

Then open #2611 → **Files changed** and walk the checklist in 8.3. If you spot something you'd change, don't fix it
silently: tell me, and we'll decide whether it's worth a follow-up commit (no force-push, Part 2.4). GitHub also
lets you comment on your own PR's lines, a polite way to point reviewers at something ("this is the read discussed
in #2328").

### 8.5 Make it a gate

From the next issue on, the plan gets a **self-review step before the push**: you walk 8.3 on the diff in your own
terminal or editor and say "reviewed". In mode P, that's the step that makes "I understand every line" literally
true about the lines being submitted, not only about a lecture describing them.

### 8.6 Recap of Part 8

Review the diff before it's public: `git diff --stat`, `git diff`, `git log -p` against `upstream/main`, or your
fork's compare page. Check scope, lines, tests, messages, identity, description, the repo's rules. A PR page is a
fine viewer, as long as it isn't the first look.

---

## Part 9. The machine we built: how the two Claudes, the files and the folders fit together

### 9.1 The rule

**Neither Claude remembers anything between sessions. Everything that carries over lives in files, and each Claude
only knows what it has been set up to read.** The whole setup is a set of files arranged so each Claude reads the
right ones at the right time.

### 9.2 The cast

| Character | Runs where | What it can see | What configures it |
|---|---|---|---|
| **Desktop Claude** (the planner; this chat) | Claude desktop app (Cowork), attached to the claude.ai Project "open source" | The connected folder `exercises`, the Project's docs, your memory files | The Project's doc `learning-protocol.md`; your memory (e.g. preferences on how to teach you); the conventions in `exercises/CLAUDE.md`, which it reads with its tools because the protocol points there |
| **CLI Claude** (the developer; Claude Code in Terminal) | Your Mac, started in `~/Desktop/projects/oss-work/iot-2328/` | That folder: the fork in `develop/iot`, plus `shared/` | The workspace `CLAUDE.md` (auto-loaded), `.claude/settings.json` (permissions), `.claude/commands/` (slash commands) |
| **`shared/`** (the mailbox) | A real folder inside the issue folder in `exercises` | Both | Its own `00-start-here.md` says who writes what |
| **You** | Everywhere | Everything | You decide, post and push |

Analogy, bounded: two engineers on different shifts who never meet, sharing one logbook. The planner writes the work
order in it; the developer reads it, works, writes a shift report; the planner reads the report next shift. Where it
stops: these "engineers" also forget everything when they go home, so even their *own* past work reaches them only
through the logbook.

### 9.3 Where everything lives

```
~/Desktop/projects/exercises/                         ← git repo #1 (your workbench); desktop Claude sees it
├── CLAUDE.md                     conventions for every session (desktop reads it by convention)
├── ai-workflow/                  how the two-Claude setup works + the interaction-modes experiment
├── private/                      git-ignored: learner model, teach-backs (desktop writes; the CLI never sees it)
└── iot/2328_gpiobutton-ispressed-not-initialized/
    ├── CLAUDE.md                 orientation for DESKTOP sessions on this issue
    ├── conversation/             desktop ↔ you, the linear log
    ├── lectures/                 lectures 001, 002, audio
    ├── shared/                   ◄── THE MAILBOX (real files)
    │   ├── 00-start-here.md          how the CLI must operate             (desktop writes)
    │   ├── 01-brief.md               the issue, facts, constraints       (desktop writes)
    │   ├── 02-decisions.md           D1, D2, …; append-only              (anyone appends)
    │   ├── 03-next.md                old work orders (superseded by plan.md in mode P)
    │   ├── 04-interaction-mode.md    current mode: P                      (you own)
    │   ├── plan.md                   the living plan, stages 1–8          (desktop body + everyone's entries)
    │   ├── STATUS.md                 one-screen state                     (CLI rewrites)
    │   ├── sessions/NNN-date.md      one report per CLI session           (CLI writes)
    │   └── evidence/NNN-*.txt        saved runs                           (CLI writes)
    └── workspace/                ◄── THE CLI'S CONFIG (real files, versioned here)
        ├── CLAUDE.md                 the CLI's root instructions (with @ imports)
        ├── .claude/settings.json     permissions
        ├── .claude/commands/handoff.md   the /handoff command
        └── setup.sh                  built the workspace below, once

~/Desktop/projects/oss-work/iot-2328/                  ← the CLI workspace (NOT a git repo)
├── CLAUDE.md       → symlink to …/workspace/CLAUDE.md
├── .claude/
│   ├── settings.json         → symlink to …/workspace/.claude/settings.json
│   ├── settings.local.json     (a real file Claude Code creates when you approve things; stays local)
│   └── commands/handoff.md   → symlink to …/workspace/.claude/commands/handoff.md
├── shared          → symlink to …/2328_…/shared
└── develop/iot/      git repo #2: your fork's clone (the only place code changes)
```

### 9.4 What a symlink is, and why we used them

**A symlink is a file-system entry that stores a path to another file or folder.** Opening
`oss-work/iot-2328/shared/STATUS.md` actually opens `exercises/iot/2328_…/shared/STATUS.md`. One real copy, two
paths to it.

C analogy, bounded: like a pointer, reading through it reads the target. Where it stops: deleting the symlink deletes
only the link, never the target; and because it stores a *path*, moving or renaming the target breaks the link.
See them with `ls -la ~/Desktop/projects/oss-work/iot-2328`: symlinks print as `name -> target`.

Why: if `shared/` were a *copy*, the CLI's reports would land in the copy, you'd paste them back by hand, and the two
copies would drift. With a symlink, the CLI writes straight into `exercises`, desktop Claude reads them there, and
committing `exercises` versions everything. The CLI's config files are symlinked for the same reason: when desktop
Claude changed `/handoff` to stop asking you questions, the CLI picked it up without you copying anything.

`setup.sh` created all this once: made the folders, created the links (`ln -sfn target linkname`), cloned your fork
with `--filter=blob:none`, and added the `upstream` remote.

### 9.5 What happens when you type `claude` in the workspace, step by step

1. **Claude Code reads `CLAUDE.md` in the folder it starts in** (and parent folders). That's built into Claude
   Code: it's the CLI's standing instructions. Ours says "you're the developer in a two-session setup…".
2. **It follows the `@` imports** inside that file. `@~/Desktop/projects/exercises/iot/2328_…/shared/00-start-here.md`
   means "load that file's text into your instructions too". Ours imports 00 (rules), 01 (brief), 03, 04 (mode)
   and `plan.md`. So before you type anything, the CLI already knows the issue, you, the rules, the mode and the
   plan. (Session 001 found that **relative** imports like `@shared/…` didn't resolve through the symlink, which is
   why they're absolute now.)
3. **It reads `.claude/settings.json`**: the permission rules (9.6).
4. **It registers every file in `.claude/commands/` as a slash command.** `handoff.md` becomes `/handoff`. The file
   is just a prompt: typing `/handoff` sends its text to the CLI as if you'd typed it. (Skills, which you've seen in
   the desktop app, are a related but separate mechanism; project commands are the simple version.)
5. **Following `00` §4, it reads `STATUS.md` and `02-decisions.md`**, checks `git status` in `develop/iot`, tells you
   where things stand, then waits, or starts if your first message was a complete instruction.

Check step 2 any time by typing `/memory` in the CLI: it lists the instruction files that are loaded.

### 9.6 The permissions file

```json
{
  "permissions": {
    "additionalDirectories": [ ".../exercises/iot/2328_gpiobutton-ispressed-not-initialized/shared" ],
    "deny": [ "Bash(git push:*)", "Bash(gh:*)" ],
    "ask":  [ "Bash(git commit:*)", "Bash(git rebase:*)", "Bash(git reset:*)", "Bash(git clean:*)" ]
  }
}
```

| Key | Meaning | Why we set it |
|---|---|---|
| `additionalDirectories` | Folders outside the start folder that the CLI may read and write | The real `shared/` lives in `exercises`, outside the workspace; this allows writing through the symlink |
| `deny` | Commands blocked outright | Pushing and GitHub actions are **yours** (your rule: an agent never posts or opens PRs for you) |
| `ask` | Commands that need your yes each time | Commits and history rewrites change your branch; you approve them |

`settings.local.json` is where Claude Code remembers approvals you grant during sessions. It's a real file in the
workspace, not symlinked, so it stays local on purpose.

### 9.7 How desktop Claude knows what to do

Desktop Claude doesn't auto-load a `CLAUDE.md` the way the CLI does. It knows the setup because the claude.ai
Project attaches `learning-protocol.md` and your memory files (e.g. "never assume I've read a document"), and those
tell it to read `exercises/CLAUDE.md`, the issue's `CLAUDE.md`, `shared/STATUS.md` and the newest session report
before acting. You can see those reads as tool calls in this chat.

### 9.8 One full cycle: who reads and writes what

```
DESKTOP (you + planner)                    shared/ (one real copy)                CLI (you + developer)
───────────────────────                    ───────────────────────                ─────────────────────
writes a plan step / runbook   ────────►   plan.md
                                           00 01 04 plan.md  ──── auto-loaded ──►  starts already briefed
                                           STATUS, decisions ──── read at start ►  "here's where we are"
                                                                                   works in develop/iot
                                           evidence/NNN.txt  ◄──── saves runs ────
                                           plan.md Stage 5   ◄──── entries ───────
                                           sessions/NNN, STATUS ◄── /handoff ─────
reads STATUS + newest session  ◄────────
files learning notes in private/ (the CLI never sees private/)
writes the next step  ──────────►  …
```

### 9.9 Changing or improving it

| You want to… | Edit | Takes effect |
|---|---|---|
| Change how the CLI behaves in general | `shared/00-start-here.md` (or ask desktop) | Next CLI session (or now, if you tell the CLI to re-read it) |
| Change the work | `plan.md` (desktop adds steps and runbooks) | Next session |
| Change the mode | `04-interaction-mode.md`, or just tell either Claude | Immediately if you say it |
| Add a slash command | a new `workspace/.claude/commands/<name>.md` + a symlink in the workspace's `.claude/commands/` | Next CLI session |
| Change permissions | `workspace/.claude/settings.json` | Next CLI session (restart the CLI) |
| Set up the next issue | Copy the issue's `workspace/` and `00`, change names and paths, run its `setup.sh` | Once |

Improvement ideas this week surfaced (candidates, not done): a self-review gate before the push (Part 8.5); a
closing "then `/handoff`" line in every plan runbook (the build session forgot it); a `/status` command that prints
STATUS and the plan's newest entry; and a template folder in `ai-workflow/` so a new issue's `workspace/` is
generated instead of copied.

### 9.10 Recap of Part 9

No memory between sessions; files carry everything. `shared/` is the mailbox; `workspace/` is the CLI's config; both
live in `exercises` and are symlinked into the CLI workspace. The CLI auto-loads `CLAUDE.md` and its `@` imports,
applies `settings.json`, and turns `.claude/commands/*.md` into slash commands. Desktop Claude reads the same files
deliberately. One writer per file keeps them from overwriting each other.

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
| Review exactly what the PR contains | `git diff upstream/main...HEAD` / `git log -p upstream/main..HEAD` |
| See where symlinks point | `ls -la ~/Desktop/projects/oss-work/iot-2328` |
| See which instruction files the CLI loaded | `/memory` (inside the CLI) |
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
10. How you'd review your own diff before pushing, and what's on the checklist.
11. What happens, file by file, when you type `claude` in the workspace and when you type `/handoff`; what the
    symlinks are for; what `deny` and `ask` do.
