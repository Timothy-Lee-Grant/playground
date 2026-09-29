# dotnet/iot#2600 — Conversation Log

> **What this is:** the single, linear record of working on [dotnet/iot#2600](https://github.com/dotnet/iot/issues/2600):
> progress, Timothy's questions and Claude's answers, and decisions, **in the order they happened**. Read it top to
> bottom and you have the full context. This is the file a new Claude session (desktop or CLI) reads first, right
> after this folder's `CLAUDE.md`.
>
> **Rules:**
> - **Append only.** New entries go at the bottom. Never rewrite an earlier entry; if something in it turns out to
>   be wrong, append a 🔁 Correction entry that links back to it.
> - The only parts edited in place are **"Where we are now"** and the **Index** table.
> - Keep entries about *this issue*. When an answer turns into a general lesson, write it as a lecture in
>   [`../../iot_concepts/`](../../iot_concepts/) and link to it from here.
> - Anything not backed by a saved run in `../sample/evidence/` is labeled **unverified**.
>
> **Entry types:** 📍 Progress · ❓ Question (Timothy, in his words) → 💬 Answer · 🧭 Decision · 🔁 Correction
>
> **Started:** 2026-09-29

---

## Where we are now *(updated in place)*

| | |
|---|---|
| **Stage** | Explore: understanding the issue. No code yet, nothing posted upstream. |
| **Last entry** | #1 (2026-09-29) |
| **Open decisions** | (1) Which route (entry #1 §10). (2) What 32-bit ARM target is available for E2/E3 (entry #1 §11). |
| **Next step** | Timothy reads entry #1 and asks questions (entry #2 onward). Then build E1, the walking skeleton (runs on the Mac, no hardware). |

---

## Index

| # | Date | Type | Title |
|---|---|---|---|
| 1 | 2026-09-29 | 📍 Briefing | What #2600 is, the root cause in the thread, the PR that already exists, and the slice that's still open |

---

## #1 · 2026-09-29 · 📍 Briefing: what #2600 is about

### 1. The issue in one paragraph

A service on a **Raspberry Pi 2 (32-bit ARM)** watches one GPIO input with `LibGpiodV2Driver`. Several times a day
the whole process is killed by `SIGABRT` from inside libgpiod (`gpiod_edge_event_copy: Assertion 'event' failed`).
A native `assert` calls `abort()`, so **no managed `try/catch` can stop it**. The reporter blamed a missing null
check. A later commenter found the likely **root cause**: the C# P/Invoke declaration for
`gpiod_edge_event_buffer_get_event` uses `ulong` (always 64-bit) for a C `unsigned long` parameter, which is
**32-bit on ARM32**. On ARM32 that one type mismatch shifts the argument into the wrong CPU register, libgpiod reads
a garbage index, returns `NULL`, and the next call asserts. An open PR (#2601) adds the null check, but **it doesn't
fix the type mismatch**. That's the part of this issue that's still open.

### 2. The facts (checked 2026-09-29)

| | |
|---|---|
| **Title** | "LibGpiodV2: unchecked null from gpiod_edge_event_buffer_get_event aborts the process (gpiod_edge_event_copy: Assertion 'event' failed)" |
| **Opened** | 2026-08-19 by **kai-melchior** |
| **State / labels** | Open · `untriaged` |
| **Assignees** | **krwq** and **Copilot** (the GitHub Copilot coding agent) |
| **Reporter's setup** | Pi 2 Model B (armv7l), Raspbian 13 (trixie), libgpiod 2.2.1 (`libgpiod.so.3`), .NET 10 self-contained `linux-arm`, System.Device.Gpio 4.2.0 |
| **Linked PR** | [#2601](https://github.com/dotnet/iot/pull/2601) "Prevent LibGpiodV2 native abort on null edge event", opened 2026-08-20 by the Copilot agent on behalf of krwq. Approved by **raffaeler** (2026-08-20 and 2026-08-27). krwq enabled auto-merge. **Not merged** as of 2026-09-29; last activity 2026-08-27. |
| **Related** | [#2604](https://github.com/dotnet/iot/issues/2604) / PR [#2605](https://github.com/dotnet/iot/pull/2605): the **same family of bug** in the *V1* binding (32-bit platforms with 64-bit `time_t`), open, active 2026-09-29. It's on the learn-only list in the scouting file. |

**The thread, in order:**

| Date | Who | Gist |
|---|---|---|
| 2026-08-19 | kai-melchior | Report. Crashes happen even when **no pin changes were logged**. Proposes a null check and/or bounding the loop by `GetNumEvents()`. |
| 2026-08-27 | pgrawehr | Does the Pi go to sleep first? |
| 2026-09-07 | kai-melchior | No sleep state. |
| 2026-09-07 | raffaeler | Doesn't understand how the handle could become invalid. |
| 2026-09-17 | pgrawehr | "[Triage] I will try to reproduce this when I find time." Still confused why the handle becomes invalid. |
| 2026-09-23 | **wolfgang-knobloch** | Same abort on another armv7l device. **Root cause: ABI mismatch, not an invalid handle** (details in §5). Suggests `nuint` for every C `unsigned long` / `size_t` in the V2 binding, plus keeping the null check as a safety net. |

Nobody has replied to wolfgang's analysis yet, and nobody has offered to implement it.

### 3. The cast of characters

| Character | Real type | Where it lives (dotnet/iot) | Its job |
|---|---|---|---|
| **The Watchman** | `LibGpiodV2EventObserver` | `src/System.Device.Gpio/System/Device/Gpio/Drivers/LibGpiodV2EventObserver.cs` | Background task. Waits for the kernel to say "something happened on your lines", then reads the events and calls your handlers. |
| **The Mailbox** | `EdgeEventBuffer` (C#) around `struct gpiod_edge_event_buffer` (C) | `.../Interop/Unix/libgpiod/V2/Proxies/EdgeEventBuffer.cs` | A fixed-size box (capacity 10) that libgpiod fills with edge events. You take them out by index. |
| **The Letter** | `EdgeEvent` / `gpiod_edge_event` | `.../V2/Proxies/EdgeEvent.cs` | One edge: rising/falling, line offset, timestamp, sequence numbers. |
| **The Translator** | the `[DllImport]` declarations in `Interop.LibgpiodV2` | `.../V2/Binding/Interop.libgpiod.cs` | Describes each C function to the .NET runtime: its name and **the exact type of every argument**. The runtime trusts it completely. |
| **The Courier** | the .NET P/Invoke marshaller + the CPU's calling convention | the runtime, and the ARM ABI (AAPCS) | Physically places arguments into CPU registers the way the Translator described. |
| **The Librarian** | libgpiod 2.x | `libgpiod.so.3`, header `include/gpiod.h` | The C library. Reads its arguments from the registers where **its own header** says they are. |
| **The Tripwire** | `assert(event)` at the top of `gpiod_edge_event_copy` | libgpiod `lib/edge-event.c` | If handed `NULL`, it calls `abort()`: the process dies on the spot. |

### 4. How an edge event travels (and where it breaks)

```
 Watchman (background task)                         libgpiod (C)
 ─────────────────────────                          ────────────
 WaitEdgeEvents(timeout)  ───────────────────────►  poll() on the line-request fd
        ◄──── 1 = "events ready"
 ReadEdgeEvents(buffer)   ───────────────────────►  reads events from the kernel into the Mailbox
        ◄──── n = number of events read (e.g. 1)
 for i in 0..n-1:
   GetEvent(i)            ─── Translator + Courier ─►  gpiod_edge_event_buffer_get_event(buffer, index)
                                                        index < num_events ? &events[index] : NULL
        ◄──── pointer (or NULL!)
   gpiod_edge_event_copy(ptr) ────────────────────►  assert(event)   ◄── NULL here = abort(), process dies
   HandleEdgeEvent → your handler(sender, args)
```

On x64 and arm64 this works. On ARM32 the `GetEvent(i)` hop hands libgpiod the wrong `index`, so it returns
`NULL` and the Tripwire fires. That also explains the reporter's odd detail: the **first real edge** crashes the
process *before* any handler runs, so the log never shows a pin change.

### 5. The root cause: one type, two sizes

C's `unsigned long` is not a fixed size. It follows the platform's data model:

| Platform | Data model | C `unsigned long` | C# `ulong` | C# `nuint` |
|---|---|---|---|---|
| Linux x64 | LP64 | 64-bit | 64-bit ✅ | 64-bit ✅ |
| Linux arm64 (Pi 3/4/5 on a 64-bit OS) | LP64 | 64-bit | 64-bit ✅ | 64-bit ✅ |
| **Linux ARM32 (Pi 2, or any Pi on a 32-bit OS)** | **ILP32** | **32-bit** | 64-bit ❌ | 32-bit ✅ |
| Windows x64 (not relevant here) | LLP64 | 32-bit | 64-bit ❌ | 64-bit ❌ |

C# `ulong` is **always** 64-bit. `nuint` is pointer-sized, which matches `unsigned long` on every Unix and matches
`size_t` everywhere. (.NET 6+ also has `CULong`, which matches `unsigned long` on every platform including Windows.)

Now the register mechanics, per wolfgang's analysis. The ARM32 procedure-call standard (AAPCS) passes the first
arguments in `r0`–`r3`, and a **64-bit argument must start in an even-numbered register**:

```
 What .NET does (Translator says: (SafeHandle buffer, ulong index))
   r0 = buffer pointer
   r1 = (skipped: 64-bit arg needs an even register pair)   ← still holds leftover junk
   r2 = index (low 32 bits)
   r3 = index (high 32 bits)

 What libgpiod reads (its header says: (struct gpiod_edge_event_buffer *buffer, unsigned long index))
   r0 = buffer   ✅
   r1 = index    ❌  junk, almost always >= num_events  →  returns NULL  →  assert  →  abort()
```

Nothing crashes on arm64 or x64, which is why nobody saw it in CI or on a Pi 4/5.

### 6. What the source shows: every size mismatch in the V2 binding

Compared `Interop.libgpiod.cs` (dotnet/iot `main` @ `1eb0b2f`, 2026-09-24) with `include/gpiod.h` at libgpiod
**v2.2.4** (the reporter runs 2.2.1; this part of the API didn't change within 2.x as far as the header shows).
**From reading source; unverified by any run.**

| C function (gpiod.h) | C type | C# declares | On ARM32 | On 64-bit | Used on the hot path? |
|---|---|---|---|---|---|
| `gpiod_edge_event_buffer_get_event(buf, index)` | `unsigned long` param | `ulong` | **❌ wrong register → NULL → abort** | ✅ | **Yes: every edge event** |
| `gpiod_edge_event_get_global_seqno` / `_line_seqno` | `unsigned long` return | `ulong` | ❌ high half = junk from `r1` | ✅ | No (only `MakeSnapshot`, debug) |
| `gpiod_line_settings_set_debounce_period_us(s, period)` | `unsigned long` param | `ulong` | ❌ same register skip as `get_event` | ✅ | No (the driver never sets debounce today) |
| `gpiod_line_settings_get_debounce_period_us`, `gpiod_line_info_get_debounce_period_us` | `unsigned long` return | `ulong` | ❌ junk high half | ✅ | No |
| `gpiod_edge_event_buffer_new(capacity)`, `gpiod_line_request_read_edge_events(.., max_events)`, `gpiod_request_config_set_event_buffer_size`, `gpiod_line_config_add_line_settings(.., num_offsets, ..)`, `..._set_output_values`, `..._get_configured_offsets`, `..._get/set_values_subset`, `..._get_requested_offsets` | `size_t` params | `int` | ✅ (both 32-bit) | ⚠️ upper 32 bits of the register are formally unspecified; works in practice | Some, yes |
| `..._get_capacity`, `..._get_num_events`, `..._get_num_lines`, `..._get_num_configured_offsets`, `..._get_num_requested_lines`, `..._get_event_buffer_size` | `size_t` return | `int` | ✅ | ⚠️ truncated to 32 bits (harmless for small counts) | Some |

So the crash comes from **one line**, but the same mistake appears in about a dozen declarations. wolfgang's
comment covers the `get_event` index and the seqno returns. The **debounce setter** is an extra case nobody has
mentioned yet: it would pass a garbage debounce period on ARM32 if anything ever calls it.

### 7. What PR #2601 does, and what it leaves

PR #2601 changes two files:

1. `EdgeEventBuffer.GetEvent`: if the returned handle `IsInvalid` (NULL), throw `GpiodException` instead of calling
   `gpiod_edge_event_copy`.
2. `LibGpiodV2EventObserver`: loop to `Math.Min(ReadEdgeEvents(...), GetNumEvents())`, and dispose each `EdgeEvent`
   (a review comment from the Copilot reviewer pointed out the copies were never freed).

That's a good safety net: no more `abort()`. But follow it through on ARM32, **assuming §5 is right**:

```
 edge arrives → n = 1 → min(1, GetNumEvents()=1) = 1 → GetEvent(0)
   → libgpiod still reads junk from r1 → NULL
   → #2601 throws GpiodException
   → the Watchman's catch prints one Console.WriteLine and EXITS the loop
   → finally: subscriptions for those lines are removed
 result: the process survives, but that pin never raises another event. Silently.
```

So on ARM32, #2601 alone likely turns **"crashes several times a day"** into **"goes deaf after the first edge"**.
That's arguably worse to debug. **Unverified** (reasoned from source, not run).

Two smaller things noticed in #2601's diff (reading only): after the "Potential fix" commit, the two lines inside the
`for` loop lost their indentation, which StyleCop may flag; and its description says no unit test was added because
the proxies are only reached by hardware tests.

### 8. What a real fix looks like

| Layer | Change | Breaking? |
|---|---|---|
| **Root cause** | In `Interop.libgpiod.cs` (V2), declare every C `unsigned long` and `size_t` as `nuint` (or `CULong` for `unsigned long`). Adjust the internal proxies that call them (`EdgeEventBuffer.GetEvent(ulong)` → `nuint`, `SequenceNumber`, debounce conversions, the `int` capacities). | **No.** All of it is `internal`; the public `GpioController`/`GpioPin` API doesn't change. That's a big difference from #2403. |
| **Safety net** | The null check and loop bound from #2601. | No. Already in flight. |
| **Guard against regression** | A test that fails if a V2 declaration's parameter size doesn't match C. A reflection test can check `[DllImport]` signatures against a small table of C types, and it runs on any machine. | No |

`nuint` vs `CULong`: libgpiod is Linux-only, where `nuint` and `unsigned long` always match. `CULong` states the
intent more precisely. It's a style choice to ask the maintainers about.

### 9. Leads, **unverified**

| # | Lead | Why it matters |
|---|---|---|
| L1 | #2601 alone may make ARM32 edge events stop silently (§7). | Worth stating in the upstream comment, because it's the argument for doing the root-cause fix at all. |
| L2 | The debounce setter has the same register-skip bug (§6). | Latent today; would bite whoever adds debounce support to the V2 driver. |
| L3 | `bool` returns (`gpiod_line_info_is_used`, `..._is_active_low`, ...) are marshalled as a 4-byte `BOOL` while C returns a 1-byte `bool`. | A classic P/Invoke hazard: garbage in the upper bytes could read as `true`. Probably fine in practice because compilers zero-extend, but it's the same class of bug. Out of scope; note only. |
| L4 | The V1 binding (#2604/#2605) had a 32-bit bug from the same family (a `time_t` layout mismatch). | Pattern: **the libgpiod bindings have never been exercised on 32-bit ARM in CI.** A second 32-bit bug makes a stronger case for a signature test. |

### 10. Is this still a good pick? (correction to scouting item C)

The scouting entry and Claude's recommendation in chat on 2026-09-29 both said #2600 was **unclaimed with no PR**.
That was wrong: the issue is assigned to krwq and Copilot, and PR #2601 was already open. What's actually open is
the **root-cause fix (§8)**, which #2601 doesn't touch and nobody has claimed.

Fit check:

| | |
|---|---|
| Breaking change? | **No** (internal interop only). Unlike #2403, this can merge in a minor release. |
| Skill fit | Strong. P/Invoke, native types, calling conventions and register-level debugging are your firmware background applied to .NET. |
| Growth | High. ABI and data models (ILP32/LP64), P/Invoke marshalling, `SafeHandle`, and how a managed runtime crosses into native code. These are interview-grade concepts. |
| Hardware | Reproducing the real crash needs **32-bit ARM**. The ABI itself can be shown without GPIO at all (§11). |
| Etiquette | krwq owns the issue and #2601. The right first move is a **comment** that builds on wolfgang's analysis and offers the follow-up. Don't open a PR that competes with #2601. |
| One-PR rule | Your docs PR (AspNetCore.Docs#37747) is still open. A comment and the evidence work don't count against the rule; the PR itself might have to wait. |
| Employer check | Still pending from the scouting file (§1 there). This is GPIO code. |

**Options:**

| Route | What it is | Could merge soon? | What you learn |
|---|---|---|---|
| **A. Evidence, then comment** | Build E1 (and E2 if possible), then comment on #2600: confirm wolfgang's diagnosis with evidence, point out L1 and L2, offer a follow-up PR with the `nuint` changes plus a signature test. | Depends on the reply | ABI, P/Invoke, upstream collaboration |
| **B. Comment now, evidence later** | Same comment, posted now with reasoning only. | Same | Less. And the thread already has the reasoning (wolfgang's comment). What it lacks is proof. |
| **C. Review #2601** | Leave a review on #2601 pointing out L1 and the indentation. | n/a | Code-review skills |
| **D. Study only** | Treat it as a 📖 learn item: the lectures and E1, no upstream action. | n/a | Concepts only |

**Claude's recommendation:** **A**. The thread's missing piece is evidence, and a small experiment showing the
wrong register on ARM32 is exactly the kind of thing that got your #1764 work taken seriously. C can be folded into
the same comment. Your call. 🧭 **Decision pending.**

### 11. What the sample could do (walking skeleton first)

| Exp | What | Needs | Proves |
|---|---|---|---|
| **E1** | **Walking skeleton, no GPIO:** a 10-line C library `abi_probe.so` with `void* probe(void* p, unsigned long index)` that prints what it received and returns `NULL` if `index >= 10`, plus a C# console app that P/Invokes it twice: once declared with `ulong`, once with `nuint`. Run it on the Mac (arm64). | The Mac (clang + .NET 10) | Control: both declarations work on a 64-bit platform. Gets the whole build → P/Invoke → print loop running. |
| **E2** | The **same E1 binaries** built for `linux-arm` and run on a 32-bit ARM target. | 32-bit ARM: a Pi running a **32-bit** (armhf) OS, **or** Docker `--platform linux/arm/v7` with emulation on the Mac (disk-heavy; ~20 GB free) | The core claim: with `ulong`, `probe` receives junk; with `nuint`, it receives the right index. |
| **E3** *(optional)* | The real thing: on 32-bit ARM with libgpiod 2.x, subscribe to a pin, jumper an output to it, toggle. Run against 4.2.0, a build with #2601, and a build with `nuint`. | Pi on a 32-bit OS + a local dotnet/iot build | 4.2.0 aborts; #2601 goes deaf after the first edge (L1); `nuint` works. |
| **E4** | A reflection test (like the "descriptions-as-prompts" test in Tool_Box) that fails on any V2 `[DllImport]` whose parameter types don't match a table of C types. | Any machine | Prevents a regression; the test that goes in the PR. |

E1 → E2 is the minimum that turns wolfgang's reasoning into evidence. Raw output goes to `../sample/evidence/NNN-*.txt`
with the usual header.

### 12. Next steps / questions for Timothy

1. Read this entry and ask whatever's unclear, in your words. Each question becomes the next entry.
2. **Hardware:** which Pi(s) do you have, and could one boot a **32-bit** Raspberry Pi OS on a spare SD card? That's
   the cleanest route to E2 and E3. If not, is Docker's arm/v7 emulation acceptable given the Mac's disk space?
3. Pick the first concept lecture for `iot_concepts/`. Candidates this issue touches:
   - **Calling conventions and data models:** what an ABI is, registers vs stack, AAPCS vs AAPCS64 vs SysV x64,
     ILP32/LP64/LLP64, why `long` changes size, alignment rules (the even-register rule in §5).
   - **P/Invoke from the inside:** `DllImport` vs `LibraryImport`, blittable types, the marshaller, `SafeHandle`
     ownership (why `EdgeEventNotFreeable` exists), `nint`/`nuint`/`CLong`/`CULong`, `SetLastError`.
   - **When native code kills a managed process:** `assert`/`abort()`/`SIGABRT`, why `try/catch` can't help, and
     how to design interop so failures stay managed.
4. Decide the route (§10).

### Sources (checked 2026-09-29)

- Issue: https://github.com/dotnet/iot/issues/2600 (body + 5 comments, via the GitHub API)
- PR: https://github.com/dotnet/iot/pull/2601 (description, 2-file diff, 1 review comment, 3 reviews)
- Related: https://github.com/dotnet/iot/issues/2604, https://github.com/dotnet/iot/pull/2605
- dotnet/iot source at `main` @ `1eb0b2f` (2026-09-24): `Interop/Unix/libgpiod/V2/Binding/Interop.libgpiod.cs`,
  `V2/Proxies/EdgeEventBuffer.cs`, `EdgeEvent.cs`, `LineRequest.cs`, `LineSettings.cs`, `LineInfo.cs`,
  `LibGpiodProxyFactory.cs`, `LibGpiodProxyBase.cs`, `V2/Binding/Handles/EdgeEventNotFreeable.cs`,
  `System/Device/Gpio/Drivers/LibGpiodV2EventObserver.cs`, `LibGpiodV2Driver.cs`
- libgpiod header: https://github.com/brgl/libgpiod/blob/v2.2.4/include/gpiod.h (the GitHub mirror has tags
  v2.2.4, v2.3, v2.3.1; v2.2.1 wasn't available there)
- Background (not re-read for this entry): the ARM AAPCS rule that 64-bit arguments occupy an even/odd register pair;
  .NET docs on `nint`/`nuint` and `CLong`/`CULong`
