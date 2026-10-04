# open-telemetry/opentelemetry-dotnet (+ contrib): project orientation

> Shared context for every OpenTelemetry .NET issue folder (core repo and `opentelemetry-dotnet-contrib`). Started
> 2026-10-04. No issue folders yet. Scouted items: L, N, O, Q, R in
> [`../scouting/001-issue_shortlist_sept_2026.md`](../scouting/001-issue_shortlist_sept_2026.md) (📡 Telemetry track).

## 1. What's in this folder

| Path | What it is |
|---|---|
| [`opentelemetry-dotnet_concepts/`](opentelemetry-dotnet_concepts/) | Lecture notes: 001 (reading) and A001 (audio), the full orientation |

## 2. What OpenTelemetry .NET is

The .NET implementation of the OpenTelemetry standard. In .NET the **API is mostly in the runtime**
(`ActivitySource`/`Activity`/`Meter`/`ILogger`); this project is the **SDK** that subscribes to those
(`ActivityListener`/`MeterListener`), samples, batches and exports, plus `OpenTelemetry.Api` (Baggage, propagators).
Not middleware: a subscriber. Not a backend: it stops at the wire (OTLP).

| Repo | Holds |
|---|---|
| `opentelemetry-dotnet` (core) | API, SDK, OTLP/Console/InMemory/Prometheus/Zipkin (deprecated) exporters, Extensions.Hosting, Propagators, Declarative config (experimental). Released together (`core-X.Y.Z` tags) |
| `opentelemetry-dotnet-contrib` | ~50 components (instrumentation for AspNetCore/Http/SqlClient/Runtime/..., resource detectors, extra exporters). Each with owners in `.github/component_owners.yml`, released separately |
| `opentelemetry-dotnet-instrumentation` | Zero-code auto-instrumentation agent (out of scope for now) |

## 3. Upstream facts (checked 2026-10-04)

| | |
|---|---|
| Core `main` | `f9dd754` (2026-10-02). Latest stable **1.19.1** (`OTelLatestStableVer` in `Directory.Packages.props`) |
| Contrib `main` | `d3588e5` (2026-10-03) |
| `global.json` SDK | **10.0.401** in both repos (the 2026-09-26 scouting note about .NET 11 + Codespaces is out of date) |
| Build | `dotnet build OpenTelemetry.slnx --configuration Release` (warnings = errors in Release) |
| Test | `dotnet test <project> --filter "FullyQualifiedName~X" --framework net10.0`; tests run serially (`build/xunit.runner.json`); `net462/net472` only on Windows |
| Lint | `dotnet format OpenTelemetry.slnx --no-restore --verify-no-changes`; `python3 ./build/scripts/sanitycheck.py` (**no non-ASCII**, no trailing whitespace) |
| TFMs | Libraries: all supported .NET + `netstandard2.0` + `net462` (hence `#if NET` blocks) |
| Conventions | Central package management (no `Version=` in csproj); `.publicApi/{Stable,Experimental}/PublicAPI.Unshipped.txt` (IDE fix, never edit `Shipped`); `src/Shared/*.cs` linked via `<Compile Link=...>`; SPDX header; nullable mandatory; XML docs on public members |
| Tests | xUnit; `InMemoryExporter<T>` sinks; build the provider inside each test; helpers in `test/OpenTelemetry.Tests/Shared/` |
| Read first | `AGENTS.md` (architecture + commands), `REVIEW.md` (what reviewers check), `CONTRIBUTING.md`, `VERSIONING.md` |
| Not yet tried | Nothing above has been run on Timothy's machines yet |

## 4. Contribution rules

- `help wanted` / `good first issue`; contrib: comment first and ping the **component owner**.
- **Find a buddy:** `#otel-dotnet` on CNCF Slack; regular SIG meeting open to anyone.
- **CLA:** CNCF/Linux Foundation, via the bot on the first PR (separate from the .NET Foundation CLA signed for iot).
- **CHANGELOG** line under `## Unreleased` in each affected component, linking the **PR**; `**Breaking Change**:` prefix when relevant. None for infra/test-only changes.
- Approval from an approver/maintainer; open ≥ 1 working day (trivial docs exempt). Small focused PRs; benchmarks for perf changes.
- AI-assisted PRs are accepted (`CONTRIBUTING.md`); spammy/incorrect ones are closed. Disclose briefly anyway (root `CLAUDE.md` §5).
- Default workflow: mode P (`../ai-workflow/default-workflow.md`).

## 5. Issue folders

None yet.
