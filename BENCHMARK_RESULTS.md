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
| `run-20260506` | `openrouter/baidu/cobuddy:free:high` | 20 | 3/20 | 15% | 0.000000 | 35,666 | 2,720 | 5,696 | 5m37s |
| `run-20260506-2` | `openrouter/baidu/cobuddy:free:high` | 20 | 4/20 | 20% | 0.000000 | 37,357 | 2,643 | 13,440 | 18m46s |
| `parallel-opencode-20260506T163957Z` | `opencode/big-pickle:high` | 20 | 15/20 | 75% | 0.000000 | 82,130 | 38,766 | 351,872 | 41m33s |
| `parallel-opencode-20260506T163957Z` | `opencode/gpt-5-nano:high` | 20 | 10/20 | 50% | 0.000000 | 117,583 | 53,187 | 505,600 | 53m40s |
| `parallel-free-models-20260506T172430Z` | `opencode/nemotron-3-super-free:high` | 20 | 18/20 | 90% | 0.000000 | 834,225 | 61,388 | 0 | 1h50m32s |
| `parallel-free-models-20260506T172430Z` | `openrouter/google/gemini-3-flash-preview:high` | 20 | 18/20 | 90% | 0.537273 | 546,662 | 85,795 | 131,132 | 56m06s |
| `parallel-free-models-20260506T172430Z` | `openrouter/minimax/minimax-m2.5:free:high` | 20 | 14/20 | 70% | 0.000000 | 71,893 | 18,805 | 244,512 | 4h42m21s |

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
results/benchmark-0.1.0/openrouter_baidu_cobuddy_free_high/run-20260506/
results/benchmark-0.1.0/openrouter_baidu_cobuddy_free_high/run-20260506-2/
results/parallel-opencode-20260506T163957Z/opencode_big-pickle_high/
results/parallel-opencode-20260506T163957Z/opencode_gpt-5-nano_high/
results/parallel-free-models-20260506T172430Z/opencode_nemotron-3-super-free_high/
results/parallel-free-models-20260506T172430Z/openrouter_google_gemini-3-flash-preview_high/
results/parallel-free-models-20260506T172430Z/openrouter_minimax_minimax-m2.5_free_high/
```

New runs created after the run-folder change use date-only names such as `run-20260505`. If another run for the same model/date already exists, the runner appends a numeric suffix such as `run-20260505-2`.

## Full 20-scenario comparison

| Category | `openai-codex/gpt-5.5:high` | `openrouter/tencent/hy3-preview:free:high` | `openrouter/baidu/cobuddy:free:high` after provider retries | `opencode/big-pickle:high` | `opencode/gpt-5-nano:high` | `opencode/nemotron-3-super-free:high` | `openrouter/google/gemini-3-flash-preview:high` | `openrouter/minimax/minimax-m2.5:free:high` |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| API bug fixing | 4/4 | 2/4 | 2/4 | 1/4 | 0/4 | 4/4 | 4/4 | 3/4 |
| EF Core | 4/4 | 3/4 | 1/4 | 4/4 | 0/4 | 4/4 | 4/4 | 2/4 |
| Authentication/authorization | 2/3 | 2/3 | 0/3 | 2/3 | 2/3 | 2/3 | 2/3 | 2/3 |
| BDD with Reqnroll | 3/3 | 2/3 | 1/3 | 3/3 | 3/3 | 3/3 | 3/3 | 2/3 |
| Performance | 2/2 | 2/2 | 0/2 | 2/2 | 2/2 | 2/2 | 2/2 | 2/2 |
| Architectural refactoring | 1/2 | 1/2 | 0/2 | 1/2 | 1/2 | 1/2 | 1/2 | 1/2 |
| Testing | 2/2 | 2/2 | 0/2 | 2/2 | 2/2 | 2/2 | 2/2 | 2/2 |
| **Total** | **18/20** | **14/20** | **4/20** | **15/20** | **10/20** | **18/20** | **18/20** | **14/20** |

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

### `opencode/big-pickle:high`

Failed scenarios in the full 20-scenario run:

| Scenario | Category | Public tests | Hidden tests | Failure summary |
|---|---|---:|---:|---|
| `api-json-contract-001` | `api-bugfix` | Failed | Not run | JSON response did not expose the expected `displayName` property. |
| `api-order-total-001` | `api-bugfix` | Failed | Not run | Order total still ignored item quantity. Expected `25`, got `15`. |
| `api-product-validation-001` | `api-bugfix` | Failed | Not run | Invalid product input returned `201 Created`; public test expected `400 BadRequest`. |
| `auth-tenant-isolation-001` | `auth-authorization` | Passed | Failed | Missing `X-Tenant` returned `404 NotFound`; hidden test expected `401 Unauthorized`. |
| `refactor-clock-service-001` | `architectural-refactoring` | Passed | Failed | Used injected clock, but expiration boundary used `>` instead of hidden-test expected `>=`. |

### `opencode/gpt-5-nano:high`

Failed scenarios in the full 20-scenario run:

| Scenario | Category | Public tests | Hidden tests | Failure summary |
|---|---|---:|---:|---|
| `api-inventory-reservation-001` | `api-bugfix` | Failed | Not run | Public validation did not restore successfully after provider retries. |
| `api-json-contract-001` | `api-bugfix` | Failed | Not run | JSON response did not expose the expected `displayName` property. |
| `api-order-total-001` | `api-bugfix` | Failed | Not run | Order total still ignored item quantity. Expected `25`, got `15`. |
| `api-product-validation-001` | `api-bugfix` | Failed | Not run | Invalid product input returned `201 Created`; public test expected `400 BadRequest`. |
| `auth-tenant-isolation-001` | `auth-authorization` | Passed | Failed | Missing `X-Tenant` returned `404 NotFound`; hidden test expected `401 Unauthorized`. |
| `ef-n-plus-one-001` | `ef-core` | Passed | Failed | Hidden query counter expected at most 2 database commands, but observed 6. |
| `ef-pagination-001` | `ef-core` | Failed | Not run | Repository returned 5 rows instead of the expected page size of 2. |
| `ef-tenant-filter-001` | `ef-core` | Failed | Not run | Tenant filtering did not return the expected single invoice. |
| `ef-tracking-update-001` | `ef-core` | Failed | Not run | Updated entity name remained `Old` instead of `New`. |
| `refactor-clock-service-001` | `architectural-refactoring` | Passed | Failed | Used injected clock, but expiration boundary used `>` instead of hidden-test expected `>=`. |

### `opencode/nemotron-3-super-free:high`

Failed scenarios in the full 20-scenario run:

| Scenario | Category | Public tests | Hidden tests | Failure summary |
|---|---|---:|---:|---|
| `auth-tenant-isolation-001` | `auth-authorization` | Passed | Failed | Missing `X-Tenant` returned `404 NotFound`; hidden test expected `401 Unauthorized`. |
| `refactor-clock-service-001` | `architectural-refactoring` | Passed | Failed | Used injected clock, but expiration boundary used `>` instead of hidden-test expected `>=`. |

### `openrouter/google/gemini-3-flash-preview:high`

Failed scenarios in the full 20-scenario run:

| Scenario | Category | Public tests | Hidden tests | Failure summary |
|---|---|---:|---:|---|
| `auth-tenant-isolation-001` | `auth-authorization` | Passed | Failed | Missing `X-Tenant` returned `404 NotFound`; hidden test expected `401 Unauthorized`. |
| `refactor-clock-service-001` | `architectural-refactoring` | Passed | Failed | Used injected clock, but expiration boundary used `>` instead of hidden-test expected `>=`. |

### `openrouter/minimax/minimax-m2.5:free:high`

Failed scenarios in the full 20-scenario run:

| Scenario | Category | Public tests | Hidden tests | Failure summary |
|---|---|---:|---:|---|
| `api-inventory-reservation-001` | `api-bugfix` | Passed | Passed | Agent timed out with pi exit code `137`; validation passed after the agent run. |
| `auth-tenant-isolation-001` | `auth-authorization` | Passed | Failed | Agent timed out with pi exit code `137`; hidden validation failed after the agent run. |
| `bdd-password-policy-001` | `bdd-reqnroll` | Passed | Passed | Agent timed out with pi exit code `137`; validation passed after the agent run. |
| `ef-n-plus-one-001` | `ef-core` | Passed | Passed | Agent timed out with pi exit code `137`; validation passed after the agent run. |
| `ef-tenant-filter-001` | `ef-core` | Passed | Passed | Agent timed out with pi exit code `137`; validation passed after the agent run. |
| `refactor-clock-service-001` | `architectural-refactoring` | Passed | Failed | Used injected clock, but expiration boundary used `>` instead of hidden-test expected `>=`. |

## Single-scenario exploratory runs

Before the 20-scenario dataset existed, three exploratory runs were executed against the original single Minimal API fixture. All passed with `openai-codex/gpt-5.5:high`.

| Run | Notes |
|---|---|
| `run-20260505-165810` | First Docker/pi benchmark proof of concept. Passed, but diff included generated artifacts before cleanup changes. |
| `run-20260505-170155` | Re-run after generated-artifact cleanup. Passed with clean source diff. |
| `run-20260505-171444` | Re-run after removing an obvious bug comment from the fixture. Passed with clean source diff. |

## Provider retry run

After adding provider-error retries, `openrouter/baidu/cobuddy:free:high` was run again as `run-20260506-2`.

Retry summary:

```text
Total scenarios: 20
Passed: 4/20
Total agent attempts: 57
Provider retry errors before final attempts: 37
Final provider errors after retries: 17
Repeated provider error: Failed to calculate accounting data
```

The retry policy recovered one additional scenario compared to the first CoBuddy run, but the OpenRouter/Baidu provider error remained frequent.

## OpenCode parallel run

`opencode/big-pickle:high` and `opencode/gpt-5-nano:high` were run as externally parallelized one-scenario dataset shards under `parallel-opencode-20260506T163957Z` with up to 16 host processes. An accidentally selected third model run is excluded from this summary.

Retry summary:

```text
opencode/big-pickle:high
Total scenarios: 20
Passed: 15/20
Total agent attempts: 20
Provider retry errors before final attempts: 0
Final provider errors after retries: 0

