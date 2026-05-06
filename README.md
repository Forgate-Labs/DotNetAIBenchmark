# DotNetAIBenchmark

Proof of concept: a .NET 10 CLI that evaluates coding models by running `pi` headlessly inside Docker.

Current benchmark version: `0.1.0`.

The goal is to keep benchmark executions isolated and reproducible while still using pi's provider/model/auth support.

See [METHODOLOGY.md](METHODOLOGY.md) for how the fixtures are designed, validated, and scored.

## Current scope

This repository is a PoC, not a full benchmark product yet. It currently supports:

- Docker-based execution for every agent run.
- `pi --mode json` as the agent backend.
- A local pi agent directory for credentials/settings used by Docker.
- Authentication dry runs before benchmark tasks start.
- A simple JSONL dataset format.
- 20 initial .NET 10 benchmark scenarios across API bug fixing, EF Core, auth/authz, Reqnroll BDD, performance, architectural refactoring, and testing.
- Public and hidden validation suites, with hidden tests copied only after the agent run.
- Clean source diffs that ignore generated `bin/` and `obj/` artifacts.
- JSON and Markdown reports stored under versioned model/date history folders.
- Provider-error retries, with up to 3 attempts per scenario by default.

## Prerequisites

Install on the host:

- .NET SDK 10
- Docker
- pi CLI

Check them:

```bash
dotnet --info
docker --version
pi --version
```

Docker must be running. This command should work, even if it prints no containers:

```bash
docker ps
```

## Repository layout

```text
.
├── .pi-agent-benchmark/              # local pi config for Docker runs; secrets are ignored
│   └── .gitkeep
├── datasets/
│   └── poc-dotnet-tasks.jsonl         # PoC task dataset with 20 scenarios
├── docker/
│   └── pi-runner.Dockerfile           # image with .NET 10, Node.js, and pi
├── fixtures/
│   └── <scenario-id>/                 # disposable .NET benchmark fixtures
├── src/
│   └── DotNetAIBenchmark.Cli/         # benchmark runner
├── DotNetAIBenchmark.slnx
├── METHODOLOGY.md
└── README.md
```

## Build the project

```bash
dotnet build DotNetAIBenchmark.slnx
```

## Build the Docker image

The CLI can build the image automatically with `--build-image`.

Manual build:

```bash
docker build -f docker/pi-runner.Dockerfile -t dotnet-ai-benchmark-pi:local .
```

The image contains:

- .NET SDK 10
- Node.js 24
- `@mariozechner/pi-coding-agent`
- basic Linux tooling such as `git`, `bash`, and `curl`

## pi authentication inside Docker

The container must access pi credentials before benchmark runs start. This PoC supports three authentication modes.

### Mode 1: `mount` default

Mounts a pi agent directory into the container at `/pi-agent` and sets:

```bash
PI_CODING_AGENT_DIR=/pi-agent
```

Resolution order for the directory to mount:

1. `--pi-agent-dir <path>`
2. `PI_CODING_AGENT_DIR`
3. `.pi-agent-benchmark`
4. `~/.pi/agent`

This mode is recommended for subscription/OAuth providers because pi can read and refresh tokens from `auth.json`.

The mount is read-write in this PoC so OAuth refresh can update files.

### Mode 2: `env`

Forwards known provider environment variables into Docker.

Examples:

```bash
export OPENAI_API_KEY="sk-..."
export ANTHROPIC_API_KEY="sk-ant-..."
```

Run:

```bash
dotnet run --project src/DotNetAIBenchmark.Cli -- \
  --models "openai/gpt-5:high" \
  --auth-mode env \
  --build-image
```

This mode is best for API keys and fully headless setups.

### Mode 3: `none`

Does not mount files or forward credentials.

Use only for debugging command construction or Docker behavior.

## Local pi agent directory

The repository contains `.pi-agent-benchmark/` for credentials/settings used by Docker benchmark runs.

Tracked:

- `.pi-agent-benchmark/.gitkeep`

Ignored by git:

- `.pi-agent-benchmark/auth.json`
- `.pi-agent-benchmark/settings.json`
- `.pi-agent-benchmark/models.json`
- any other file created inside `.pi-agent-benchmark/`

Copy your host pi config into it:

```bash
cp ~/.pi/agent/auth.json .pi-agent-benchmark/auth.json
cp ~/.pi/agent/settings.json .pi-agent-benchmark/settings.json
```

`models.json` is optional. It is only needed for custom provider/model definitions. Many pi installations will not have it.

After copying, verify the files exist locally:

```bash
ls -la .pi-agent-benchmark
```

Expected example:

```text
.gitkeep
auth.json
settings.json
```

