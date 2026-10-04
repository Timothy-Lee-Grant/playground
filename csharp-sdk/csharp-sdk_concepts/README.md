# MCP C# SDK concepts

Lectures on the MCP C# SDK (`modelcontextprotocol/csharp-sdk`) and the protocol and .NET ideas behind it, written to
stay useful across issues. Style and template: root [`CLAUDE.md`](../../CLAUDE.md) §4.4 and §8.5.

| # | Lecture | Prompted by | Status |
|---|---|---|---|
| 001 | [The MCP C# SDK from the ground up](001-mcp-csharp-sdk-from-the-ground-up.md) | Orientation for all csharp-sdk work: what MCP is for, JSON-RPC, primitives, lifecycle then (initialize) vs now (2026-07-28: server/discover + per-request `_meta`), transports and session modes, MRTR; packages/assemblies/namespaces; cast of characters; startup; one `tools/call` step by step; the session engine (TaskCompletionSource correlation, concurrency timeline, cancellation, forced-yield deadlock); stateless HTTP = server per POST; errors; filters; attributes, DI lifetimes, delegates, async, syntax decoder, AOT, versioning; build, tests (object graph), contributing; Tool_Box 1.x vs 2.x. Checked against `main` @ `c40ee04` | Written 2026-10-04 (~1,150 lines); not yet read |

## Audio lectures (for NotebookLM)

Written to be **listened to**: loaded into Google NotebookLM to generate a podcast-style Audio Overview. Only written
when Timothy explicitly asks for one. Format: root [`../../CLAUDE.md`](../../CLAUDE.md) §10.7. Files live in
[`audio/`](audio/): `NNN-audio-<topic>.md` (the source to upload) + `NNN-audio-<topic>.prompt.md` (paste into
NotebookLM's "Customize" box; don't upload it).

| # | Audio lecture | Prompted by | Status |
|---|---|---|---|
| A001 | [The MCP C# SDK from the ground up](audio/001-audio-mcp-csharp-sdk-from-the-ground-up.md) ([customize prompts](audio/001-audio-mcp-csharp-sdk-from-the-ground-up.prompt.md)) | Same scope as reading lecture 001, same characters (Requester, Stage Manager, Supply Room, Starter, Messenger, Inbox, Postmaster, Claim-Ticket Board, Stop Cords, Directory, Concierge, Blueprint, Catalog Builder, Catalog, Interpreter, Work Order, Return Address, Checkpoints, Intake Desk, Session Ledger) | Written 2026-10-04 (~12,700 words, three episodes via the prompt file); not yet listened to |
