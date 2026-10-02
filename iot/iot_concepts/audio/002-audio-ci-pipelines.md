# Audio Lecture A002: The Recipe, the Kitchen and the Cooks. How a CI pipeline really works, told through dotnet/iot

This is an audio lecture, written to be turned into a two-host conversation. It follows the reading lecture "CI
pipelines: what the YAML file controls, what the service controls, and how dotnet/iot does it". It's for any junior
engineer who has seen green and red checks on a pull request without knowing what produced them. Everything about
dotnet/iot's files was read from the repository on October first, 2026. Settings inside Azure DevOps can't be seen
from outside, so the lecture says so whenever it touches them.

## Part one: the problem

Here is the problem continuous integration solves. Someone changes the code and says "it works". What they usually
mean is: it worked on my laptop, with whatever happened to be installed there, if I remembered to run the tests. On
a project with hundreds of contributors, that sentence isn't evidence of anything.

Continuous integration, usually shortened to CI, replaces that sentence with a recorded run. For every change, a
service takes the exact commit, puts it on a clean machine, runs the same commands every time, and posts the result
back on the pull request. Anyone can open the run and see which commit, which machine, which commands, and what they
printed.

The key idea is this: CI produces evidence. It does not decide whether a change is a good idea. Humans, the
reviewers, decide that. Hold on to that sentence, because it's the answer to the security question at the end.

There's a sibling term, CD, for continuous delivery or continuous deployment. CI asks "does it still build and
pass?". CD takes what passed and ships it: signs it, publishes it, deploys it. dotnet/iot does both, and you'll hear
exactly where the line between them sits.

## Part two: the characters

Let's meet the characters. Keep their names, because the rest of the story uses them.

The first character is the Recipe. That's the pipeline file, written in a format called YAML, stored in the
repository. The Recipe lists what to do: the stages, the jobs, the steps, which kind of machine each job needs, and
which events should start a run.

The second character is the Kitchen. That's the CI service itself: Azure Pipelines, or GitHub Actions. The Kitchen
watches for events, reads the Recipe, books machines, streams the logs, stores files, and reports the result back to
GitHub.

The third character is the Registration. That's the Kitchen's own record saying "this repository, this Recipe file,
these permissions". In Azure Pipelines, someone creates the Registration once, by hand. In GitHub Actions, it's
automatic.

The fourth character is the Cook. In Azure the Cook is called an agent; in GitHub it's called a runner. A Cook is a
machine with the agent program installed. It takes one job, runs its steps in order, sends the output back, and then,
if it's a hosted machine, it's wiped.

The fifth character is the Cook Pool: a named group of interchangeable Cooks. And here's the first correction to a
common belief. The Recipe doesn't ask for a particular Cook by name. It asks for a pool, or a kind of machine, and
whichever matching Cook is free takes the job.

The sixth character is the Handoff Box. That's an artifact: a file a job deliberately saves so it survives after the
machine is gone.

The seventh character is the Safe. That's where secrets live: passwords, signing keys, credentials for package feeds.
The Recipe refers to secrets by name. The values are never in the repository.

The eighth character is the Gate Guard. That's an environment, a named deployment target, which the Kitchen can
configure to require a human's approval before anything is deployed to it.

The ninth character is the Report Card: the green check or red cross the Kitchen posts on the pull request.

And the tenth character is the Bouncer. That's GitHub's branch protection: the repository settings that say which
Report Cards are required, how many approving reviews are needed, and who may merge.

Now notice something about these ten. Only one of them, the Recipe, lives in the repository. The Registration, the
Safe, the Gate Guard and the Bouncer all live in settings, in Azure DevOps or on GitHub. A pull request can change the
Recipe. It can't change any of those.

## Part three: is the YAML file in charge of everything?

So, is that one YAML file in control of everything? The honest answer is: it's in control of what happens during a
run. It's not in control of whether the run is trusted.

The Recipe decides which events start a run, what the stages, jobs and steps are, what kind of machine each job
asks for, which files get saved, and which environment name a deployment targets.