## Using an OpenAI subscription

For ChatGPT Plus/Pro/Codex subscription auth, pi normally needs OAuth credentials in `auth.json`.

That means one of these must be true before the benchmark runs:

- `.pi-agent-benchmark/auth.json` already contains valid pi OAuth tokens, or
- `~/.pi/agent/auth.json` contains valid pi OAuth tokens and is mounted, or
- you use a Docker volume that was authenticated previously, or
- you use an OpenAI API key instead of subscription auth.

Fully automatic first-time OAuth login from a non-interactive `docker run` is not expected to work reliably because browser/device/MFA flows require user interaction.

Recommended subscription workflow:

```bash
# 1. Ensure the host pi installation is authenticated somehow.
#    This is usually done once with pi's interactive login.

# 2. Copy credentials into the local benchmark agent directory.
cp ~/.pi/agent/auth.json .pi-agent-benchmark/auth.json
cp ~/.pi/agent/settings.json .pi-agent-benchmark/settings.json

# 3. Validate from inside Docker.
dotnet run --project src/DotNetAIBenchmark.Cli -- auth-check \
  --models "openai/<model-id>:high" \
  --auth-mode mount \
  --build-image
```

Use actual model IDs reported by your pi installation.

List available models on the host:

```bash
pi --list-models openai
```

## Using OpenRouter models

After adding OpenRouter credentials to pi, copy the updated host auth file into the local Docker agent directory:

```bash
cp ~/.pi/agent/auth.json .pi-agent-benchmark/auth.json
cp ~/.pi/agent/settings.json .pi-agent-benchmark/settings.json
```

The local `.pi-agent-benchmark/models.json` can define additional OpenRouter models that are not listed by the current pi build. For example, this repository has been configured locally for:

```text
openrouter/tencent/hy3-preview:free:high
```

Validate the model from inside Docker:

```bash
dotnet run --project src/DotNetAIBenchmark.Cli -- auth-check \
  --models "openrouter/tencent/hy3-preview:free:high"
```

Run the benchmark with it:

```bash
dotnet run --project src/DotNetAIBenchmark.Cli -- \
  --models "openrouter/tencent/hy3-preview:free:high"
```

## Using an OpenAI API key

This is the simplest fully headless OpenAI setup.

```bash
export OPENAI_API_KEY="sk-..."

dotnet run --project src/DotNetAIBenchmark.Cli -- auth-check \
  --models "openai/gpt-5:high" \
  --auth-mode env \
  --build-image
```

Then run the benchmark:

```bash
dotnet run --project src/DotNetAIBenchmark.Cli -- \
  --models "openai/gpt-5:high" \
  --auth-mode env
```

## Authentication dry run

Before running tasks, the CLI runs an authentication check for each model unless `--skip-auth-check` is set.

The check runs pi inside Docker with a minimal prompt:

```bash
pi --mode json \
  --no-session \
  --no-tools \
  --no-context-files \
  --no-skills \
  --no-extensions \
  --model <model> \
  "Respond exactly: OK"
```

Run it directly:

```bash
dotnet run --project src/DotNetAIBenchmark.Cli -- auth-check \
  --models "anthropic/claude-sonnet-4-5:high" \
  --build-image
```

If this fails, fix auth/model selection before running benchmark tasks.

## Run the PoC benchmark

Single model:

```bash
dotnet run --project src/DotNetAIBenchmark.Cli -- \
  --models "anthropic/claude-sonnet-4-5:high" \
  --build-image
```

Multiple models:

```bash
dotnet run --project src/DotNetAIBenchmark.Cli -- \
  --models "anthropic/claude-sonnet-4-5:high,openai/gpt-5:high" \
  --repetitions 1
```

With explicit local pi agent directory:

```bash
dotnet run --project src/DotNetAIBenchmark.Cli -- \
  --models "openai/<model-id>:high" \
  --pi-agent-dir .pi-agent-benchmark \
  --auth-mode mount
```

Skip the authentication check:

```bash
dotnet run --project src/DotNetAIBenchmark.Cli -- \
  --models "openai/<model-id>:high" \
  --skip-auth-check
```

## Controlled mode vs realistic mode

By default the runner uses controlled mode. It disables external pi context to reduce run-to-run noise:

```bash
--no-context-files --no-skills --no-extensions
```

Use `--realistic` to allow pi to load context files, skills, and extensions from the mounted pi agent directory and workspace:

```bash
dotnet run --project src/DotNetAIBenchmark.Cli -- \
  --models "anthropic/claude-sonnet-4-5:high" \
  --realistic
```

Controlled mode is better for reproducible benchmarking. Realistic mode is better for evaluating your actual pi setup.

