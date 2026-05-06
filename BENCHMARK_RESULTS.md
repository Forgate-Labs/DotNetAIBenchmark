# Benchmark Results Summary

This file summarizes the local benchmark runs executed so far.

Benchmark version: `0.1.0`

> These are local results, not a stable leaderboard. Results can change with provider availability, model updates, subscription limits, pi configuration, Docker image changes, and fixture revisions.

## Run history

| Run | Model | Dataset size | Passed | Score | Reported cost | Input tokens | Output tokens | Cache read tokens | Agent duration |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `run-20260505-165810` | `openai-codex/gpt-5.5:high` | 1 | 1/1 | 100% | 0.075267 | 11,103 | 428 | 13,824 | 38.9s |
| `run-20260505-170155` | `openai-codex/gpt-5.5:high` | 1 | 1/1 | 100% | 0.039600 | 4,436 | 410 | 10,240 | 35.0s |
| `run-20260505-171444` | `openai-codex/gpt-5.5:high` | 1 | 1/1 | 100% | 0.042272 | 4,634 | 449 | 11,264 | 31.4s |
| `run-20260505-174107` | `openai-codex/gpt-5.5:high` | 20 | 18/20 | 90% | 1.495189 | 143,165 | 22,190 | 227,328 | 35m55s |
| `run-20260505-192353` | `openrouter/tencent/hy3-preview:free:high` | 20 | 14/20 | 70% | 0.000000 | 80,580 | 46,324 | 433,664 | 38m49s |

## Result locations

Historical result folders are stored under:

```text
results/benchmark-0.1.0/<model-name>/<run-id>/
```

Current known result folders:

```text
results/benchmark-0.1.0/openai-codex_gpt-5.5_high/run-20260505-165810/
results/benchmark-0.1.0/openai-codex_gpt-5.5_high/run-20260505-170155/
results/benchmark-0.1.0/openai-codex_gpt-5.5_high/run-20260505-171444/
results/benchmark-0.1.0/openai-codex_gpt-5.5_high/run-20260505-174107/
results/benchmark-0.1.0/openrouter_tencent_hy3-preview_free_high/run-20260505-192353/
```

New runs created after the run-folder change use date-only names such as `run-20260505`. If another run for the same model/date already exists, the runner appends a numeric suffix such as `run-20260505-2`.

## Full 20-scenario comparison

| Category | `openai-codex/gpt-5.5:high` | `openrouter/tencent/hy3-preview:free:high` |
|---|---:|---:|
| API bug fixing | 4/4 | 2/4 |
| EF Core | 4/4 | 3/4 |
| Authentication/authorization | 2/3 | 2/3 |
| BDD with Reqnroll | 3/3 | 2/3 |
| Performance | 2/2 | 2/2 |
| Architectural refactoring | 1/2 | 1/2 |
| Testing | 2/2 | 2/2 |
| **Total** | **18/20** | **14/20** |

## Failed scenarios by model

### `openai-codex/gpt-5.5:high`

Failed scenarios in the full 20-scenario run:

| Scenario | Category | Public tests | Hidden tests | Failure summary |
|---|---|---:|---:|---|
| `auth-tenant-isolation-001` | `auth-authorization` | Passed | Failed | Missing `X-Tenant` returned `404 NotFound`; hidden test expected `401 Unauthorized`. |
| `refactor-clock-service-001` | `architectural-refactoring` | Passed | Failed | Used injected clock, but expiration boundary used `>` instead of hidden-test expected `>=`. |

### `openrouter/tencent/hy3-preview:free:high`

Failed scenarios in the full 20-scenario run:

| Scenario | Category | Public tests | Hidden tests | Failure summary |
|---|---|---:|---:|---|
| `api-order-total-001` | `api-bugfix` | Failed | Not run | Order total still ignored item quantity. Expected `25`, got `15`. |
| `api-json-contract-001` | `api-bugfix` | Failed | Not run | JSON response did not expose the expected `displayName` property. |
| `ef-pagination-001` | `ef-core` | Failed | Not run | Repository returned all rows instead of applying page size. |
| `auth-tenant-isolation-001` | `auth-authorization` | Passed | Failed | Missing `X-Tenant` returned `404 NotFound`; hidden test expected `401 Unauthorized`. |
| `bdd-cart-discount-001` | `bdd-reqnroll` | Failed | Not run | Discount threshold was not fixed. Expected total `90`, got `100`. |
| `refactor-clock-service-001` | `architectural-refactoring` | Failed | Not run | Implementation still used system time behavior instead of satisfying the injected-clock public test. |

## Single-scenario exploratory runs

Before the 20-scenario dataset existed, three exploratory runs were executed against the original single Minimal API fixture. All passed with `openai-codex/gpt-5.5:high`.

| Run | Notes |
|---|---|
| `run-20260505-165810` | First Docker/pi benchmark proof of concept. Passed, but diff included generated artifacts before cleanup changes. |
| `run-20260505-170155` | Re-run after generated-artifact cleanup. Passed with clean source diff. |
| `run-20260505-171444` | Re-run after removing an obvious bug comment from the fixture. Passed with clean source diff. |

## Interpretation

- `openai-codex/gpt-5.5:high` currently performs best on the local 20-scenario benchmark, with a 90% pass rate.
- `openrouter/tencent/hy3-preview:free:high` completed the same dataset with a 70% pass rate and zero reported cost.
- Both models failed `auth-tenant-isolation-001` on the same hidden edge case, which suggests that the scenario wording should be reviewed for fairness.
- Both models struggled with `refactor-clock-service-001`, but for different validation stages.
- The benchmark is still version `0.1.0`; scenario wording, hidden-test fairness, scoring, and reporting are expected to evolve.
