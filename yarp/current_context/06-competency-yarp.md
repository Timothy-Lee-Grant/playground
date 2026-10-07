# 06 · Timothy's current understanding of YARP (and the ideas under it)

> **What this is for:** so any session (CLI or desktop) can pitch an explanation or a lecture at the right level:
> skip what he knows, build on it, and spend the words on what's new. Read it before explaining anything
> substantial or writing a lecture.
>
> **Rules for updating** (both CLI and desktop may edit):
> - **Evidence only**, dated, with the source: he said he read X; he explained Y correctly (teach-back, chat); he
>   asked Z (which shows what's missing); he did W himself. **Never** raise a level because a document was
>   generated or a session went quietly.
> - Neutral wording: this repo is public. Record what he can and can't yet do, not judgments about him.
> - Last full review: 2026-10-07 (desktop, from the record so far).

## 1. The scale

| Level | Meaning | Practical consequence for you |
|---|---|---|
| **0** Not met | Hasn't encountered it | Introduce from purpose up; run something first |
| **1** Exposed | Has read, heard or used it, but hasn't explained it back | Re-introduce briefly; don't assume the mechanism |
| **2** Can explain | Has explained it correctly in his own words (teach-back or chat) | Build on it; one-line reminder is enough |
| **3** Can apply | Has used it on his own in real work, unprompted | Just use it |

## 2. Competency map

| Area | Level | Evidence (date, source) | Next step that would raise it |
|---|---|---|---|
| What a reverse proxy is for; routes → clusters → destinations | 1–2 | Used YARP as the gateway in his own LLM_Monitor project (2026-07); configured it for the #1764 repro (2026-09-26) | Explain the config model back |
| Connections, WebSockets through YARP, `ActivityTimeout`, keep-alives | 2 | Ran the #1764 three-process repro, found the 2-minute keep-alive vs 100 s timeout gotcha, wrote the docs PR (AspNetCore.Docs#37747, 2026-09-28) | — |
| YARP's internal pipeline (Clerk/ProxyPipelineInitializer → affinity → load balancing → passive health → Forwarder → HttpForwarder) | 0–1 | Covered by audio A001 (2026-10-03), **not yet listened to** | Listen to A001 episode 1; trace one request in the debugger |
| `ProxyConfigManager`, config providers, hot reload | 0 | Only in A001 (not listened) | — |
| Health checks, load-balancing policies, session affinity, transforms | 0–1 | Only in A001 (not listened) | — |
| ASP.NET Core host: builder → build → run | 2–3 | His strongest .NET model; uses it as the template for new systems (teach-back 001, 2026-09-29) | — |
| Middleware pipeline as a chain of `RequestDelegate` calls; endpoint routing | 1 | Has written middleware-based apps; the delegate-chain mechanism hasn't been explained back. Covered in ASP.NET Core audio A001 and YARP A001 (both not listened) | Explain "who calls whom" in the pipeline |
| Delegates and events (`=` vs `+=`, immutability, `event` = private field + add/remove) | 2 | Teach-back 001 (2026-09-29), after corrections | Re-test due |
| Dependency injection: registration vs resolution, lifetimes (singleton/scoped/transient), interfaces as seams | 1 | Uses DI daily; described this layer himself as "fuzzy enough to stumble through" (2026-09-29) | A short worked example in #275: who builds `ForwarderMiddleware` in production vs in a test |
| async/await, `Task`, `CancellationToken` | 1 | Listed in his profile as an area he wants to deepen | Explain the activity-timeout token in `HttpForwarder` |
| Unit tests with Moq (`new Mock<T>()`, `.Object`, `Setup`, `Verify`, loose vs strict) | 1–2 | First met in iot#2328 (2026-10-01); asked what was real vs fake and how mocks were set up; the arrange/act/assert + object-graph explanation worked | #275 lecture 1 |
| Autofac `AutoMock`, `TestAutoMockBase`, the shared-mock and loose-default traps (#275) | 0–1 | Brief and plan exist (2026-10-01); no lecture yet | #275 lecture 1 |
| YARP build: `global.json`, `restore.sh`, repo-local `.dotnet`, Arcade, `Versions.props` | 1 | Read in plan/brief; hasn't built YARP himself yet | Run `./restore.sh` and one test project himself (lecture 002 §0) |
| CI (Azure Pipelines on PRs) | 1 | Asked good structural questions (2026-10-01); CI lecture not read yet | — |
| Git: remotes, tracking, branches, rebase, fork vs upstream | 2 | Teach-back 003 (2026-10-04): rebuilt the remote/tracking model nearly exactly | Scenario recipes for review rounds |
| Contribution process: issue comment → PR → CLA → review | 2 | Did it end to end on dotnet/iot#2611 (2026-10-02) | — |

## 3. Character names he may know

These names come from lectures. **Only names from documents he has read or listened to count as known**; the rest
are free to reuse (keeps lectures consistent), but introduce them as new.

| Name | Is | From | He knows it? |
|---|---|---|---|
| the Gatekeeper, Caller, Echo, Pump, Watchdog | YARP; client; backend; `StreamCopier`; activity timeout | `yarp_concepts/001` | reading status unknown |
| the Listener, Job Ticket, Assembly Line, Sorter, Supply Room | Kestrel; `HttpContext`; middleware pipeline; endpoint routing; DI container | ASP.NET Core A001, YARP A001 | not yet listened |
| the Rulebook, Librarian, Clerk, Proxy Note, Loyalty Desk, Dispatcher, Inspector, Patrol, Courier, Editor, Outbound Line | config; `ProxyConfigManager`; `ProxyPipelineInitializerMiddleware`; `IReverseProxyFeature`; session affinity; load balancing; passive health; active health; `HttpForwarder`; transforms; `HttpMessageInvoker` | YARP A001 | not yet listened |

## 4. YARP reading ledger

States: Generated → Read / Listened (he says so) → Questions → Teach-back → Re-tested. Update only from what he says.

| Document | Generated | State | Notes |
|---|---|---|---|
| `link/1764_websocket_idle_timeout/` (concept notes, lab report) | 2026-09-26/27 | Worked through while doing #1764 | He ran the experiments himself |
| `link/yarp_concepts/001-the-gatekeeper-in-the-middle.md` | 2026-09-27 | unknown | Ask |
| `exercises/lectures/001-yarp-websocket-activity-timeout.md` | 2026-09 | unknown | |
| `link/yarp_concepts/audio/001-audio-yarp-from-the-ground-up.md` (audio, 2 episodes) | 2026-10-03 | not yet listened | |
| `exercises/aspnetcore/aspnetcore_concepts/audio/001-…` (audio, 3 episodes) | 2026-10-03 | not yet listened | Meant to come before YARP A001 |
| `link/issues/275_remove-autofac-from-tests/` brief + plan | 2026-10-01 | approved the plan (G1, 2026-10-02); reading depth unknown | |

## 5. Changelog

| Date | Who | Change |
|---|---|---|
| 2026-10-07 | desktop | v1, from the record so far (teach-backs 001–003, #1764, iot#2328, profile) |
