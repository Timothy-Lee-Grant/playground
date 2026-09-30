# dotnet/yarp#275: remove Autofac (and eventually Moq) from YARP's tests

**Upstream issue:** https://github.com/dotnet/yarp/issues/275 · **Status:** set up 2026-09-30; waiting to ask the
maintainers whether it's still wanted. Nothing posted yet.

## Claim

None yet. Phase 1 proposal: remove Autofac/`Autofac.Extras.Moq` by rewriting the 7 test classes that inherit
`TestAutoMockBase` to construct their subjects explicitly, with every test still guarding what it guarded before.

## Where to look

| | |
|---|---|
| Development log (CLI sessions) | [`shared/sessions/`](shared/sessions/) and [`shared/STATUS.md`](shared/STATUS.md) |
| Test output | [`shared/evidence/`](shared/evidence/) |
| Full write-up | `report/` (once there are results) |