## CLI options

```text
DotNetAIBenchmark.Cli - pi + Docker proof of concept

Usage:
  dotnet run --project src/DotNetAIBenchmark.Cli -- --models "anthropic/claude-sonnet-4-5:high" --build-image
  dotnet run --project src/DotNetAIBenchmark.Cli -- auth-check --models "openai/gpt-5:high"

Options:
  --models             Comma-separated model list. Required.
  --dataset            Task JSONL file. Default: datasets/poc-dotnet-tasks.jsonl
  --repetitions        Repetitions per task/model. Default: 1
  --results-dir        Output directory. Default: results
  --image              Docker image. Default: dotnet-ai-benchmark-pi:local
  --dockerfile         Dockerfile. Default: docker/pi-runner.Dockerfile
  --build-image        Build the image before running.
  --pi-agent-dir       pi agent directory to mount. Default: PI_CODING_AGENT_DIR, .pi-agent-benchmark, or ~/.pi/agent
  --auth-mode          mount|env|none. Default: mount
  --skip-auth-check    Do not run an authentication dry run before the tasks.
  --provider-retries   Attempts per scenario when provider errors occur. Default: 3
  --realistic          Load pi skills/extensions/context files. Default is controlled, without skills/extensions/context files.
```

## Dataset format

Datasets are JSONL files. Each non-empty line is one task.

Example:

```json
{"id":"api-order-total-001","category":"api-bugfix","fixture":"fixtures/api-order-total-001","prompt":"Read task.md and expected-behavior.md, inspect the public tests, fix the scenario, and run `dotnet test tests/PublicTests/PublicTests.csproj`. Do not hardcode for the visible tests.","validationCommands":["dotnet test tests/PublicTests/PublicTests.csproj"],"hiddenValidationCommands":["dotnet test hidden-tests/HiddenTests/HiddenTests.csproj"],"timeoutSeconds":600}
```

Fields:

- `id`: stable task identifier.
- `category`: grouping label for reports.
- `fixture`: path to a disposable project fixture.
- `prompt`: task text sent to pi.
- `validationCommands`: public validation commands executed after pi finishes, inside Docker, outside the agent.
- `hiddenValidationCommands`: hidden validation commands executed only after public validation passes. Hidden tests are copied into the workspace after the agent run, so the model cannot inspect them.
- `timeoutSeconds`: timeout for the pi run and validation commands.

## What happens during a run

For each task/model/repetition, the CLI:

1. Creates a workspace under `results/benchmark-0.1.0/<model-name>/run-YYYYMMDD/workspaces/...`.
2. Copies the fixture into that workspace, excluding generated folders such as `bin/`, `obj/`, `.git/`, `.vs/`, and `hidden-tests/`.
3. Adds workspace ignore rules for generated .NET artifacts.
4. Initializes a git repository and commits the clean baseline.
5. Runs pi inside Docker with `read`, `bash`, `edit`, and `write` enabled.
6. Retries the scenario up to 3 times when pi reports a provider-side assistant error, resetting the workspace to the clean git baseline before each retry.
7. Copies `hidden-tests/` into the workspace only after the final agent attempt finishes.
8. Parses pi JSONL events from stdout.
9. Measures:
   - total duration
   - time to first token
   - tool calls
   - bash calls
   - tool errors
   - input tokens
   - output tokens
   - cache read/write tokens
   - total cost reported by pi
10. Runs public and hidden validation commands inside Docker, outside the agent.
11. Saves the workspace diff. Generated build artifacts are ignored so the patch focuses on source changes.
12. Updates JSON and Markdown reports.

## Outputs

Each benchmark run creates a versioned history directory grouped by model name and run date:

```text
results/benchmark-0.1.0/openrouter_tencent_hy3-preview_free_high/run-20260505/
├── report.json
├── summary.md
└── workspaces/
    └── <task-id>/
        └── <model>/
            └── rep-1/
                ├── pi.stdout.jsonl
                ├── pi.stderr.log
                ├── diff.patch
                └── ...fixture files...
```

Important files:

- `report.json`: full structured results, including `BenchmarkVersion`, agent attempts, and provider errors.
- `summary.md`: compact table for humans, including the benchmark version, attempt counts, and provider-error counts.
- `pi.stdout.jsonl`: raw pi JSON event stream from the final attempt.
- `pi.stderr.log`: pi/docker stderr from the final attempt.
- `pi.attempt-*.stdout.jsonl`: raw pi JSON event stream for each attempt.
- `pi.attempt-*.stderr.log`: pi/docker stderr for each attempt.
- `diff.patch`: code changes made by the agent.

## Methodology