The settings decide whether builds from forks run at all and whether a maintainer has to approve them first, which
secrets exist and who can use them, which pools exist, whether an environment needs a human approval, and, through
the Bouncer, whether a check is required before merging.

A common misconception is that the YAML file is the whole CI setup. Actually, it's the recipe. The kitchen, the
safe, the gate guard and the bouncer are configured elsewhere.

And the part of your instinct that's right is a feature. Because the Recipe is a file in the repository, it's
versioned, reviewed and diffable like any other code. This practice is called pipeline as code. Before it, pipelines
were configured by clicking through web forms, and nobody could tell who changed what.

## Part four: how the Kitchen finds the Recipe

How does the service know which file to run? The two Kitchens do it differently.

GitHub Actions finds Recipes by folder. Every YAML file inside the folder dot-github slash workflows is a workflow.
dotnet/iot has three of them there.

Azure Pipelines finds a Recipe through a Registration. Someone goes into an Azure DevOps project, creates a pipeline,
picks the repository, picks the path of the YAML file, and connects it to GitHub. The name azure-pipelines dot yml at
the root of the repository is just the default that the setup wizard suggests; the file could be anywhere.

For dotnet/iot, a maintainer did that one-time setup at some point: created the pipeline, which shows up on pull
requests under the name dotnet dot iot, connected GitHub, and created the secret group and the environment it uses.
After that, editing the Recipe is all an ordinary change needs.

## Part five: following one run through dotnet/iot

Let's follow a real run. When you open a pull request against dotnet/iot's main branch, here is what happens.

The Recipe, azure-pipelines dot yml, starts with two triggers. One, called trigger, fires on pushes to main and to a
release branch: that's CI after a merge. The other, called pr, fires on pull requests into those branches: that's CI
before a merge. Your pull request fires the second one, every time you push to your branch.

The Recipe then has three stages, in order: Build, CodeSign, and Publish.

The Build stage has three jobs: Windows, Linux and macOS. Each job runs on its own Cook, so the three run in parallel
on three separate machines. Each job also has a matrix with two entries, Release and Debug. A matrix means: run this
same job once per entry, with different settings. Three jobs times two entries is six. And if you look at the checks
on any dotnet/iot pull request, you'll see exactly six Azure lines, with names like "dotnet dot iot, Build, Linux
Build, Build underscore Release". The check name is a map: pipeline, stage, job, matrix entry.

Each Build job asks for a Microsoft-hosted machine. The Recipe literally says vm image, ubuntu-latest, or
windows-latest, or macOS-latest. Nobody at dotnet/iot created those machines. Microsoft keeps pools of them ready,
with the dotnet SDK and git and other tools preinstalled. One is handed to the job, and when the job finishes, it's
discarded.

The main step in each job runs a script called build dot sh, with a flag that says "this is CI". Here's a design
point worth remembering: that's the same script a developer runs on their own machine. The Recipe stays thin, and
the real logic lives in a script you can run locally, so a failure in CI can be reproduced with the same command.

And here's a surprise. There's no separate test step in this Recipe. The build script builds a file called build dot
proj, which lists every project in the repository, and for the test projects it asks for a target called VSTest,
which means build them and run them. So in dotnet/iot, the step called "Build" also runs all the device unit tests.
That's a choice this project made. Many projects have a separate build step and test step. Neither is the one true
way.

After the build, each job saves its logs as an artifact, and that step is marked to run always, even when the build
failed. That's deliberate: logs matter most when something went wrong, and without that marking, a failure would skip
the step and the logs would disappear with the machine.

There's also a step in the Linux job that's switched off. It would send the hardware tests to Helix, which is the
dotnet team's own service for running tests on queues of real machines. For dotnet/iot, the queue is made of real
Raspberry Pis, because those tests toggle real pins. The devices are offline, so the step has a condition of false,
with a comment pointing to issue twenty-four oh six. That's why tests for this project use fake drivers: today,
those are the only tests CI runs.

