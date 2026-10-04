# OpenTelemetry .NET concepts

Lectures on OpenTelemetry (`open-telemetry/opentelemetry-dotnet` and `-contrib`) and the concepts behind it, written
to stay useful across issues. Style and template: root [`CLAUDE.md`](../../CLAUDE.md) §4.4 and §8.5.

| # | Lecture | Prompted by | Status |
|---|---|---|---|
| 001 | [The Case File: OpenTelemetry from the ground up](001-the-case-file-opentelemetry-from-the-ground-up.md): what OTel is and isn't; the repo family; spans/metrics/logs/baggage/resource; the .NET twist (API in the runtime, API vs SDK packages); cast of characters; startup; one span's life (state table); propagation and `traceparent`; `AsyncLocal` and the Baggage leak (#7449); metrics and cardinality; logs; sampling; the Collector; semantic conventions; disposal; repo tour with a depth budget; reading a test; contribution rules; experiments X1–X7 | Orientation request (2026-10-04); 📡 track items L, N, O, Q, R. Checked against core `f9dd754`, contrib `d3588e5` | Written 2026-10-04 (~1,500 lines, with a 45-minute "core path"); not yet read |

## Audio lectures (for NotebookLM)

Written to be **listened to**: loaded into Google NotebookLM to generate a podcast-style Audio Overview. Only written
when Timothy explicitly asks for one. Format: root [`../../CLAUDE.md`](../../CLAUDE.md) §10.7. Files live in
[`audio/`](audio/): `NNN-audio-<topic>.md` (the source to upload) + `NNN-audio-<topic>.prompt.md` (paste into
NotebookLM's "Customize" box; don't upload it).

| # | Audio lecture | Prompted by | Status |
|---|---|---|---|
| A001 | [OpenTelemetry from the ground up: the standard, the .NET SDK, and the ideas underneath](audio/001-audio-opentelemetry-from-the-ground-up.md) ([customize prompts](audio/001-audio-opentelemetry-from-the-ground-up.prompt.md)) | Same request as 001; same character names (Reporter, Timecard, Clipboard, Bureau Chief, Selector, Mailroom, Out-Tray, Mail Carrier, Packer, Stamper, Annotator, Ledger, Auditor, Sorting Office, Archive) | Written 2026-10-04 (~13,000 words, three episodes via the prompt file); not yet listened to |
