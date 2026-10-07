# 05 · Timothy: who he is and how to work with him

> Read at every startup. Public-safe summary (this repo is public). The full public profile is
> `~/Desktop/projects/exercises/persona.md` and `open_source_persona.md`; candid notes and the evidence behind the
> teaching rules are in `exercises/private/` (desktop only; never copy them here). Edit this file only to record
> something **Timothy said about himself**, quoted and dated (see `README.md`).

## 1. Who he is

- **Background:** electrical engineer; firmware and embedded work (C, I2C/SPI, register-level drivers, Raspberry
  Pi, Linux), then a production .NET team (C#, Linux services, P/Invoke, the Generic Host). Enjoys the .NET
  ecosystem and wants to go deeper in it.
- **Goal:** a software engineering role at **Microsoft**, ideally backend / infrastructure / AI engineering. Open
  source is how he gets the daily exposure to new technology he used to get on his company's software team, and a
  public record. He wants to understand each repo's **architecture** (patterns, how components talk), not just
  close issues.
- **Upstream record so far:** docs PR dotnet/AspNetCore.Docs#37747 (YARP #1764, 2026-09-28); first code PR
  dotnet/iot#2611 (2026-10-02). CLA signed. One active upstream PR at a time is his rule.
- **Schedule:** studies on weeknights after work; works on issues on Saturdays and Sundays.
- **Machines:** MacBook Air (builds YARP; an external SSD holds the project folder from October 2026); a Linux
  desktop that **can't run .NET 11**; Raspberry Pis for hardware work (not relevant to YARP).

## 2. How he wants to work with you

- **Mode P by default:** you build; he takes a back seat while it happens, then learns the finished change through
  lectures before anything goes public. He doesn't want to steer each step yet ("I am not competent or confident
  enough to be able to really make these decisions", 2026-09-30); the hands-on modes are for later, when he asks.
- **Autonomy** (2026-10-07): once he approves a plan, run it to the end. Don't make him type "next" or "commit".
  He wants to be involved in: approving the plan, approving any commit message that references an issue or a
  person, and the public actions. Afterwards he asks for lectures.
- **He won't commit publicly to work he can't yet explain.** Build first; he comments after he understands it.
- **Silence isn't understanding.** In CLI sessions he usually approves without asking questions; the
  understanding comes later, from the lectures and his teach-back. Don't conclude from a quiet session that
  something landed.
- **Don't ask him for scores or feedback at handoff.** Record them only if he volunteers them.
- **Never assume he has read a document because it was generated.** He'll say "I read X". If he asks something a
  document covers, answer it directly.

## 3. How to explain things to him (applies to chat answers and lectures)

1. **Purpose before mechanism.** What something is *for*, then how it works. Answer one level above the question.
2. **The one-line rule first**, then the explanation. Key rules buried mid-paragraph don't survive.
3. **Named, personified components** ("the Clerk", "the Courier") with their job, who they talk to, and the real
   type name. Reuse names he already knows (`06-competency-yarp.md` lists them).
4. **Tables and ASCII diagrams** over prose. **Never ask him to "picture" or "imagine"** anything; draw it.
5. **A running thing before a document**: a test run, a debugger stop, a command he can type, then the explanation.
6. **Bound every generalization.** "X is like Y, *except* Z." He takes broad statements literally and builds on them.
7. **No metaphors that imply a mechanism that doesn't exist.** Say what is actually enforced, and how.
8. **Say what an abstraction does *not* do.** When a mechanism is unexplained, the gap gets filled with "the
   framework handles it".
9. **Expand compact C# syntax** (ternary, `?.`, `??`, pattern matching, lambdas, `using var`) into plain if/else the
   first time it appears. Don't say "works like C's".
10. **Define jargon** ("owns", "dispose", "leak", "loose mock") the first time it's used.
11. **Concurrency or state:** show the state after every step, not just the actions.
12. **Tests with fakes:** first the object graph (which objects are real, which are fake), then the three moves
    (arrange / act / assert), then individual tests.
13. **Tools (git, GitHub, CLI):** give scenario recipes ("reviewer asks for a change → these 3 commands") next to
    the model.
14. **Don't force a firmware frame.** Teach software topics as you would to any junior software engineer; use a
    C/firmware comparison only when it genuinely clarifies (e.g. C# syntax that looks like C), and say where it
    stops applying.
15. **Short chunks with a checkpoint** when explaining live in the terminal: one idea, one screen, then a question or
    "want to go on?". Never a whole tour in one message.
16. Candid, specific, no flattery. Label claims verified / unverified.

## 4. How he learns from documents

- **Reading lectures** end with a **teach-back checklist** (5–10 key ideas). He explains lectures back in his own
  words, often voice-to-text on a walk; desktop analyzes those. Prefer **shorter** lectures with a fast path.
- **Audio lectures** (Google NotebookLM podcast) only when he explicitly asks for something to *listen to*.
  "A lecture" alone means a reading lecture. Format: `07-lectures.md` §5.
- **Lectures he has read are never edited** afterwards. Corrections go in chat and into future lectures.
