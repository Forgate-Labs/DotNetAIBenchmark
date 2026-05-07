# Benchmark Fixture Methodology

This document describes how the benchmark fixtures are defined, generated, and validated.

## Goals

The fixture set is designed to evaluate whether an LLM-powered coding agent can perform small but realistic .NET engineering tasks. The scenarios are intentionally compact so a model can understand them in one session, but each scenario includes a real bug, incomplete behavior, or architectural flaw.

The benchmark prioritizes objective validation over subjective grading. A run passes a scenario only when the public and hidden validation commands pass after the agent finishes.

## Scenario planning process

The initial 20 scenarios were designed with two independent planning passes using subagents:

1. One pass focused on ASP.NET Core Minimal API, EF Core, and authentication/authorization scenarios.
2. Another pass focused on Reqnroll BDD, performance, architectural refactoring, and testing scenarios.

The resulting ideas were normalized into a single fixture format and implemented as small .NET 10 projects.

This split was used to reduce scenario-design bias and to make sure the benchmark covers multiple kinds of engineering reasoning instead of only simple bug fixes.

## Scenario distribution

The current dataset contains 21 scenarios:

| Category | Count | Purpose |
|---|---:|---|
| ASP.NET Core Minimal API bug fixing | 4 | Validate HTTP contracts, status codes, input validation, and JSON shape. |
| EF Core | 4 | Validate data access reasoning, filtering, pagination, tracking, and query efficiency. |
| Authentication/authorization | 3 | Validate role checks, tenant isolation, and ownership rules. |
| BDD with Reqnroll | 3 | Validate behavior-driven reasoning from feature files and step definitions. |
| Performance | 2 | Validate practical performance fixes such as allocation reduction and cancellation handling. |
| Architectural refactoring | 2 | Validate dependency inversion, testability, and clock abstraction. |
| Unit/integration testing | 2 | Validate edge-case testing and API integration contracts. |
| Frontend application creation | 1 scenario / 12 evaluation items | Validate root-level Blazor Server app creation, Tailwind setup, component tests, container artifacts, and deterministic architectural heuristics. |

## Technical constraints

All fixtures follow these constraints where applicable:

- .NET 10
- Modern C#
- Nullable reference types enabled
- Implicit usings enabled
- xUnit for tests
- FluentAssertions when useful
- ASP.NET Core Minimal APIs for API scenarios
- Blazor Server / interactive server rendering for frontend scenarios
- EF Core for data-access scenarios
- SQLite in-memory for EF Core scenarios
- WebApplicationFactory for API integration tests
- Reqnroll for BDD scenarios

The fixtures avoid unnecessary complexity. The point is not to test whether a model can navigate a huge codebase, but whether it can understand a focused engineering problem, make an appropriate fix, and avoid regressions.

## Fixture structure

Each fixture has this shape:

```text
fixtures/<scenario-id>/
├── task.md
├── expected-behavior.md
├── AGENTS.md
├── <scenario-id>.slnx
├── src/
├── tests/
│   └── PublicTests/
└── hidden-tests/
    └── HiddenTests/
```

The agent sees only the files copied into the benchmark workspace before execution. The runner excludes `hidden-tests/` from the initial workspace copy.

## Public and hidden tests

Each scenario has two validation layers.

### Public tests

Public tests are visible to the agent. They cover the basic failure mode and provide enough signal for the model to identify the problem.

Public tests are intentionally not exhaustive. They should not be enough to reward a hardcoded fix.

### Hidden tests

Hidden tests are physically stored under `hidden-tests/`, but the runner does not copy them into the agent workspace until after the agent has finished.

Hidden tests cover:

- edge cases
- regressions
- invalid inputs
- boundary conditions
- multi-tenant or authorization bypasses
- performance constraints
- hardcoding-resistant cases

A scenario passes only when both public and hidden validation commands pass.

## Task files

Each scenario includes:

- `task.md`: short instructions for the agent.
- `expected-behavior.md`: the intended behavior and domain rules.

The task text should describe the problem without giving away the exact line to change. For example, it should say that a validation project is failing and the root cause must be fixed, not point to a specific bug comment.

## Intentional flaws

The fixture set includes flaws such as:

