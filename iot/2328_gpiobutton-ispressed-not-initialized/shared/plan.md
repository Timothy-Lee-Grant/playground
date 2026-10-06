2026_09_30_22_33 — dotnet/iot#2328: initialize `GpioButton.IsPressed` from the pin

> **The living plan for mode P (plan-driven, Timothy observes).** One document, read top to bottom.
>
> **Who writes what:** the plan body (Stages 1–3) is owned by **desktop** and revised in place with a version
> number (v1, v2, …; old versions summarized, not deleted). **Entries** are append-only and tagged
> `[Author — YYYY_MM_DD_HH_MM]`: `[Timothy]` (recorded by whichever Claude he said it to), `[Desktop]`, `[CLI]`.
> Nobody edits someone else's entry. The CLI writes Stage 5 entries and change requests; desktop writes the rest.
>
> **Gates** (only Timothy opens them): **G1** plan approved → implementation may start · **G2** before anything is
> posted upstream · **G3** after the teach-back → PR may be opened. Between gates the CLI works autonomously.
> Deviation rules: `00-start-here.md` §6.

---

# Stage 1 (Direction)

**[Desktop — 2026_09_30_22_33] Direction, from Timothy's decisions on 2026-09-29/30**

1. **Goal:** Timothy's first upstream *code* PR, opened by **Sunday 2026-10-04**. Merge timing is the maintainers'.
2. **Issue:** dotnet/iot#2328. A button already held at startup is reported as released, and with debounce on its
   first release is swallowed (brief §3). Fix scope: `GpioButton` learns the pin's level at construction.
