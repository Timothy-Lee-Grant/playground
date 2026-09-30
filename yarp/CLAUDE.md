# dotnet/yarp: project orientation

> Shared context for every YARP issue folder. Started 2026-09-30 (the first issue, `1764_*`, predates this file and
> keeps its own legacy `CLAUDE.md`).

## 1. What's in this folder

| Path | What it is |
|---|---|
| [`yarp_concepts/`](yarp_concepts/) | Lecture notes on YARP / reverse-proxy concepts |
| [`1764_websocket_idle_timeout/`](1764_websocket_idle_timeout/) | [#1764](https://github.com/dotnet/yarp/issues/1764) docs PR (legacy v1 layout; don't restructure) |
| [`275_remove-autofac-from-tests/`](275_remove-autofac-from-tests/) | [#275](https://github.com/dotnet/yarp/issues/275): remove Autofac from the tests (v2 + AI workflow). Queued |

## 2. What YARP is

A reverse-proxy **toolkit** built on ASP.NET Core: you host it in your own ASP.NET Core app and configure routes →
clusters → destinations. Unlike dotnet/iot, it plugs into ASP.NET Core's middleware pipeline (it *is* middleware).

## 3. Building and testing (checked 2026-09-30)

| | |
|---|---|
| `global.json` | SDK `11.0.100-rc.1.26420.103`; extra runtimes 8.0 and 9.0 installed by the scripts |
| Setup | `./restore.sh` installs the SDK into `.dotnet/` inside the clone; `source activate.sh` puts it on `PATH` |
| All tests | `./test.sh` (Microsoft.Testing.Platform) |
| One project | `dotnet test test/ReverseProxy.Tests/` |
| Machines | **Mac or Codespaces.** The Linux desktop can't run .NET 11 (x86-64-v2) |
| Build infra | Arcade (`eng/`), package versions in `eng/Versions.props` |

## 4. Contributions

To fill in from `CONTRIBUTING.md` and the PR template (yarp#275 WO-1 records them).

## 5. Where forks live

| Issue | Workspace | Fork clone |
|---|---|---|
| #275 | `~/Desktop/projects/oss-work/yarp-275/` | `develop/yarp` (origin `Timothy-Lee-Grant/yarp`); via the issue's `workspace/setup.sh` |
