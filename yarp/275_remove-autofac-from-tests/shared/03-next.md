# 03 — Next: the current work order

> Owner: desktop Claude. CLI: read-only.

## WO-1 · Setup check and read-only tour  *(mode: M0 Tutor)*

**Goal:** YARP builds and its tests run on the Mac; Timothy understands what AutoMock does in these tests. No edits.

**Steps (suggested):**
1. Check remotes and `./restore.sh`; record the SDK version and disk used (`du -sh .dotnet`).
2. `dotnet test test/ReverseProxy.Tests/` → `shared/evidence/001-baseline-reverseproxy-tests.txt`. Note whether
   the `HttpSysDelegator*` tests ran, were skipped, or were excluded on macOS.
3. Tour `TestAutoMockBase.cs`, then `LoadBalancingPoliciesTests.cs` (simplest) and `ForwarderMiddlewareTests.cs`
   (shared-mock trap): what AutoMock creates, and what the hand-built version would look like (explain; don't edit).
4. Summarize `CONTRIBUTING.md` and the PR template rules in the session report.

**Done when:** baseline evidence saved; the macOS question answered; Timothy can explain the shared-mock trap.

**Not doing:** any edit until a maintainer says the work is wanted (P1).

## Queued

- **WO-2** Rewrite the two small files. Suggested mode: M1 or M2.
- **WO-3** Rewrite the rest. Suggested mode: M3 (compare with WO-2).
- **WO-4** Delete the helper and package refs; preservation evidence; PR description draft.

## Done

(none yet)
