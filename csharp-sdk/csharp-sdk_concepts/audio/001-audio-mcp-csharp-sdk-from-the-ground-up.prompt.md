# NotebookLM customization prompts for MCP C# SDK audio lecture 001

Upload only `001-audio-mcp-csharp-sdk-from-the-ground-up.md` as the source. **Don't upload this file**; paste one of
the prompts below into the Audio Overview "Customize" box.

The source is ~12,700 words, set up as **three episodes from the same source** (like ASP.NET Core A001). Generate
episode 1, then a new Audio Overview for each of the other two prompts. Choose the "Longer" length option if it's
offered. It reuses two names from the ASP.NET Core episode (the Stage Manager, the Supply Room) with the same
meanings; it doesn't depend on having heard it.

---

## Episode 1: parts 1 to 4 (what MCP is for, JSON-RPC, what servers offer, lifecycle and transports)

The listener is an engineer who has already built an MCP server with this SDK and is about to contribute to the
modelcontextprotocol csharp-sdk repository. Cover only parts one to four of the source, in order, and don't skip any.
In part one, explain the N times M problem and the three roles, and spend real time on the warning that "host" means
the AI application in MCP but the .NET Generic Host in C#. In part two, go through all four JSON-RPC message shapes
and say clearly that the id is the only link between a reply and its request. Spend the most time on part four:
contrast the initialize handshake and session id with the July 2026 revision's per-request metadata, explain why a
handshake creates state and why that blocks horizontal scaling, explain the probe-then-fall-back behavior, why
standard output must stay clean on stdio, what backpressure is and why legacy SSE lacks it, and MRTR as a form sent
back to be filled in. Read every "common misconception" as the wrong version, then the correction. Describe sequences
in words and never ask the listener to picture or imagine anything. Don't add facts, numbers or version details that
aren't in the source. End by repeating final-recap ideas one to three.

## Episode 2: parts 5 to 8 (where the code comes from, the cast, startup, one tool call)

The listener already heard an episode on what MCP is, the JSON-RPC message shapes, and the transports. Open with a
one-minute reminder that replies are matched to requests only by id. Then cover parts five to eight, in order. In part
five, explain package, assembly and namespace slowly, use the example of Add M-C-P Server living in Microsoft's
dependency injection namespace, and say plainly that a using directive grants no access. In part six, introduce every
character by name with its real class name, and keep the names exactly: the Requester, Tool Cards, the Stage Manager,
the Supply Room, the Starter, the Messenger, the Inbox, the Postmaster, the Claim-Ticket Board, the Stop Cords, the
Directory, the Concierge, the Blueprint, the Catalog Builder, the Catalog, the Interpreter, the Work Order, the Return
Address, the Checkpoints, the Intake Desk and the Session Ledger. Stress that the client and server each have a
Postmaster and it's the same class. In part seven, walk the startup chain reaction link by link, and the shutdown.
Spend the most time on part eight: go through all nineteen numbered steps of the tool call in order, saying who hands
what to whom; explain what a task completion source is; then the two-calls-at-once walk with what's on each board
after every event, including the cancellation, and why the forced yield prevents a deadlock. Say clearly that
cancellation is cooperative. Never ask the listener to picture or imagine anything. Don't add facts that aren't in the
source. End by repeating final-recap ideas four to seven.

## Episode 3: parts 9 to 12 (HTTP, errors and filters, the .NET ideas, the repo and contributing)

The listener already heard two episodes on the protocol and on one tool call traveling through the SDK. Open with a
one-minute reminder of the Postmaster, the Claim-Ticket Board and the Stop Cords. Then cover parts nine to twelve, in
order. In part nine, stress that in stateless mode every POST gets a brand-new server, and connect it to the voxel
world data race exactly as the source does. In part ten, explain the two kinds of errors and why ordinary exception
messages are hidden, and that filters are functions that wrap handlers, assembled backwards. Spend the most time on
part eleven: library versus framework with its bound, attributes doing nothing on their own, descriptions being
prompts, tool classes being created for every call so fields aren't state, special parameters, ownership and
disposal, delegates as functions returning functions, and what await and cancellation tokens do not do; read each
piece of compact syntax with its if-and-else expansion. In part twelve, walk the Can Call Registered Tool test as real
versus replaced, then arrange, act, assert, and explain fake versus mock; say that the build commands haven't been
tried on the listener's Mac yet. Read all ten misconceptions as wrong version, then correction. Never ask the listener
to picture or imagine anything. Don't add facts or numbers that aren't in the source. End by repeating all ten ideas
from the final recap.