For a pull request, that's the end. The next two stages both carry a condition that says, in words: only run if the
reason for this build is not a pull request. So CodeSign and Publish are skipped.

After a merge to main, they run. CodeSign starts on a fresh Windows machine. It downloads two artifacts the Build
stage saved: the packages, and a list of which files to sign. It uses a secret group, named in the Recipe as Sign
Client V2, whose values live in Azure DevOps and Azure Key Vault. It signs the packages and saves them as a new
artifact, the signed packages. Then the Publish stage, which is a deployment to an environment named Dotnet Iot,
downloads the signed packages and pushes them to a nightly NuGet feed. That's the CD half: every merge produces signed
nightly packages.

## Part six: the Cooks, and correcting "create an agent with the same name"

A natural first model of agents goes like this: I create an agent, which is a machine with an operating system; the
YAML names my agent; the service runs the file there. The first half is right. An agent is a machine with an
operating system and tools, and that's where the steps run.

The correction is about naming and creating. The Recipe names a pool, or a hosted machine image, not an individual
agent. Any free agent that matches picks up the job. And for hosted pools, nobody creates agents at all.

You create agents yourself only when you run self-hosted ones. Then you install the agent program on your own
machine, register it into a pool with a token, and describe what it can do. The Recipe asks for that pool by name,
and can add demands, such as "must have Docker", which the agent's declared capabilities have to satisfy.

When would you do that? When you need special hardware, like a GPU or a device with real sensors, or access to a
private network, or bigger machines. The trade-off is that self-hosted machines keep their state between jobs. That
makes them faster, because caches survive, but it also means one job can leave junk, or secrets, behind for the
next. Hosted machines are always fresh.

And the key idea about agents: an agent doesn't decide anything. It downloads the repository at the exact commit,
runs the steps it's given, streams the output back, and uploads what it's told to upload.

## Part seven: all the ways a run can start

A trigger is simply which event starts the pipeline. dotnet/iot happens to use every kind somewhere.

Push to a branch: the main Azure pipeline runs after merges to main.

Pull request: both systems. The Markdown checks workflow in GitHub Actions runs on every pull request.

Path filter: run only if certain files changed. The Arduino workflow runs automatically on a pull request only if it
touches the SDK version file, the Arduino build script, or the Arduino compiler's folder.

Schedule: a workflow called locker runs every day at nine in the morning, UTC, and locks old closed issues.

Manual: in GitHub Actions, adding workflow dispatch to the triggers puts a Run workflow button on the Actions tab.
The locker workflow even takes two inputs when you run it by hand: how many days since an issue closed, and how many
since it was updated. In Azure Pipelines, every pipeline has a Run pipeline button, and the Recipe can declare
parameters for it.

Comment: the Arduino workflow also starts when someone comments slash run dash arduino dash tests on a pull request.
Its first job is a gate: it checks that the comment really is that command, and asks GitHub whether the commenter
has write or admin permission. If they don't, it stops. Its last job reports back on the pull request, and it's set
to run always, because the one time you most need a failure message is when the build failed.

Why is the Arduino build opt-in at all? The comment at the top of its file says it plainly: the build is narrow and has
been flaky. A flaky check that runs on every pull request teaches people to ignore red, which is worse than not
running it.

## Part eight: the Handoff Box

Here's the rule about artifacts: when a job ends, its machine and everything on it is gone. An artifact is a file the
job explicitly uploaded so it survives.

Artifacts do three jobs. First, they connect stages. Build and CodeSign run on different machines, so the only bridge
is: publish in one, download in the other. Second, they serve humans. When a build fails, you download the logs
artifact and open the detailed build log on your own machine. Third, they are the release. The packages that get
signed and published are artifacts from start to finish.

In dotnet/iot the chain goes: every Build job saves its logs. The Windows Release job also saves the packages and the
list of files to sign. CodeSign downloads those two and saves the signed packages. Publish downloads the signed
packages and pushes them to the feed.

A common misconception is that files built in one job are just there for the next one. Actually, every job starts
on an empty machine. If it wasn't published as an artifact and downloaded again, it doesn't exist.

