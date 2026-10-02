# Lectures (cross-cutting)

Full, in-depth lectures on concepts that **aren't specific to one upstream project** (for example: how to read a
large codebase, semantic versioning in general, async/await fundamentals).

Concepts tied to one project's domain go in that project's concepts folder instead:

| Project | Concepts folder |
|---|---|
| dotnet/iot | [`../iot/iot_concepts/`](../iot/iot_concepts/) |
| dotnet/yarp | [`../yarp/yarp_concepts/`](../yarp/yarp_concepts/). The older `001-yarp-websocket-activity-timeout.md` still lives here and moves there when the YARP folder is migrated to the new layout. |

| # | Lecture | Prompted by |
|---|---|---|
| 000 | [Architecture and pattern catalog](000-pattern-catalog.md): cross-repo table of patterns seen in real code (a living index, not a lecture) | all issues |
| 001 | [YARP WebSocket activity timeout](001-yarp-websocket-activity-timeout.md) | dotnet/yarp#1764 |
| 002 | [The kinds of tests a professional engineer should know](002-kinds-of-tests.md): scope vs purpose, Microsoft's L0–L4, test doubles, shift right, bad tests; examples from dotnet/iot | Timothy's question, 2026-10-01 |

Files are named `NNN-title.md`. Style and conventions: root [`CLAUDE.md`](../CLAUDE.md) §4.4 and
[`persona.md`](../persona.md).
