# Evidence

Raw output of runs, written by the CLI. Name: `NNN-short-name.txt`. Never edited after capture.
Header of every file: command, date, `dotnet --version`, fork commit (`git -C develop/{{REPO}} rev-parse --short HEAD`).
A run that never actually ran (a tooling or scripting error before the command executed) may be replaced; say so in
the session report. A real run is never edited, even if it failed.
