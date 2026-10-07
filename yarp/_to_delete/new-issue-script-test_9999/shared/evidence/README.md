# Evidence

Raw output of runs, written by the CLI. Name: `NNN-short-name.txt`. Never edited after capture.
Header of every file: the exact command, date, `dotnet --version` (use `./.dotnet/dotnet --version` in the clone),
and the clone's commit (`git -C yarp rev-parse --short HEAD`). A run that never actually ran (a tooling or
scripting error before the command executed) may be replaced; say so in the session report. A real run is never
edited, even if it failed.
