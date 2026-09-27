# Developer Persona: Timothy Grant

# Mission

My goal is to become an exceptional backend and infrastructure software engineer capable of working at companies such as Microsoft, Google, Meta, Amazon, TikTok, or similar large-scale technology organizations.

I am optimizing for long-term engineering excellence rather than quick tutorials or copy-paste solutions. Whenever possible, teach me the underlying principles instead of only solving the immediate problem.

---

# Current Experience

## Professional

I currently work as a software/firmware engineer in the embedded systems space.

My daily work includes:

* Embedded C/C++
* Raspberry Pi development
* Microcontrollers
* Hardware/software integration
* Linux environments
* Device communication (I2C, SPI, etc.)
* Python scripting
* Some C#/.NET development

Although I have professional software engineering experience, much of it is closer to firmware and hardware integration than modern cloud backend development.

---

# Programming Background

## Comfortable With

* C
* C++
* Python
* C#
* Basic Bash
* Git

I understand:

* Functions
* Classes
* OOP
* Data structures
* Basic algorithms
* Memory management
* Debugging
* Reading existing codebases
* Working from documentation

I am comfortable reading medium-sized codebases but am still developing confidence navigating very large enterprise repositories.

---

# Current Learning Priorities

My highest priorities are:

1. Backend Engineering
2. Distributed Systems
3. Cloud Infrastructure
4. AI Engineering
5. High-performance architecture

Specifically I want to master:

* ASP.NET Core
* Java Spring Boot
* REST APIs
* gRPC
* Microservices
* Event-driven architecture
* Message queues
* Redis
* PostgreSQL
* MongoDB
* Docker
* Kubernetes
* CI/CD
* Observability
* Distributed caching
* Service discovery
* API Gateways
* Authentication
* Authorization
* Horizontal scaling
* Performance optimization

---

# AI Engineering Goals

I want to become an AI-native engineer.

I actively use coding agents and want to understand:

* Agentic workflows
* MCP servers
* Tool calling
* Vector databases
* Embeddings
* Semantic search
* Retrieval-Augmented Generation (RAG)
* Multi-agent architectures
* Prompt engineering
* Evaluation systems

Do not treat AI as a black box. Explain how systems work internally whenever possible.

---

# Areas I'm Actively Growing

Areas I'm deliberately building depth in:

## Distributed Systems

I have limited intuition for:

* Event-driven systems
* Pub/Sub
* Kafka-style architectures
* Eventually consistent systems
* CAP theorem tradeoffs
* Distributed transactions
* Consensus algorithms
* Coordination between services

---

## Asynchronous Programming

I want deeper understanding of:

* async/await internals
* Task scheduling
* Thread pools
* Non-blocking I/O
* Synchronization
* Race conditions
* Deadlocks
* Lock-free programming

---

## Large System Design

I want to improve at:

* Architecture decisions
* Scalability
* Reliability
* Fault tolerance
* Load balancing
* Caching strategies
* Database partitioning
* System decomposition

---

## Reading Large Codebases

When explaining a project:

Start by explaining:

* Overall architecture
* Folder organization
* Control flow
* Dependency relationships

before diving into implementation details.

Think like a senior engineer onboarding a new team member.

---

# Learning Style

I learn best when explanations proceed from:

High-level architecture

↓

Major components

↓

Interactions

↓

Control flow

↓

Implementation details

↓

Edge cases

↓

Performance considerations

Avoid jumping immediately into code without context.

---

# Preferred Teaching Style

When teaching:

* Explain why something exists.
* Explain what problem it solves.
* Explain alternative designs.
* Explain tradeoffs.
* Explain industry best practices.
* Explain historical context when useful.

Assume I want deep understanding rather than surface familiarity.

Analogies:

* The type of analogies that I like are the ones that personify the concepts which I am struggling with.
* I want to be able to see the different characters of each component, be able to give them a name or a title, understand who they are, what they are trying to accomplish, who they interact with, and their place within the larger ecosystem.

---

# Documentation Preferences

When generating markdown files:

Use:

* Clear headings
* Tables
* Diagrams (ASCII if necessary)
* Examples
* Analogies
* Step-by-step walkthroughs
* Code snippets
* References to source files

Include sections like:

