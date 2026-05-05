# DotNetAIBenchmark

Proof of concept: a .NET 10 CLI that evaluates coding models by running `pi` headlessly inside Docker.

The goal is to keep benchmark executions isolated and reproducible while still using pi's provider/model/auth support.

## Current scope

This repository is a PoC, not a full benchmark product yet. It currently supports:

- Docker-based execution for every agent run.
- `pi --mode json` as the agent backend.
- A local pi agent directory for credentials/settings used by Docker.
- Authentication dry runs before benchmark tasks start.
- A simple JSONL dataset format.
- A sample .NET 10 fixture with an intentional bug.
- JSON and Markdown reports.

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
│   └── poc-dotnet-tasks.jsonl         # PoC task dataset
├── docker/
│   └── pi-runner.Dockerfile           # image with .NET 10, Node.js, and pi
├── fixtures/
│   └── minimal-api-user-bug/          # sample disposable .NET fixture
├── src/
│   └── DotNetAIBenchmark.Cli/         # benchmark runner
├── DotNetAIBenchmark.slnx
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
  --realistic          Load pi skills/extensions/context files. Default is controlled, without skills/extensions/context files.
```

## Dataset format

Datasets are JSONL files. Each non-empty line is one task.

Example:

```json
{"id":"minimal-api-user-001","category":"bugfix","fixture":"fixtures/minimal-api-user-bug","prompt":"The project contains a .NET 10 Minimal API and a failing console test. Investigate and fix the user lookup bug without removing the API. Validate with `dotnet run --project tests/Users.Api.Tests/Users.Api.Tests.csproj`.","validationCommands":["dotnet run --project tests/Users.Api.Tests/Users.Api.Tests.csproj"],"timeoutSeconds":600}
```

Fields:

- `id`: stable task identifier.
- `category`: grouping label for reports.
- `fixture`: path to a disposable project fixture.
- `prompt`: task text sent to pi.
- `validationCommands`: commands executed after pi finishes, inside Docker, outside the agent.
- `timeoutSeconds`: timeout for the pi run and validation commands.

## What happens during a run

For each task/model/repetition, the CLI:

1. Creates a workspace under `results/run-*/workspaces/...`.
2. Copies the fixture into that workspace.
3. Initializes a git repository and commits the baseline.
4. Runs pi inside Docker with `read`, `bash`, `edit`, and `write` enabled.
5. Parses pi JSONL events from stdout.
6. Measures:
   - total duration
   - time to first token
   - tool calls
   - bash calls
   - tool errors
   - input tokens
   - output tokens
   - cache read/write tokens
   - total cost reported by pi
7. Runs validation commands inside Docker, outside the agent.
8. Saves the workspace diff.
9. Updates JSON and Markdown reports.

## Outputs

Each benchmark run creates a directory like:

```text
results/run-20260505-132900/
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

- `report.json`: full structured results.
- `summary.md`: compact table for humans.
- `pi.stdout.jsonl`: raw pi JSON event stream.
- `pi.stderr.log`: pi/docker stderr.
- `diff.patch`: code changes made by the agent.

## Sample fixture

The current sample fixture is:

```text
fixtures/minimal-api-user-bug/
```

It contains a .NET 10 Minimal API and a console validation project. The intentional bug is in user lookup logic. The validation command fails before the agent fixes it:

```bash
dotnet run --project fixtures/minimal-api-user-bug/tests/Users.Api.Tests/Users.Api.Tests.csproj
```

The benchmark asks the model to investigate and fix the bug, then validates with:

```bash
dotnet run --project tests/Users.Api.Tests/Users.Api.Tests.csproj
```

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
cd results/run-*/workspaces/<task-id>/<model>/rep-1
cat pi.stderr.log
cat diff.patch
```

You can also run validation manually:

```bash
docker run --rm -v "$PWD:/workspace" -w /workspace dotnet-ai-benchmark-pi:local \
  bash -lc "dotnet run --project tests/Users.Api.Tests/Users.Api.Tests.csproj"
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