3. **How we work (mode P):** Timothy isn't yet confident making the design and implementation calls, so the AI
   drives: desktop plans, the CLI checks the plan against the code and implements it. Timothy watches, then learns
   the finished change through lectures and a teach-back **before** the PR exists (his rule: never submit code he
   can't explain line by line). The hands-on public actions stay his: posting comments, running the final tests,
   pushing, opening the PR, answering review.
4. **What success looks like:** a small, conventional PR that a maintainer can review in minutes, with tests
   that fail before the fix and pass after; and a Timothy who can explain every line and every choice in it.
5. **Not in scope:** #1715 (startup `Press` event), the `GpioButton.cs` double-assignment nit, anything in PR
   #2608's territory (the time source).

# Stage 2 (Discussion)

**[Desktop — 2026_09_30_22_33] Opening entry: the decisions, and whose they are**

Every decision is tagged **ours** (we decide, Timothy grants at G1) or **upstream** (the maintainers decide; we
propose and build so it's cheap to change).

| ID | Decision | Owner | Proposal | Why |
|---|---|---|---|---|
| U1 (= P1) | **When** to take the first reading: (a) immediately, (b) after a settle delay, (c) lazily on first subscription (#1715's fix) | **upstream** | Ask in the comment. Build (a) as the default | (a) is the smallest change; (b) and (c) are easy to switch to later because the tests pin *behavior*, not timing |
| U2 (= P3) | **Order** of the reading vs. registering the edge callback | **upstream** | Ask. Build **register, then read** as the default | Firmware rule: enable the interrupt, then sample the level. Read-then-register can permanently miss an edge between the two calls (state wrong until the next edge). Register-then-read can overlap with a callback, but both end at the level that was actually read. The CLI checks this in Step 0 |
| O1 (= P2) | Base branch | ours | Branch from `upstream/main`; rebase after #2608 merges | #2608 has no review yet and may change; basing on an unmerged PR ties our PR to its fate. Expected conflict is small (#2608 edits the ctor's *defaults*, we add lines after `OpenPin`) |
| O2 | How to test without hardware | ours | Copy the in-repo `MockableGpioDriver` + Moq pattern (`src/devices/Ili934x/tests/`) into the Button tests | Precedent maintainers already accept. Step 0 checks whether a better in-repo fake exists (e.g. the virtual GPIO controller) |
| O3 | Scope of the change | ours | `GpioButton` constructor + tests + `IsPressed` XML doc remark. Nothing in `ButtonBase` logic | Smallest diff that fixes the issue; keeps clear of #2608 |
| O4 | Commits | ours | Two commits: (1) tests that fail, (2) the fix | Lets a reviewer check out commit 1 and see the bug proven |

# Stage 3 (Implementation Planning)

**[Desktop — 2026_10_01_00_25] Implementation Plan v2** (v1 → v2: Timothy's decision of 2026_10_01_00_25, Stage 3 Discussion. The
upstream comment moves from *before* the code to *after* lecture 1, because he won't post what he can't explain. The
code is built tonight on the Stage 2 defaults. Nothing public happens until he understands it.)

Ordering (v2): check the plan against reality, build the whole change locally on the Stage 2 defaults, then
understanding (lecture 1 covers the code, the tests **and** the comment), then the public steps. If the maintainers
later choose differently, it's a plan revision: the tests' structure stays and only the expected behavior changes.

| Step | What | Who | Proof | Gate |
|---|---|---|---|---|
| **0** | **Plan review against the code** (no edits): confirm brief §3 paths and line numbers; read PR #2608's diff for `src/devices/Button` and list exact collisions; evaluate O2 (Mockable driver vs. any in-repo virtual/fake controller); sanity-check U2's reasoning in the code (which thread callbacks run on, whether `IsPressed` writes race). Write one Stage 5 entry: "plan holds" or change requests | CLI | Stage 5 entry | — |
| **1** | *(moved, v2)* **Post the upstream comment**: now after lecture 1 (between Steps 8 and 9) | **Timothy** posts; desktop drafted | Link in a `[Timothy]` entry | **G2** |
| **2** | Branch `fix/2328-gpiobutton-initial-state` from `upstream/main`. Add the mock driver to `src/devices/Button/tests/` and a helper that builds a `GpioButton` over it. One smoke test (construct + dispose) | CLI | `evidence/002-harness-smoke.txt` | G1 must be open |
| **3** | **Failing tests** (red): pull-up + Low → pressed; pull-up + High → not pressed; pull-down mirror (High → pressed); external resistor (pull-up wiring, `hasExternalResistor: true`); held at startup + debounce on → releasing raises `ButtonUp` and `Press` | CLI | `evidence/003-red.txt`: each new test fails **for the stated reason** (assertion on `IsPressed` / missing event), not a crash | — |
| **4** | **The fix** in the `GpioButton` constructor, per U1/U2 defaults: after registering the callback, read the pin once and set `IsPressed` from the active level, raising no events. XML-doc remark on `IsPressed` | CLI | `evidence/004-green.txt`: all Button tests pass (old 8 + new) | — |
| **5** | **Hygiene:** build with no new warnings (StyleCop/analyzers); run the Button tests 5× to check for flakiness; diff is only the intended files | CLI | `evidence/005-hygiene.txt` + `git diff --stat` | — |
| **6** | **Commits** per O4 (CLI asks; settings require approval) | CLI proposes, Timothy approves | `git log --oneline` in the entry | — |
| **7** | **Implementation summary entry:** every changed line grouped by purpose; the "why" of each choice and the alternatives rejected; for **each test**, one line on what it proves and what it doesn't; open questions. Save the full diff (`git diff upstream/main...HEAD > evidence/006-diff.patch`). This is the raw material for lecture 1 | CLI | Stage 5 entry + `evidence/006-diff.patch` | — |
| **8** | **Lecture 1 tonight** (spec in Stage 6), then the comment (Step 1, G2), further lectures only if needed, then the teach-back | Desktop writes; **Timothy** reads, posts, teaches back | Teach-back analysis | **G2**, then **G3** |
| **9** | **Contribution** (Stage 7): Timothy runs the final test command himself, pushes the branch, opens the PR. CLI drafts the description; desktop reviews it | **Timothy** acts; AI drafts | PR link | after G3 |
| **10** | **Review** (Stage 8): each review comment becomes a step here; the AI drafts code and replies, Timothy posts | all | — | G2 applies to each reply |

**Timothy's own work, per step:** Step 6 (approve commits), Step 8 (read lecture 1), Step 1 (post, after lecture 1), Step 8 (teach back), Step 9
(test, push, open PR), Step 10 (post replies). Everything else he may watch or skip.

**Acceptance criteria**

1. New tests fail on `upstream/main` and pass with the fix; the 8 existing Button tests still pass.
2. The diff touches only `src/devices/Button/GpioButton.cs`, `ButtonBase.cs` (doc remark only, if needed) and
   files under `src/devices/Button/tests/`. No public API added or removed.
3. No new build warnings. Tests stable across 5 runs.
4. PR description: first line `Fixes #2328`; the problem (including the swallowed release); the U1/U2 defaults
   stated as open questions; verified environment; brief AI-assistance disclosure.
5. Timothy passes his own teach-back on the change (G3) before the PR is opened.

**Risks**

| Risk | If it happens |
|---|---|
| A maintainer prefers (b) or (c), or the other order | A plan revision: change one step and the expected values; the tests' structure stays |
| pgrawehr folds the fix into #2608 | Switch to 📖 learn mode; review his version against ours. Still a win for understanding |
| #2608 merges first and conflicts | Rebase (a learning moment for Timothy; desktop explains it) |
| No maintainer reply by Sunday | Open the PR anyway; the description states the defaults as questions. (v2: the comment now goes out later, so a reply before Sunday is less likely. That's acceptable) |
| Someone else claims #2328 before Timothy comments | Low (no activity since 2024). Re-check the issue right before posting. If claimed: switch to 📖 learn mode with our working version to compare |
| Understanding isn't ready by Sunday | **The PR slips.** Understanding beats the deadline (Timothy's rule) |
| The mock pattern doesn't fit `GpioButton` | Step 0 catches it; change request |

**Timeline (v2):** Tonight (Wed/Thu night): Steps 0, 2–7 in one CLI session, then lecture 1. Thu–Fri: read lecture 1,
questions, post the comment when ready (G2). Sat: teach-back on a walk. Sun 10/4: G3, Step 9.

### Stage 3 Discussion Subsection

*(Questions about the plan, and the G1 grant, go here.)*

**[Timothy — 2026_10_01_00_25, via desktop] G1 granted, with a reorder**

Doesn't want to post the comment yet: he'd be committing to something others rely on without understanding what it
says or being sure he can deliver it. Writing code isn't the bottleneck, so: the CLI builds the whole change
tonight on the most likely direction (the Stage 2 defaults). If the maintainers choose differently, little is lost
and we re-implement. Then lecture 1 tonight, covering what changed and why, how it solves the issue, the tests and
their output and what they prove, and what the comment means and implies. **G1 is open for Steps 0 and 2–7.**
G2 (posting) stays closed until he's read lecture 1.

# Stage 4 (Upstream Communication)

**[Desktop — 2026_09_30_22_33] Draft comment for #2328 (Timothy posts in his own voice after G2)**

> I'd like to pick this up if it's still wanted. On `main`, `GpioButton` never reads the pin's current level, so
> `IsPressed` stays `false` until the first edge. It's more than cosmetic: with debounce enabled,
> `HandleButtonReleased` returns early when `!IsPressed`, so the first release of a button held at startup is
> swallowed (no `ButtonUp`/`Press`).
>
> My plan: read the pin once in the constructor and set `IsPressed` from the active level (Low for pull-up, High for
> pull-down), without raising events, plus unit tests with a mockable `GpioDriver`. Two questions first:
> 1. **Timing:** #1715 says the pull-up may not have settled right after `OpenPin`. Is an immediate read acceptable,
>    or would you prefer a short settle delay, or reading lazily on first subscription as #1715's agreed fix suggests?
> 2. **Ordering:** read before registering the edge callback (can miss an edge in between) or after (the callback
>    can race the constructor)? I'd lean towards registering first, then reading.
>
> I see @pgrawehr's #2608 touches the same files; I'll keep my change separate and rebase once it's in.
> @raffaeler, OK for me to take this?

**[Timothy — 2026_10_01_18_57, via desktop] G2: comment posted on #2328**

Posted his own edit of comment v3 publicly on dotnet/iot#2328: https://github.com/dotnet/iot/issues/2328#issuecomment-5944161026. Same content as v3: the startup
bug and the swallowed release, the change ready with 4 red → green tests, question 1 (timing: immediate / settle
delay / lazy), question 2 (ordering: the small read→assign window), and the #2608 compatibility note; @raffaeler.
Nothing pushed. **Now waiting for a maintainer reply**; adapt per lecture 001 §5.4.

**[Desktop — 2026_10_02_16_48] Maintainer reply on #2328 (pasted by Timothy)**

> Hi @Timothy-Lee-Grant great points.
> About point 1, I am afraid it can also depends on the board. We should definitely avoid send bogus notifications but delays usually cause a lot of issues (very often when running tests as you have seen in the recent PR).
>
> On point 2, it is very use-case depent. Not entirely sure how to solve this but would be nice
>
> You are more than welcome to submit a PR, just be aware that @pgrawehr is working on the other PR and avoid collisions.
>
> What do you think @pgrawehr ?

Read against lecture 001 §5.4: **U1 → (a) immediate read** (delays rejected; "no bogus notifications" = our no-events
design). **U2 → keep register-then-read, no lock**; document the window in the PR. **Go-ahead for a PR**, avoiding
collisions with #2608 (already true: no `ButtonBase.cs` changes; 25/25 on top of #2608, `evidence/007`). He asked
pgrawehr's view. **No code changes needed.**

# Stage 5 (Implementation)

*(CLI entries go here, one per step or per meaningful finding, each with: what changed, deviations, evidence,
and **why**. Change requests are marked **CHANGE REQUEST** and stop work on the affected steps until Timothy
grants or declines them.)*

**[CLI — 2026_10_01_00_33] Step 0: plan review against the code. Verdict: plan holds; one deviation, one U2 refinement**

*Changed:* nothing in `develop/` (fetched `upstream/main` and `pr-2608` refs only). *Evidence:* code reading; line
numbers below are on `upstream/main` @ `1eb0b2f6`, unchanged since the brief.

1. **Brief §3 paths confirmed (by reading; Step 3 proves them by test).** `ButtonBase.cs:73` `IsPressed` starts
   `false`. `GpioButton.cs:87-91`: `OpenPin` then `RegisterCallbackForPinValueChangedEvent`; the pin is never read.
   `ButtonBase.cs:126`: `if (_debounceTime.Ticks > 0 && !IsPressed) return;` swallows the first release.
2. **PR #2608 collisions** (head `a1563be0`, 3 commits on `1eb0b2f6`). In our files: `GpioButton.cs:40` (the
   short ctor's `this(...)` defaults renamed to `DefaultDoublePressTime/DefaultHoldingTime`); `ButtonBase.cs` time
   fields, `TimeSource`, and every `DateTime.UtcNow` in the handlers; `TestButton.cs` (adds `AddTime`); most of
   `ButtonTests.cs`. **Our change touches none of those lines**: we add lines after `GpioButton.cs:91`, a doc
   remark on `ButtonBase.cs:70-73` (untouched by #2608, though the hunk at :73 is adjacent: a possible trivial
   textual conflict), a csproj line and a new test file. Expected rebase: clean or one adjacent-hunk conflict.
3. **O2: test harness. Keep the pattern, but link instead of copy (deviation, implementation detail).**
   `MockableGpioDriver` exists twice: the original in `src/System.Device.Gpio.Tests/` and a byte-identical copy
   (except namespace) in `Ili934x/tests/`. Three test projects (Tca955x, Gpio, Board) **link** the original with
   `<Compile Include="..\..\..\System.Device.Gpio.Tests\MockableGpioDriver.cs" Link="MockableGpioDriver.cs" />`.
   *Why link:* 3 precedents vs. 1; no duplicated 100-line file in our diff; a reviewer sees one csproj line.
   *Rejected:* copying (the Ili934x way: more diff, a 3rd copy to drift); `Iot.Device.Gpio.VirtualGpioController`
   (in `src/devices/Board`): it's a real in-repo fake, but Button doesn't reference Board, and adding that
   project reference to the tests drags a big dependency in for one test file; it also routes events through its
   own pin objects, so we'd test Board's plumbing as much as ours. Moq reaches every test project via
   `eng/Versions.external.props:13` (Moq 4.20.72), no csproj change needed.
   Mock gotchas found (they shape the tests): `MockBehavior.Loose` returns `false` for `IsPinModeSupportedEx`, so
   `GpioButton`'s ctor throws unless the test sets it up; `ReadEx` returns `default(PinValue)` (Low) unless set up,
   so every test sets the level explicitly; `FireEventHandler` invokes the callback **synchronously on the test
   thread**.
4. **U2 sanity check: "register, then read" still the better default, but the plan's reason needs one correction.**
   Callbacks run on **driver-owned background threads** (`SysFsDriver._eventDetectionThread`;
   `LibGpiodDriverEventHandler` `Task.Run`; `LibGpiodV2EventObserver` dedicated threads), so the ctor and
   `PinStateChanged` can truly overlap, and `IsPressed` is an unlocked auto-property (existing code has no locking
   anywhere). The plan says register-then-read "ends at the level that was actually read". Not quite: there is a
   **lost-update window** between `Read()` returning and the ctor assigning `IsPressed`. If an edge's callback runs
   inside that window, the ctor then overwrites the newer value with the older reading → wrong until the next
   edge. That's the same failure *kind* as read-then-register, but the window is a few instructions instead of the
   whole `RegisterCallback…` call (which on libgpiod starts a thread). Closing it fully needs a lock shared with
   the handlers, i.e. `ButtonBase` changes (out of O3 scope, and #2608 territory). **Not a change request:**
   nothing in Steps 2-7 changes; it's a sentence for the PR's open question and for lecture 1. Also note: no
   test can reproduce it, since the mock fires synchronously (tests pin behavior, not this timing).
5. **Environment:** in `develop/iot`, `dotnet --version` = 10.0.302 (global.json 9.0.306, `rollForward: major`,
   `allowPrerelease: true`). Outside the repo the default SDK is now `11.0.100-preview.5`; irrelevant to us, but
   if someone runs `dotnet` from another directory they'll see a different version.

**[CLI — 2026_10_01_00_44] Steps 2-6: harness, red, fix, hygiene, commits. All done; acceptance criteria 1-3 met**

*Changed:* branch `fix/2328-gpiobutton-initial-state` from `upstream/main` @ `1eb0b2f6` (tracks `upstream/main`;
nothing pushed). Commits (both verified authored **and** committed by `Timothy-Lee-Grant <timothyleegrant1995@gmail.com>`,
no co-author trailer: Timothy asked for this check):
```
395b9fbf Initialize GpioButton.IsPressed from the pin level        (GpioButton.cs, +9)
bd01e163 Add GpioButton tests for the initial pressed state        (Button.Tests.csproj +4, GpioButtonTests.cs +85)
```
*Evidence:* `002-harness-smoke.txt` (9/9), `003-red.txt` (15 run: 11 pass, 4 fail on assertions), `004-green.txt`
(15/15), `005-hygiene.txt` (clean rebuild 0 warnings/0 errors; 5 runs × 15/15; diff = 3 files, 98 insertions,
0 deletions), `006-diff.patch`.

*Deviations* (all implementation details, decided per §6):
1. **Mock linked, not copied** (see Step 0 entry §3).
2. **Doc remark on `GpioButton`'s class `<remarks>`, not on `ButtonBase.IsPressed`.** *Why:* `ButtonBase` is the
   hardware-independent base; a remark there about a subclass's constructor is the wrong direction of knowledge,
   and leaving `ButtonBase.cs` untouched means **zero** overlap with #2608 (it edits 56 lines there).
   Acceptance criterion 2 allowed `ButtonBase.cs` "doc remark only, if needed"; it wasn't needed.
3. **Two of the plan's tests are guards, not red tests.** Pull-up + High → not pressed, and pull-down + Low → not
   pressed, *pass on `main`* because `false` is already the default. Step 3's "each new test fails" can't hold for
   them. Kept anyway: they catch a wrong fix (always `true`, or the wiring inverted). The four level cases are one
   `[Theory]` with four `[InlineData]` rows (levels as `0`/`1`: `PinValue` is a struct and can't appear in an
   attribute; `int` converts implicitly, `PinValue.cs:32`). *Rejected:* four separate `[Fact]`s (4× the same body).
4. **Smoke test checks the wiring, not just construct+dispose:** open, mode `InputPullUp`, callback on
   `Falling|Rising`; on dispose, callback removed and pin closed. *Why:* cheap, and it proves the mock actually sees
   `GpioButton`'s calls, so later passes aren't vacuous.
5. **Commit 2's body ends with `Fixes #2328`.** Needs Timothy's eye before he pushes: GitHub shows a "referenced
   this issue" line on #2328 as soon as a public commit mentioning it is pushed, even to his fork. That's a public
   trace, so it should come **after** the comment is posted (G2). Options: keep it (push only after G2, which Step
   9 does anyway), or amend it out before pushing and keep `Fixes #2328` only in the PR description. Not changed
   without his say.

*Why the harness looks the way it does:* `Mock<MockableGpioDriver>` with `CallBase = true` (the Tca955x pattern;
the mock's own doc comment says it's required: without it Moq replaces the protected overrides that forward to
the `…Ex` methods). Every test sets `IsPinModeSupportedEx → true` (otherwise the ctor throws) and the startup level
via `ReadEx` (otherwise Moq returns `default(PinValue)` = Low, which would silently mean "pressed" for pull-up).
`FireEventHandler` plays the hardware: it calls the registered callback synchronously, like an edge.

**[CLI — 2026_10_01_00_44] Step 7: implementation summary (raw material for lecture 1)**

**A. Every changed line, by purpose** (`006-diff.patch`)

| Purpose | Lines | Why this way · alternatives rejected |
|---|---|---|
| The fix | `GpioButton.cs`: `PinValue initialValue = _gpioController.Read(_buttonPin);` | One read through the controller (which also checks the pin is open). Placed **after** `RegisterCallback…` = U2 default (Step 0 §4 for the remaining window). Placed **inside the existing `try`**, so a failing `Read` gets the same cleanup as a failing `OpenPin` (controller disposed if we own it). *Rejected:* reading before registering (U2 alternative); reading outside the `try` (leaks the controller on failure). |
| | `IsPressed = _eventPinMode == PinMode.InputPullUp ? initialValue == PinValue.Low : initialValue == PinValue.High;` | Uses **`_eventPinMode`**, not `_gpioPinMode`: with `hasExternalResistor: true` the pin mode is plain `Input`, and only `_eventPinMode` still records the wiring (the external-resistor test proves this). Same decision shape as `PinStateChanged` (`if (_eventPinMode == PinMode.InputPullUp)`), so a reader sees one rule twice. *Rejected:* `initialValue == (pullUp ? Low : High)` (shorter but needs a second look). Sets the property directly: **no** `HandleButtonPressed()`, so no `ButtonDown`, no holding timer, no debounce bookkeeping. |
| Comment | the two `//` lines above | Says *why* (no edge at startup) and the one non-obvious rule (no events). The file has few comments; two lines kept it proportionate. |
| Doc | `<remarks>` on `GpioButton` | Public behavior change, stated where users of `GpioButton` look. See deviation 2. |
| Harness | `Button.Tests.csproj`: one `<Compile Include=… Link=…>` | Deviation 1. |
| Tests | `GpioButtonTests.cs` | Below. |

**A correction to brief §4's reasoning (for the lecture, not the code):** the brief says raising `ButtonDown` in
the ctor "would recreate #1715". Strictly, nobody *can* be subscribed while the constructor is still running
(`button.ButtonDown += …` only happens after `new` returns), so an event raised there reaches no one, and
`IsHoldingEnabled` can't be `true` yet either. The real reasons not to call `HandleButtonPressed()`: `ButtonDown`
means "a press **happened**", and none did; and it keeps the fix to "set state" with no side effects that a
later refactor (e.g. #1715's lazy subscription) could turn into real events.

**B. Behavior before → after** (verified by tests where marked)

| Situation | Before | After |
|---|---|---|
| Created while held (pull-up Low / pull-down High / external pull-up Low) | `IsPressed == false` | `true` (*verified*, Theory rows 1 and 3, external test) |
| Created while released | `false` | `false` (*verified*, guard rows) |
| Held at startup, debounce on, then released | release swallowed: no `ButtonUp`, no `Press` | `ButtonUp` + `Press`, `IsPressed` false (*verified*) |
| Held at startup, debounce off, then released | `ButtonUp` + `Press`, no prior `ButtonDown` | same (*unverified*, by reading: unchanged path) |
| Held at startup with `IsHoldingEnabled` | no `Holding` | still no `Holding`: the timer starts only on a press edge; release raises `Press` (*unverified*, by reading). Worth one line in the PR? Open question 4 |

**C. Each test: what it proves, and what it doesn't**

| Test | Proves | Doesn't prove |
|---|---|---|
| `If_Button_Is_Created_And_Disposed_Pin_Is_Opened_And_Closed` (smoke) | The mock sees the real wiring: open, pull-up mode, both edges, cleanup on dispose | Anything about `IsPressed` (passes before and after the fix) |
| `If_Button_Is_Created_IsPressed_Reflects_Pin_Level` (pull-up, Low → true) | **The bug** (red on `main`) and the fix for the default wiring | That the level is settled at that moment (U1/#1715): the mock answers instantly |
| …(pull-up, High → false) | Guard: a released button stays released | Nothing about the bug (green on `main`) |
| …(pull-down, High → true) | The fix honors pull-down wiring (red on `main`) | — |
| …(pull-down, Low → false) | Guard for pull-down | Nothing about the bug |
| `If_Button_Has_External_PullUp_And_Pin_Is_Low_At_Startup_Button_Is_Pressed` | Pin mode is `Input` and the fix still uses the wiring (`_eventPinMode`) (red on `main`) | External pull-*down* (symmetric code path, not tested separately) |
| `If_Button_Is_Held_At_Startup_With_Debouncing_Release_Raises_ButtonUp_And_Press` | The issue's real consequence: the swallowed release is gone (red on `main` at `Assert.True(buttonUp)`) | Real time: debounce never matters here because the release path doesn't check time |
| **None of them** | | Real hardware; the read-vs-callback order (U2) or its race (the mock fires synchronously on the test thread); settle time (U1) |

**D. Open questions** (for lecture 1 and the PR description; none blocks anything)
1. **U1** timing (a/b/c): built (a). Upstream.
2. **U2** order: built register-then-read; the remaining lost-update window (Step 0 §4) should be mentioned
   honestly in the comment/PR. Upstream.
3. `Fixes #2328` in commit 2's body: keep, or amend before pushing (deviation 5). Timothy.
4. Held-at-startup + holding enabled: say it in the PR, or leave it. Timothy/desktop.
5. Test naming copies `ButtonTests.cs` (`If_…_…`); the repo's newer tests use other styles. Kept local consistency.

**[CLI — 2026_10_01_19_06] Stage 6 action: our change on top of #2608 (`6a2f8973`). Verdict: works; one trivial conflict**

*Changed:* no commits on the real branch. Local refs only: `pr-2608-latest` (= `upstream` `pull/2608/head` @
`6a2f8973`) and a throwaway copy `try/2328-on-2608` (`ec05984d`, `fc9862c0`). The old local `pr-2608` @ `a1563be0`
is now stale. The real branch `fix/2328-gpiobutton-initial-state` is still at `395b9fbf`, and at Timothy's request
**its upstream tracking was removed** (`git branch --unset-upstream`; it used to track `upstream/main`). *Evidence:*
`007-on-top-of-2608.txt`.

1. **Rebase:** commit 1 (tests) conflicted only in `Button.Tests.csproj`, as desktop predicted. Commit 2 (fix)
   applied to `GpioButton.cs` without conflict. Our diff on top of #2608 is the same 3 files / +98 as on `main`.
2. **Resolution (implementation detail):** keep both. Our `<Compile … Link="MockableGpioDriver.cs">` goes in its
   own `<ItemGroup>` (as on `main`); #2608's `PackageReference` stays in the group with the `ProjectReference`.
   *Why:* our side of the diff stays exactly what it was on `main` (one added group), so a reviewer sees the
   same 4-line csproj hunk either way. *Rejected:* putting all three in one group (that would edit #2608's group;
   no functional difference).
3. **Build and tests (*verified*):** clean build, 0 warnings / 0 errors. **25/25 pass**: #2608's 18 `ButtonTests`
   (it rewrote them to use `FakeTimeProvider`) plus our 7 `GpioButtonTests`, unchanged. The held-at-startup +
   debounce test still passes on #2608's `TimeProvider`-based `ButtonBase`. This confirms desktop's reading
   that #2608 does not fix the bug and does not break our tests.
4. **Caveat about the evidence file:** its last line, `Exit code: 0`, is the exit status of a separator `echo`,
   not of `dotnet test` (a scripting slip). The real signal is the `Test Run Successful` / `Passed: 25` lines
   above it. The file was not edited (`evidence/README.md`).
5. **For later (not done):** when #2608 merges, rebase the real branch the same way. The conflict and its
   resolution will be the same if #2608 doesn't change again. **Rebasing changes the commit hashes; whether to
   push only after the rebase (no force-push during review) is Timothy's call.**

**[CLI — 2026_10_02_17_06] Stage 7 runbook step 2: rebased onto current `upstream/main`. Verified: 15/15, 0 warnings**

*Changed:* the real branch `fix/2328-gpiobutton-initial-state` was rebased from base `1eb0b2f6` onto `upstream/main` @
`95384e77` (the one new upstream commit, #2605, libgpiod `time_t`, touches nothing in `src/devices/Button`
or the mock). No conflicts. New hashes: `bd01e163 → 4c8682ce` (tests), `395b9fbf → 5f676802` (fix). Both are still
authored and committed by Timothy-Lee-Grant; there's no co-author trailer, and commit 2's body still ends with
`Fixes #2328` (open question 3, unchanged). Nothing pushed; no upstream tracking (D6). *Evidence:* `008-final-on-main.txt`.

1. **Same change, new base (*verified*):** `git diff upstream/main...HEAD` = the same 3 files / +98 / −0. With the
   `index` lines filtered out, the patch is byte-identical to the pre-rebase diff (`1eb0b2f6..395b9fbf`).
2. **Build and tests (*verified*):** `dotnet build src/devices/Button/tests/ --no-incremental` gave 0 warnings and
   0 errors. `dotnet test … --no-build` passed 15/15 (8 `ButtonTests` + 7 `GpioButtonTests`). Exit codes in the
   file are the real ones (`${pipestatus[1]}`), which fixes the 007 slip.
3. **For the PR's environment line:** macOS 26.3 arm64, .NET SDK 10.0.302 (global.json 9.0.306, `rollForward:
   major`); Button tests 15/15, no new warnings.

*Deviations:* none. *Why rebase now:* nothing is public yet, so new hashes cost nothing. Rebasing after the push
would need a force-push, which the repo's guidance asks us to avoid during review. *Rejected:* opening the PR on the
old base. GitHub would show it as one commit behind; harmless, but the final test run would then not match what
gets merged. **Not re-checked:** on top of #2608 after the rebase. #2608 is unchanged (`6a2f8973`) and `95384e77`
doesn't touch Button, so `007` should still hold (*unverified*).

**[CLI — 2026_10_06_00_50] Stage 8: PR #2611 conflict after #2608 merged. Resolved by merging `main`; pushed by Timothy**

*Changed:* merge commit `5adc9b9c` on `fix/2328-gpiobutton-initial-state` (parents `5f676802` + `upstream/main` @
`336e4696`), authored by Timothy, no trailer. Timothy pushed it (plain push, 00:48). Our two commits are unchanged.
*Evidence:* `009-merge-main-after-2608.txt` (trial in a throwaway worktree), `010-merge-commit-on-branch.txt` (real branch).

1. **What happened:** #2608 merged on 2026-10-05 (`bc9baa95`, squashed). It added `TimeProvider.Testing` into the same
   `<ItemGroup>` of `Button.Tests.csproj` where our branch changes the group, so GitHub flagged #2611 as conflicting.
   `GpioButton.cs` auto-merged. This is the conflict predicted in the Stage 6 / `007` check.
2. **Resolution (implementation detail):** keep both, as in `007`: our `<Compile … Link>` in its own group, then
   #2608's `PackageReference` with the `ProjectReference`. The PR diff vs `main` stays 3 files / +98 / −0, the same
   4-line csproj hunk as before.
3. **Verified:** 0 warnings / 0 errors, **25/25** (#2608's 18 + our 7) on the merged `main`. Note: the merged #2608
   differs from the `6a2f8973` we tested in `007` by 7 lines in `ButtonBase.cs`; 009/010 cover the real one.

*Deviations:* the Stage 8 rule says "ask desktop before resolving"; Timothy chose to proceed (he began the merge
himself). *Why merge, not rebase:* the PR is under review; a rebase rewrites the hashes and needs a force-push, which
the repo's guidance asks to avoid. A merge commit only adds on top, so `git push` is a fast-forward.
*Rejected:* rebase + force-push (B); GitHub's "Resolve conflicts" button (C: same result, but nothing is tested
before it lands). Squash-merge upstream would likely drop the merge commit from `main`'s history (*unverified*).

# Stage 6 (Understanding)

*(Desktop: lecture list and links; Timothy: reading confirmations and teach-back; G3.)*

**[Desktop — 2026_10_01_00_25] Lecture 1 spec: "The change, end to end"** (written tonight from the CLI's Stage 5 entries,
`evidence/` and `006-diff.patch`; destination `../../iot_concepts/` or this issue's `report/`, linked here)

Timothy asked for exactly these parts:
1. **The problem in the issue**, shown happening: the two broken paths (property wrong; release swallowed with
   debounce), traced through the real code.
2. **What changed, line by line, and why**: each choice and the alternatives rejected (from the CLI's *why*s).
3. **How it fits together and solves the issue**: the constructor's new sequence, the callback, `ButtonBase`, and
   why no events are raised.
4. **The tests**: the harness (why a fake driver), each test with its **red output before the fix and green output
   after**, what each one proves, and what none of them proves (real hardware timing, the settle question).
5. **The comment**: every sentence explained; what it commits him to; what each possible maintainer answer (timing
   a/b/c, order, "#2608 will do it", "not wanted") would mean for the code and for him.
6. Teach-back checklist (5–10 ideas). Kept as short as these six parts allow; concept lectures (Moq, events,
   `GpioController`) only if lecture 1 shows they're needed.

**[Desktop — 2026_10_01_00_52] Review of Steps 0–7, and lecture 1 written**

Review: the CLI's work holds up. Checked against the evidence: 4 red on assertions (`003`), 15/15 green (`004`), 0
warnings under `TreatWarningsAsErrors`, 5 stable runs, diff 3 files / +98 / −0 (`005`, `006`). Its deviations are
all sound implementation details (linked mock, doc remark on `GpioButton` so `ButtonBase.cs` is untouched, guard rows
kept). Its two corrections to the desktop's own reasoning are right and are taught in the lecture: (1) the U2
"lost-update window" remains with register-then-read; (2) raising events in the ctor reaches no subscriber, so
"would recreate #1715" was the wrong reason. **Not done:** `/handoff` for this session (STATUS.md and a session
report are missing).

Lecture 1: [`../lectures/001-the-change-end-to-end.md`](../lectures/001-the-change-end-to-end.md) (all six parts of
the spec, plus a 5-minute "watch it fail, then pass" start). §5.3 has a **revised comment** (v2 of Stage 4's draft):
it now says the change exists, and states the ordering trade-off honestly. Stage 4's v1 draft is superseded.

**[Desktop — 2026_10_01] Audio version of lecture 1** (Timothy asked):
[`../lectures/audio/001-audio-the-button-that-was-already-pressed.md`](../lectures/audio/001-audio-the-button-that-was-already-pressed.md)
+ its NotebookLM customization prompt (`.prompt.md`, paste into "Customize"; don't upload as a source).

**[Desktop — 2026_10_01_19_30] PR #2608 moved: impact check, and comment v3**

New commit on #2608: `6a2f897` (2026-10-01, by **joperezr**, Jose Perez Rodriguez, Microsoft; Copilot co-authored):
"Use TimeProvider for button timing and deterministic tests". Still open, no reviews. Touches `ButtonBase.cs` (time
fields → `TimeProvider`), one line of `GpioButton.cs` (the short ctor's defaults), `Button.Tests.csproj` (adds
`Microsoft.Extensions.TimeProvider.Testing`), `ButtonTests.cs`, `TestButton.cs`, `README.md`.

Impact on our change (textual rebase simulation of our diff onto `pr-2608`; **not built or tested**):
- **The bug still exists on #2608:** `IsPressed` still starts `false`; the release guard
  `if (_debounceTime.Ticks > 0 && !IsPressed) return;` is unchanged; the `GpioButton` ctor still never reads the pin.
- `GpioButton.cs`: **applies cleanly** (different lines).
- `Button.Tests.csproj`: **one trivial conflict**: both add a line in the same spot. Resolution: keep both.
- Our tests don't use time, so `TimeProvider` shouldn't affect them (*unverified* until run on top of #2608).

**Action for the CLI (next session, before Step 9):** rebase a **copy** of our branch onto
`upstream/pull/2608/head`, resolve the csproj by keeping both lines, run the Button tests, save
`evidence/007-on-top-of-2608.txt`. Don't move the real branch. Comment v3: `../conversation/` entry #6.

**[Desktop — 2026_10_02_21_03] Lecture 002 written** (Timothy asked, while waiting for review):
[`../lectures/002-the-contribution-process-end-to-end.md`](../lectures/002-the-contribution-process-end-to-end.md):
repositories/remotes/branches/PRs, commits and issue references, the week's timeline, reading comments (Friday's
reply decoded), the hunt method with a practice issue (#1663), verification and evidence, coordinating with #2608.

# Stage 7 (Contribution)

*(PR description draft; Timothy's final test run, push, PR link.)*

**[Desktop — 2026_10_02_16_48] Step 9 runbook + PR description draft**

`upstream/main` moved one unrelated commit since our base (`95384e7`, libgpiod V1 `time_t`, #2605). #2608 unchanged
(`6a2f897`), not merged.

1. **G3 check:** Timothy answers TB 002's three retrieval questions (desktop chat).
2. **CLI (no push):** rebase `fix/2328-gpiobutton-initial-state` onto current `upstream/main` (safe: nothing is
   public yet), rebuild, run Button tests → `evidence/008-final-on-main.txt` (record the new commit hashes). Confirm
   the diff is still 3 files / +98.
3. **Timothy, in his own terminal:** `git push -u origin fix/2328-gpiobutton-initial-state`.
4. **Timothy, on GitHub:** open the PR (base `dotnet/iot` `main` ← head `Timothy-Lee-Grant/iot`
   `fix/2328-gpiobutton-initial-state`), paste the description below, fill the environment line from evidence 008.
5. Paste the PR link to desktop. From then on: reply to review within 48 h; **no force-push during review**.

```markdown
Fixes #2328

### Problem
`GpioButton` only learns its state from pin edges, so a button that is already held down when it's created reports
`IsPressed == false` until the first edge. With debounce enabled it's worse: `HandleButtonReleased` returns early
when `!IsPressed`, so the first release of a button held at startup is swallowed (no `ButtonUp`/`Press`).

### Change
After registering the edge callback, the `GpioButton` constructor reads the pin once and sets `IsPressed` from the
active level (Low for pull-up, High for pull-down, using the wiring even with an external resistor). It only sets
state; no events are raised. The read is inside the existing `try`, so a failed read gets the same cleanup as a
failed `OpenPin`. A `<remarks>` on `GpioButton` documents the behavior.

### Tests
New `GpioButtonTests` run the real `GpioButton` over `MockableGpioDriver` (linked from `System.Device.Gpio.Tests`,
as Tca955x/Gpio/Board do): initial state for pull-up/pull-down × Low/High, the external-resistor case, and a
held-at-startup button with debounce whose release now raises `ButtonUp` and `Press`. Four of the new tests fail on
`main` and pass with the change; the first commit adds the tests alone so this can be checked.

### Notes (from the discussion in #2328)
- No settle delay: the pin is read immediately, per the discussion.
- Ordering: the pin is read *after* registering the callback, so an edge between the two can't be lost. A small
  window remains where a callback running between the read and the assignment could be overwritten; closing it
  fully would need locking in `ButtonBase`, which I've left alone to avoid colliding with #2608.
- #2608: this doesn't touch `ButtonBase.cs`. I checked it on top of #2608's latest commit (6a2f897): the only
  overlap is one line in `Button.Tests.csproj` (keep both), and all Button tests pass together.

Verified on macOS arm64, .NET SDK <version from evidence 008>: Button tests <N>/<N> pass, no new warnings.
This change was developed with help from an AI assistant; I've reviewed and understand every line.
```

**[Timothy — 2026_10_02_17_34, via desktop] PR opened: https://github.com/dotnet/iot/pull/2611**

Title "Initialize GpioButton.IsPressed from the pin level at startup" (per desktop's suggestion), description from the
draft above. Signed the .NET Foundation CLA (`@dotnet-policy-service agree`) → `license/cla` ✓. State on opening:
the `dotnet.iot` CI workflow is **awaiting maintainer approval** (normal for a first-time contributor), review
required (1 approval from someone with write access). G3: the three retrieval questions were not answered before
opening; Timothy chose to proceed (recorded as-is).

# Stage 8 (Review)

*(One entry per review comment and its resolution.)*

Rules: reply within 48 h; **no force-push**: add commits; if #2608 merges and the PR shows a conflict, ask desktop
before resolving (merging `main` into the branch avoids a force-push).