Fixture design and validation methodology is documented in [METHODOLOGY.md](METHODOLOGY.md).

In short:

- scenarios are small but realistic engineering tasks;
- public tests provide the visible failure signal;
- hidden tests are copied only after the agent run;
- a scenario passes only when both public and hidden validation pass;
- Docker isolates the execution environment;
- `diff.patch` is generated from a clean git baseline and ignores generated artifacts.

## Scenario set

The initial dataset contains 20 scenarios:

- 4 ASP.NET Core Minimal API bug-fixing scenarios.
- 4 EF Core scenarios.
- 3 authentication/authorization scenarios.
- 3 BDD scenarios with Reqnroll.
- 2 performance scenarios.
- 2 architectural refactoring scenarios.
- 2 unit/integration testing scenarios.

Each scenario has:

```text
fixtures/<scenario-id>/
├── task.md
├── expected-behavior.md
├── src/
├── tests/          # public tests visible to the agent
└── hidden-tests/   # hidden tests copied only after the agent run
```

Run a public suite manually:

```bash
dotnet test fixtures/api-order-total-001/tests/PublicTests/PublicTests.csproj
```

Run a hidden suite manually:

```bash
dotnet test fixtures/api-order-total-001/hidden-tests/HiddenTests/HiddenTests.csproj
```

## Latest local benchmark sample

A local run with benchmark version `0.1.0` and `openai-codex/gpt-5.5:high` over the 20-scenario dataset produced this result:

```text
Passed: 18/20
Score: 90%
Total reported cost: 1.495189
Input tokens: 143,165
Output tokens: 22,190
```

Category breakdown:

```text
api-bugfix:                 4/4
ef-core:                    4/4
auth-authorization:         2/3
bdd-reqnroll:               3/3
performance:                2/2
architectural-refactoring:  1/2
testing:                    2/2
```

This is a sample local run, not a stable leaderboard result. Provider availability, subscription limits, model updates, and local pi configuration can change the outcome.

## Troubleshooting

### Docker command works but no containers are listed

That is normal. `docker ps` only lists running containers. The benchmark uses short-lived containers with `docker run --rm`, so they disappear after completion.

### Docker image not found

Build it:

```bash
dotnet run --project src/DotNetAIBenchmark.Cli -- \
  --models "<model>" \
  --build-image
```

Or manually:

```bash
docker build -f docker/pi-runner.Dockerfile -t dotnet-ai-benchmark-pi:local .
```

### Auth check fails

Check these points:

1. The model ID is valid:
   ```bash
   pi --list-models <provider>
   ```
2. `.pi-agent-benchmark/auth.json` exists if using `--auth-mode mount`.
3. API key environment variables are exported if using `--auth-mode env`.
4. The Docker image was rebuilt after changing the Dockerfile.
5. Try running pi directly inside the container:
   ```bash
   docker run --rm -it \
     -v "$PWD/.pi-agent-benchmark:/pi-agent" \
     -e PI_CODING_AGENT_DIR=/pi-agent \
     dotnet-ai-benchmark-pi:local \
     pi --mode json --no-session --no-tools --model "<model>" "Respond exactly: OK"
   ```

### OpenAI subscription does not work in Docker

Subscription auth depends on pi OAuth tokens. Make sure the mounted `auth.json` contains valid tokens and that pi can refresh them. If you need fully headless behavior without prior OAuth bootstrap, use `OPENAI_API_KEY` with `--auth-mode env`.

### Cost is zero or missing

Subscription providers may not report token cost the same way API-key providers do. The runner records whatever pi includes in the JSON event stream.

### The fixture still fails after a run

Open the run workspace and inspect:

```bash
cd results/benchmark-0.1.0/<model-name>/run-YYYYMMDD/workspaces/<task-id>/<model>/rep-1
cat pi.stderr.log
cat diff.patch
```

You can also run validation manually:

```bash
docker run --rm -v "$PWD:/workspace" -w /workspace dotnet-ai-benchmark-pi:local \
  bash -lc "dotnet test tests/PublicTests/PublicTests.csproj && dotnet test hidden-tests/HiddenTests/HiddenTests.csproj"
```

## Security notes

- `.pi-agent-benchmark/auth.json` contains secrets and is ignored by git.
- Do not commit benchmark credentials.
- The agent runs with `read`, `bash`, `edit`, and `write` inside the disposable workspace.
- The current Docker isolation is intended for local benchmarking, not hostile code execution.
- The pi agent directory mount is read-write so tokens can refresh.

## Clean up

Remove generated results:

```bash
rm -rf results
```

Remove the Docker image:

```bash
docker rmi dotnet-ai-benchmark-pi:local
```