## Part nine: couldn't someone just edit the Recipe so everything passes?

Now the question that started all this. If the Recipe controls what runs, couldn't a malicious contributor edit it,
delete the test step, and watch everything go green?

The honest answer is yes, a pull request can change what its own run does. That's by design: it's how you'd test a
change to the pipeline itself. The protection is that passing was never the thing that lets code in. There are layers.

Layer one: the diff is reviewed. A change to the Recipe is a code change. A reviewer sees it in the diff exactly like
they'd see a deleted test in a C sharp file. And notice: deleting a test from a C sharp file would also turn things
green. Review is the defense against both.

Layer two: green doesn't mean merged. The Bouncer requires approving reviews and the required checks. The checks are
evidence for the reviewer, not a gate on their own.

Layer three: forks get no secrets. Builds of pull requests from forks run without secrets and with reduced
permissions by default. So a malicious pull request can't reach the signing key.

Layer four: outsiders may need approval to run at all. On GitHub, by default, all first-time contributors need a
maintainer's approval before their workflows run. Azure Pipelines has a similar option. Whether dotnet/iot turns it
on for Azure isn't visible from outside.

Layer five: the dangerous stages don't run for pull requests. CodeSign and Publish only run after a merge, which
means after a review. And they depend on a secret group and an environment that live in Azure DevOps, where an
approval can be required.

Layer six: triggers check permissions, like the Arduino comment trigger checking that the commenter has write
access.

Two real weak spots are worth knowing, because this is how CI actually gets attacked. The first is third-party
steps. When a workflow uses someone else's action by a version tag, it runs their code with your job's permissions.
If that tag gets moved to malicious code, you run the malicious code. dotnet/iot's locker workflow pins its
third-party action to a full commit hash, which can't be moved. That's the professional default. The second weak
spot is a GitHub trigger called pull request target, which runs with the base repository's secrets. If a workflow
using it checks out and runs a fork's code, the fork can steal secrets. dotnet/iot doesn't use it.

And a careful bound on all this: the system doesn't make cheating impossible. A reviewer who approves a Recipe change
without reading it defeats layer one. What the system does is make cheating visible and keep secrets out of reach.

## Part ten: not every YAML file is a pipeline

One last clarification. YAML is just a data format. What a YAML file does depends entirely on which program reads it.
dotnet/iot has a dependabot file, read by GitHub's Dependabot, which opens weekly pull requests to update package
versions. And it has policy files, read by a Microsoft bot, which label new issues as untriaged and close stale pull
requests. Same idea as pipelines, configuration as code, but a different reader, and no build at all.

## Recap

Let's recap the key ideas.

One. CI answers "does this exact commit build and pass?" on a clean machine, the same way every time, as evidence for
reviewers. CD ships what passed.

Two. The Recipe, the YAML file, says what happens during a run. The settings, in the CI service and on GitHub, say who
may run it, with which secrets, on which machines, and whether its result is required to merge.

Three. GitHub Actions discovers every workflow file in the workflows folder automatically. Azure Pipelines needs a
Registration that points at a repository and a file path.

Four. Stage, job, step. A job is one machine. Steps run in order on it. A matrix runs a job once per set of settings;
dotnet/iot runs three operating systems times two configurations, which is the six checks on every pull request.

Five. The Recipe asks for a pool or a machine image, not a named agent. Hosted machines are fresh and thrown away.
Self-hosted machines are yours and keep their state.

Six. Runs can start from a push, a pull request, a path filter, a schedule, a manual button, or a comment.

Seven. Artifacts are the only files that outlive a job. They connect Build to CodeSign to Publish, and they give
humans the logs.

Eight. A pull request can change its own Recipe. The protections are review, required checks, no secrets for forks,
approval for outsiders, and dangerous stages that only run after merge.

Nine. In dotnet/iot, the build script builds and runs the unit tests in one step, and the hardware tests on real
Raspberry Pis are switched off until the devices are back.