- incorrect business-rule calculation
- missing validation
- incorrect HTTP status codes
- multi-tenant authorization bugs
- EF Core N+1 queries
- missing pagination
- incorrect EF Core tracking usage
- async cancellation bugs
- idempotency or boundary mistakes
- bloated or hard-to-test services
- missing edge-case tests
- JSON contract mismatches
- incorrect DateTime/DateTimeOffset boundary handling
- unnecessary allocations

## Runner isolation model

Each scenario run is isolated:

1. The fixture is copied to a disposable workspace under `results/benchmark-0.2.0/<model-name>/run-YYYYMMDD/workspaces/...`.
2. Generated folders such as `bin/`, `obj/`, `.git/`, `.vs/`, and `hidden-tests/` are excluded from the initial copy.
3. A workspace `.gitignore` is created for generated .NET artifacts.
4. A clean git baseline is committed before the agent runs.
5. The agent runs inside Docker through `pi --mode json`.
6. If pi reports a provider-side assistant error, the workspace is reset to the clean git baseline and the scenario is retried up to 3 attempts by default.
7. Hidden tests are copied into the workspace only after the final agent attempt ends.
8. Public and hidden validation commands run outside the agent. They run inside Docker by default; commands prefixed with `host:` run on the host from the workspace directory.
9. Host validation commands receive unique `DOTNET_AI_BENCHMARK_VALIDATION_ID`, `DOTNET_AI_BENCHMARK_DOCKER_IMAGE_TAG`, and `COMPOSE_PROJECT_NAME` values so Docker resources do not collide during parallel runs.
10. The final source diff is saved as `diff.patch`.
11. Diagnostic diff metrics are saved as `diff-metrics.json` and included in reports. They capture changed files, added/deleted lines, project count, package references, and changed file extensions without affecting score.

This keeps the generated diff focused on meaningful source changes and prevents build artifacts from polluting the benchmark result.

## Validation and scoring

The current PoC supports pass/fail scoring per scenario and weighted evaluation items:

- pass: pi exits successfully and all validation commands or evaluation items pass
- fail: pi fails, times out, exhausts provider-error retries, public validation fails, or hidden validation fails
- weighted item score: scenarios can define `evaluationItems` so one agent run produces multiple scored checks without duplicating the fixture in the dataset

Reports are stored in versioned model/date history folders and include the benchmark version. The report also captures supporting metrics:

- duration
- attempt count
- provider errors
- time to first token
- tool call count
- bash call count
- tool errors
- input tokens
- output tokens
- cache read/write tokens
- cost reported by pi

These metrics are diagnostic. The primary quality signal is still validation success.

## Controlled vs realistic mode

Controlled mode is the default. It disables pi context files, skills, and extensions to reduce variance:

```text
--no-context-files --no-skills --no-extensions
```

Realistic mode can be enabled with `--realistic`. It allows pi to load the user's configured context, skills, and extensions. This is useful for evaluating a real local pi setup, but less reproducible as a benchmark.

## Known limitations

This is still a proof of concept. Current limitations include:

- No statistical aggregation beyond repetitions.
- No standardized difficulty score per scenario.
- No model-independent semantic grading.
- Hidden tests are in the repository, so secrecy depends on the runner excluding them from the agent workspace.
- Scenario quality still depends on human review of task wording and hidden-test fairness.
- Some providers may report incomplete or zero cost data, especially subscription-based providers.
- Frontend UI fidelity is only partially deterministic. Static selectors, build/test commands, Tailwind/Docker checks, CodePass rules, vulnerable-package checks, and architecture heuristics can be validated automatically, but visual polish and exact ChatGPT likeness still benefit from human review.

## Adding a new scenario

To add a scenario:

1. Create `fixtures/<scenario-id>/`.
2. Add `task.md` and `expected-behavior.md`.
3. Add a small .NET 10 source project under `src/`, or require a root-level project when the scenario explicitly evaluates project creation/layout.
4. Add public xUnit tests or validation scripts under `tests/PublicTests/`.
5. Add hidden xUnit tests or validation scripts under `hidden-tests/HiddenTests/`.
6. Add a JSONL entry to `datasets/poc-dotnet-tasks.jsonl`.
7. Make sure the public tests fail before the fix.
8. Make sure the hidden tests compile and fail for the initial flawed implementation.
9. Run the benchmark and inspect `diff.patch` for artifact noise.

The scenario should be understandable, realistic, and resistant to trivial hardcoding.