* What problem is being solved?
* Why is this design chosen?
* What should I pay attention to?
* Common mistakes
* Interview relevance
* Real-world production usage

---

# Career Objective

My objective is to become a senior-level engineer capable of designing and building large-scale backend systems rather than simply implementing features.

I want to develop strong engineering intuition so that I can reason about unfamiliar systems, contribute to major open-source projects, and perform effectively in highly technical interviews.

---

# Active Projects

## Open-source contributions (this repo, Sept 2026, in progress)

This repo (formerly a general playground) became an open-source workbench on 2026-09-27: scout issues, reproduce
them in small running experiments, save the output as evidence to link from upstream PRs, and write lectures for
the concepts along the way. First search pass (2026-09-26, `scouting/001-issue_shortlist_sept_2026.md`) covered
dotnet/iot, the MCP C# SDK, YARP and OpenTelemetry .NET, with a deliberate mix of 🛠️ contribute and 📖 learn-only
items and a "one active PR at a time" rule. First pick: **YARP #1764** (docs, WebSocket idle timeout). Instead of
copying the maintainer's explanation into the docs, he had Claude build a three-process repro (client → YARP →
echo server), ran it, and confirmed the abort at `ActivityTimeout`, the `KeepAliveInterval` fix, and a gotcha the issue doesn't mention:
ASP.NET Core's default 2-minute keep-alive is longer than YARP's 100 s timeout, so "turn on keep-alives" alone
doesn't fix it. While researching he also found that the Timeouts docs page already covers most of the issue,
which shrinks the real change to a cross-reference. That's a good example of checking before contributing. Nothing
posted upstream as of 2026-09-27.

Second pick (2026-09-27): **dotnet/iot #2403** (`GpioPin` event handlers get a driver-internal object as `sender`),
in the new per-project layout (`iot/` with `iot_concepts/` lectures and an issue folder split into `conversation/`,
`sample/`, `report/`). He designed that layout himself: a single linear conversation log (questions, answers and
progress together, readable top to bottom by a later Claude CLI session), a lab-report-style `report/` modeled on
his undergraduate physics/chemistry reports, and project-level concept lectures instead of per-issue ones.

## LLM_Monitor (2026, in progress)

A self-built AI orchestration platform. Phase 1 was 100% hand-written code (AI used only for review/mentorship docs). Phase 2 (July 2026, plan 001) introduced a disciplined AI-collaboration workflow: Timothy directs a staged process (design → discussion → plan → step-by-step permissioned implementation → verification), with every decision and deviation logged in Documentation/AI_Implementation_Plans. Microservices: C#/.NET YARP gateway, Python/Flask + LangChain/LangGraph service, pgvector, Ollama — all Docker-composed with mock/live modes.

**Skills demonstrated so far:** Docker Compose profiles/healthchecks/startup-ordering, YARP reverse proxy + ASP.NET middleware pipeline, REST API contract design (single contract doc, snake_case wire convention, contract-shaped errors), pipeline registry pattern for dispatch/growth, LangChain chains + compiled LangGraph graphs sharing components, pgvector RAG with idempotent (content-hash) ingestion and mock-embeddings testability, factory pattern for mock/live models, gunicorn process model, honest pytest suite + CI, directing an AI implementation through explicit staged permissions (strong interview story: found that CI had been green while installing zero dependencies).

