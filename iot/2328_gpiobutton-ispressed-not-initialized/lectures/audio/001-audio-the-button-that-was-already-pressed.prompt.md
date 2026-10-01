# NotebookLM customization prompt for audio lecture 001 (iot#2328)

Paste the text below into NotebookLM's Audio Overview "Customize" box. **Don't upload this file as a source**;
upload only `001-audio-the-button-that-was-already-pressed.md`.

---

The listener is an embedded C engineer who has just had his first code change to a Microsoft open-source library
built for him, and wants to understand it well enough to explain it to the maintainers. Walk through all seven parts
in order: the cast, the bug step by step, the fix, the tests, the two open questions, the comment, and the
misconceptions. Keep the character names from the source exactly: the Switchboard, the Hardware Whisperer, the
Wiring Translator, the Brain, the Watcher, and the Prop Department. Follow the two step-by-step sequences slowly: the
held button whose release gets swallowed, and the two orderings of read-versus-register with their gaps. Compare to
firmware where the source does (edge interrupts, sampling the pin at init, a hardware abstraction layer with stubs),
and say where the comparison stops. Be clear about what was verified by tests and what was not (no real hardware;
the race can't be tested). For each misconception, state the wrong version, then the correction. Never ask the
listener to picture or imagine anything. Don't add facts that aren't in the source. End by repeating the nine key
ideas.
