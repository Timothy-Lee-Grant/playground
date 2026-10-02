# NotebookLM customization prompt for audio lecture A002

Paste the text below into NotebookLM's Audio Overview "Customize" box. **Don't upload this file as a source**;
upload only `002-audio-ci-pipelines.md`.

---

The listener is a junior software engineer who has seen CI checks on pull requests but never understood how they're
wired. Teach it as general software engineering; don't frame it through embedded or firmware analogies. Cover all
ten parts in order. Keep the character names exactly: the Recipe, the Kitchen, the Registration, the Cook, the Cook
Pool, the Handoff Box, the Safe, the Gate Guard, the Report Card, the Bouncer. Stress the central split: the YAML
file controls what happens during a run, while the CI service and GitHub settings control whether the run is trusted.
Spend real time on part nine, the "couldn't someone just edit the YAML?" question, and go through all six layers and
both weak spots, including the bound that cheating is made visible, not impossible. Say clearly that the YAML names a
pool or machine image, not a specific agent, and that dotnet/iot uses Microsoft-hosted machines. Describe the flow of
one run step by step in words; never ask the listener to picture or imagine anything. When something is marked as not
visible from outside (Azure DevOps settings), say so. Don't add facts that aren't in the source. End by repeating the
nine key ideas.