**Current roadmap (July 2026, see Documentation/AI_Suggestions/006):** OpenWebUI frontend via an OpenAI-compatible API facade with SSE streaming; YARP as a real API gateway; LangGraph state-machine agent (policy check → RAG → tool loop) with Postgres checkpointer memory (short- and long-term); fully local observability (Langfuse + OpenTelemetry/Prometheus/Grafana with C#→Python distributed traces); and an AI evaluation harness (golden dataset, hit@k/MRR, RAGAS, LLM-as-judge, regression-gated CI). Goal: a portfolio project demonstrating AI-engineering operational maturity (observe/evaluate/defend), targeted at Microsoft AI software engineer roles.

---

## Hand-written practice exercises (`hand_experiments/`, Aug 2026)

Deliberately hand-written (no AI code) integration exercises: `reactive/` (pub/sub, delegates, `event`,
Rx.NET concepts), `kafka/` (producer/consumers with a shared contracts assembly on KRaft-mode Kafka in Docker
Compose), and `redis/` (planned: Redis Pub/Sub as the fire-and-forget counterpart to Kafka). Each has a companion
lecture in `hand_experiments/lectures/`.

## Tool_Box (July 2026, starting)

An MCP tool platform: a C#/.NET server (official ModelContextProtocol SDK, .NET 10) exposing toolsets to AI agents — Claude Desktop/Code and, over HTTP, LLM_Monitor's LangGraph tool loop. Goals: give LLM_Monitor real tools, learn packaging/cross-project consumption (dotnet tool, Docker image, NuGet), and build a portfolio-grade platform. Architecture: thin Host + Core plumbing (bounded output, audit, read/write tool tiers) + independent toolset libraries, stdio first then streamable HTTP. Plan 001 (MVP foundation) implemented 2026-07-16 via the staged-permission process: Host/Core/Basics projects, Directory.Build.props with warnings-as-errors, stderr-only logging (stdout = protocol), OutputLimiter discipline, TimeProvider-injected clock, 17 tests including a reflection test enforcing descriptions-as-prompts, honest CI with a deliberate-red ritual, tool catalog + 6 ADRs. Debugging story: Inspector handshake failure root-caused to missing .NET 10 runtime (preview SDK compiles what it can't run). Plan 002 (same day): streamable HTTP as second transport with measured zero toolset diffs, stateless mode, integration tests via the SDK's own client on ephemeral ports, multi-stage non-root Docker image with layer-cache-ordered restore, CI job that boots the container and polls /health, ADR-008 security posture (isolation-not-auth, AllowedHosts DNS-rebinding pin), LLM_Monitor consumption walkthrough (langchain-mcp-adapters). Debugging stories: NU1510 redundant-package after FrameworkReference; three-round SDK API-drift saga ending in "read the docs first" (docs also yielded Stateless + AllowedHosts improvements); dockerfile→Dockerfile case-sensitivity trap defused pre-CI. Plan 003 (Voxel World Builder) shipped: first stateful toolset (ADR-009 singleton world), first companion `IHostedService` (ADR-010, browser viewer over WebSocket), call-economy tool design (form primitives — box/cylinder/cone/sphere/tube/mirror — instead of per-block placement), and ADR-011/012 (a consciously reviewed supersession of an earlier security ADR, and a loopback-vs-wildcard bind bug that only appeared through Docker).

**Released v1.0.0 (and v1.0.1/v1.0.2), July 2026.** Plan 005 took the existing platform — Host/Core, 2 toolsets, 15 tools, 2 transports, 77 tests, 12 ADRs — and made it releasable: a multi-arch (`linux/amd64` + `linux/arm64`) image published to GHCR via a tag-gated workflow, consumed for real by LLM_Monitor's compose over a pinned version tag. Deliberately shipped *one* of the three packaging shapes named as learning goals (Docker image) and named the other two (dotnet tool, NuGet) as explicitly deferred rather than half-built. Release-phase debugging stories, all logged in plan 005: the amd64-only first publish (`docker/build-push-action` without an explicit `platforms:` key builds only for the runner's architecture — and Timothy noted honestly that he'd reviewed that same workflow earlier, caught two other bugs, and missed this one); GHCR rejecting the mixed-case `github.repository` as an image name; the Azure-hosted-runner apt mirror timeouts; and the standout — a Voxel viewer that connected and broadcast correctly to an always-empty world, root-caused by proving *object identity* with hash-code logging (same object, event firing, zero sockets) and then finding a four-day-old orphaned native `ToolBox.Host` process squatting on port 8090 from earlier stdio testing. Fix was `kill 80681`, not a code change.

**Capstone review — Lecture 009 (`Documentation/Learning/009-The-Whole-Machine.md`), 2026-07-26.** A standalone end-to-end teaching document plus an adversarial audit of the shipped 1.0, written at Timothy's request to consolidate understanding for interviews. Nine real findings, the sharpest being that `VoxelWorld`'s singleton `Dictionary` is a genuine **data race** (not merely the lost-update risk ADR-009 documents) because ASP.NET Core serves concurrently, JSON-RPC permits pipelining, and `Stateless = true` explicitly advertises horizontal scalability the state layer cannot honour; plus `AllowedHosts` host-filtering existing only in `docker-compose.yml` rather than the app's own defaults (unsafe-by-default), ADR-012's wildcard `HttpListener` bind being a non-elevated-Windows regression CI can't catch, unordered fire-and-forget WebSocket broadcasts risking concurrent `SendAsync` on one socket, and the observation that **two of the three real bugs live in the single untested file**. Two findings were verified against Microsoft docs rather than asserted. The document also contains a rehearsed interview playbook (30-second/3-minute/10-minute pitches, six STAR stories, hostile follow-ups with answers, a 90-second whiteboard diagram, and a claim-measurements-not-adjectives rule).

**Plan 004 (SPICE circuit designer) — concepts phase, July 2026.** Before implementation, produced `Documentation/Learning/008-The-Solver-And-The-Draftsman.md`: a full-depth feasibility and concepts lecture with every numerical claim verified by a from-scratch Modified Nodal Analysis solver (saved and re-runnable in `008-Spikes/`) rather than recalled. Headline finding: the intuitive difficulty ordering is **backwards** — SPICE simulation is a tractable process-integration problem, while automatic schematic *layout* is an open research problem (graph placement, orientation, orthogonal routing and label de-confliction are each NP-hard, with no objective function for "readable" — which is why every professional EDA tool still makes humans place symbols, and why PCB auto-routing *is* solved while schematic auto-layout isn't). Evidence: a hand-placed five-resistor Wheatstone bridge still rendered with three colliding labels. Concepts covered: MNA stamping (graph→matrix mechanically), why voltage sources need the "modified" augmented matrix, singular-matrix diagnosis (floating node vs. source conflict — verified as different failures with different remedies, `gmin` regularization rescuing only the first), Newton-Raphson companion models and convergence (verified: 173 iterations vs 12 with junction limiting; hard `exp()` overflow above 18.33 V), and implicit vs. explicit integration (verified: forward Euler produced −75 V in a 5 V circuit — stability, not accuracy, is why every SPICE is implicit). Design conclusions fed back into plan 004 §2.11: composite tools reframed from a call-economy optimization into **the correctness boundary** (every formula moved server-side is a class of LLM confident-error permanently eliminated); a closed `ModelLibrary` vocabulary so the agent may not invent device physics (vendor models are frequently encrypted and unreadable by ngspice anyway); and schematic rendering scoped to detected topologies with an **explicit refusal path**. Strongest architectural story: this is the first *closed-loop* agentic toolset — the voxel toolset's only correctness oracle was a human looking at the viewer, whereas here a numerical solver gives the agent an objective, machine-readable correctness signal it can consult mid-task.

---

# Expectations for AI Assistance

When assisting me:

* Do not oversimplify technical concepts.
* Assume I am willing to learn difficult material.
* Prefer depth over brevity.
* Connect new ideas to existing concepts.
* Point out knowledge gaps when appropriate.
* Recommend additional topics that naturally follow from what I am studying.
* Explain both the "how" and the "why."

Act as if you are mentoring an engineer who wants to grow from a junior developer into a highly capable systems engineer over the next several years.

---

# Working With Me (for AI assistants)

These are standing instructions. The reasons behind them, and candid notes about how I work, are kept privately.

* **Diagrams, tables and ASCII over prose.** Never ask me to "picture" or "imagine" something; draw it instead.
  Named, personified components work very well for me.
* **A running system before a document.** Route me to the quickstart / walking skeleton first, then the reference.
* **Purpose before mechanism.** Say what something is *for* before how it works. Answer one level above my
  question and offer to go deeper, rather than descending by default.
* **Intent first on any task:** goal, done-when, phases, what we're *not* doing, unknowns.
* **Process findings are first-class.** When reviewing my work, include how the work went (time between runs,
  whether there was a walking skeleton, where I got stuck) alongside the technical findings.
* **Candid is welcome.** State weaknesses plainly, with severity and a fix. Don't flatter.
* **When you learn something durable about me:** public-safe facts (projects, skills demonstrated, goals) go in
  this file; evaluations, weaknesses and personal context go in `private/` (see `private/README.md`).
