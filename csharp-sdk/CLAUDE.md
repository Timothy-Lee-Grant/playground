# modelcontextprotocol/csharp-sdk: project orientation

> Shared context for every MCP C# SDK issue folder. Started 2026-10-04. No issue folders yet.

## 1. What's in this folder

| Path | What it is |
|---|---|
| [`csharp-sdk_concepts/`](csharp-sdk_concepts/) | Lecture notes (001 reading + A001 audio: full orientation) |

## 2. What the SDK is

The official C# **library** for the Model Context Protocol (JSON-RPC 2.0 between AI apps and tool servers), co-maintained
by Microsoft and the MCP project. Client and server. Plugs into the .NET Generic Host and ASP.NET Core. **Not here:**
`AIFunction`/`IChatClient` (Microsoft.Extensions.AI → dotnet/extensions); DI/options/logging/hosting (dotnet/runtime);
the MCP spec itself (modelcontextprotocol/modelcontextprotocol).

## 3. Upstream facts (checked 2026-10-04, `main` @ `c40ee04`, 2026-09-18)

| | |
|---|---|
| Latest release | v2.2.0 (2026-08-13). v2.0.0 (2026-07-28) implemented spec revision **2026-07-28** (no `initialize`, no `Mcp-Session-Id`, MRTR); still talks to ≤ 2025-11-25 peers |
| Packages | `ModelContextProtocol.Core` → `ModelContextProtocol` (DI/hosting) → `ModelContextProtocol.AspNetCore`; plus `.Extensions.Tasks`, `.Extensions.Apps` |
| TFMs | net10.0, net9.0, net8.0, netstandard2.0 (AspNetCore: no netstandard) |
| SDK | `global.json` 10.0.101, rollForward minor. `LangVersion=preview`, `TreatWarningsAsErrors=true`, nullable on |
| Build/test | `dotnet build`, `dotnet test` (or `make build`/`make test`); Node 22 + `npm ci` for conformance tests; Docker optional |
| Tests | xUnit v3, Moq. Bases: `LoggedTest`, `ClientServerTestBase` (in-memory pipes), `KestrelInMemoryTest`, `TestServerTransport`. `TestConstants.DefaultTimeout` = 60 s |
| CI | GitHub Actions `ci-build-test.yml`: ubuntu/windows/macos × Debug/Release, AOT test, pack |
| API safety | Package validation vs 2.0.0; `[Experimental("MCPEXP…")]`; `[Obsolete]` `MCP9004` (legacy SSE), `MCP9005` (sampling/roots/logging), `MCP9006` (stateful HTTP knobs) |
| Not yet tried | Nothing above has been run on Timothy's machines |

## 4. Contribution rules (CONTRIBUTING.md, .github/copilot-instructions.md)

- `good first issue` / `help wanted` / `ready for work`. No tracked issue → open one first. Non-trivial → post the
  approach on the issue before coding. Assign yourself.
- PR: tests for every fix/feature, all tests passing locally, docs updated, `.editorconfig` style.
- AI disclosure: visible note on anything posted with AI help (repo's own AI instructions).
- Test rules: never `WithStdioServerTransport()` in unit tests; `await using` providers holding a server; no
  `Task.Delay` for sync; no short hard-coded timeouts.
- Apache 2.0. Releases are cut by maintainers with AI agent skills (`.github/skills/`).

## 5. Key source files

| File | Why |
|---|---|
| `src/ModelContextProtocol.Core/McpSessionHandler.cs` | The engine: read loop, dispatch, id correlation, cancellation, tracing |
| `src/ModelContextProtocol.Core/Server/McpServerImpl.cs` | Server: handler registration (`Configure*`), filter pipelines, tool errors, MRTR |
| `src/ModelContextProtocol.Core/Server/AIFunctionMcpServerTool.cs` | C# method → tool (schema, parameter binding, result wrapping) |
| `src/ModelContextProtocol/McpServerBuilderExtensions.cs` | `WithTools*`, `WithStdioServerTransport` |
| `src/ModelContextProtocol.AspNetCore/StreamableHttpHandler.cs` | HTTP POST handling; new server per POST in stateless mode |
| `src/Common/McpProtocolVersions.cs` | Protocol-era gates |

## 6. Scouting items here (status last checked 2026-09-26; re-check before acting)

E #1806 (flaky OAuth metadata timeout, Windows CI) · H #1781 (doc/sample: integration-testing Streamable HTTP) ·
O `Diagnostics.cs` + SEP-414 (distributed tracing, study) · learn-only #1774.