opencode/gpt-5-nano:high
Total scenarios: 20
Passed: 10/20
Total agent attempts: 38
Provider retry errors before final attempts: 18
Final provider errors after retries: 8
Repeated provider error: Unknown error (no error details in response)
```

## Free-model parallel run

`opencode/nemotron-3-super-free:high`, `openrouter/google/gemini-3-flash-preview:high`, and `openrouter/minimax/minimax-m2.5:free:high` were run as externally parallelized one-scenario dataset shards under `parallel-free-models-20260506T172430Z` with up to 16 host processes. `openrouter/qwen/qwen3-coder:free:high` was excluded because the stricter authentication check did not receive a model response due to upstream rate limiting.

Retry summary:

```text
opencode/nemotron-3-super-free:high
Total scenarios: 20
Passed: 18/20
Total agent attempts: 24
Provider retry errors before final attempts: 4
Final provider errors after retries: 1

openrouter/google/gemini-3-flash-preview:high
Total scenarios: 20
Passed: 18/20
Total agent attempts: 21
Provider retry errors before final attempts: 1
Final provider errors after retries: 0

openrouter/minimax/minimax-m2.5:free:high
Total scenarios: 20
Passed: 14/20
Total agent attempts: 47
Provider retry errors before final attempts: 65
Final provider errors after retries: 16
Repeated provider errors: 429 rate limits, upstream timeouts, and network connection loss
```

Minimax M2.5 free had weak benchmark performance mostly because the provider was slow to respond and aggressively rate-limited. Several scenarios produced correct code that passed public and hidden validation, but the agent process still timed out with pi exit code `137` after provider retries, so the benchmark correctly counted those runs as failures.

## Interpretation

- `openai-codex/gpt-5.5:high`, `opencode/nemotron-3-super-free:high`, and `openrouter/google/gemini-3-flash-preview:high` are currently tied for the best local 20-scenario pass rate at 90%.
- `opencode/big-pickle:high` completed the dataset with a 75% pass rate and no provider errors, performing especially well on EF Core, BDD, performance, and testing scenarios.
- `openrouter/tencent/hy3-preview:free:high` and `openrouter/minimax/minimax-m2.5:free:high` completed the same dataset with a 70% pass rate, but Minimax M2.5 free was heavily affected by provider slowness and aggressive rate limits.
- `opencode/gpt-5-nano:high` completed the dataset with a 50% pass rate, but provider errors caused many retries and several final failures.
- `openrouter/baidu/cobuddy:free:high` had severe provider stability issues in these local runs. Even with up to 3 attempts per scenario, 17 final attempts still ended with `Failed to calculate accounting data`.
- GPT-5.5, Hy3, Big Pickle, GPT-5 Nano, Nemotron 3 Super Free, Gemini 3 Flash Preview, and Minimax M2.5 Free failed `auth-tenant-isolation-001` on the same hidden edge case, which suggests that the scenario wording should be reviewed for fairness.
- GPT-5.5, Hy3, Big Pickle, GPT-5 Nano, Nemotron 3 Super Free, Gemini 3 Flash Preview, and Minimax M2.5 Free all struggled with `refactor-clock-service-001`, but for different validation stages across the runs.
- The benchmark is still version `0.1.0`; scenario wording, hidden-test fairness, scoring, retry policy, and reporting are expected to evolve.
