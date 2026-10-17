# PerfumeTracker.Evals

Eval harness for the chat agent (`PerfumeTracker.Server/Features/ChatAgent`). It runs the **real**
agent - real system prompt builder, real tools against a real PostgreSQL test database, real
`ChatAgent` loop - and scores the resulting trajectory.

It is a console project, deliberately not a hosted service and not a unit test project:

- a hosted service would ship eval code to production, need the dataset in the prod database, and
  run nondeterministic, paid model calls on a schedule;
- an xUnit-only home cannot gate on model behaviour (no API key in CI, nondeterministic, paid);
- a separate Python script would have to reimplement `ChatSystemPromptBuilder`, `ChatAgentTools`
  (EF Core + pgvector) and `ChatAgentResponseProtocol`, which drifts from production immediately.

Deterministic, key-free assertions that do not need a model still belong in `PerfumeTracker.xTests`.

## Usage

### Weekly digest suite

`--suite weekly-digest` runs the production `WeeklyDigestAgent` and `DigestCandidates` against
synthetic tenants in the dedicated PostgreSQL **test** database. It never queries a production
collection, starts workers, sends a digest, or enables scheduling. Only the selected cases' synthetic
tenant rows are reset; fixture data remains available for inspection afterward.

```bash
# Nine bounded scenarios, live generation plus rubric judging; save responses for replay
dotnet run --project PerfumeTracker.Evals -- --suite weekly-digest --mode record --out results/digest-recorded

# Rerun real candidate queries with saved model responses, without an API key or paid calls
dotnet run --project PerfumeTracker.Evals -- --suite weekly-digest --mode replay --out results/digest-replay

# Optional production-budget case (excluded unless explicitly selected)
dotnet run --project PerfumeTracker.Evals -- --suite weekly-digest --mode record --filter production-budget --out results/digest-production
```

Use `--model` to override `WeeklyDigest:Model` / `OpenAI:AssistantModel`. Credentials use server
User Secrets (`OpenAI:ApiKey`), environment variables, or the existing `--api-key` override.
Configure `ConnectionStrings:PerfumeTrackerTest`; the database name must contain `test`.
Environment variables override secrets and test settings. The host applies database migrations
only after validating that dedicated test connection.

Visual Studio: set **PerfumeTracker.Evals** as startup project and choose **Weekly digest - record**
or **Weekly digest - replay**. Both profiles use the server's configured model and secrets.

Cases are in `Cases/weekly-digest.cases.json`: diverse wardrobes, two/no eligible perfumes,
eligibility decoys, four prior digests, fixed winter/summer dates, missing embeddings, and hostile
collection data. Fixtures use a 1–5 rating scale, minimum eligible rating 3, deterministic IDs,
synthetic vectors, and frozen time. Smoke budgets are 3 exploration requests, 60k tokens,
20k finalization reserve and 10k output per request. The extended case retains production defaults.

Recordings live under `Recordings/weekly-digest/<fingerprint>/<case-id-hash>/`. The fingerprint
includes the initial canonical request, fixture definition, profile and budget. Every call also
verifies its request hash, including finalization and repair. Stale or missing recordings fail;
replay never falls back to live generation. Commit these synthetic response fixtures.

Reports include payloads, model requests/responses, candidate pages, checkpoint snapshots, stop
reason, repair use, latency and token usage. Diversity and previous-week overlap are reported
without arbitrary hard limits. Deterministic gates validate structure, eligibility/evidence,
candidate arguments, shortages, and exploration/repair policy. Finalization budget overruns
permitted by production are reported rather than incorrectly rejected.

The judge receives bounded context, selected evidence, alternatives, history and final output.
Live/record runs fail if judging fails or a rubric violation is found; `--no-judge` disables it.
Replay disables judging by default; `--judge-model MODEL` explicitly enables paid judging of
saved agent responses. `--filter`, `--cases`, `--recordings`, `--out`, `--baseline`, and `--no-fail`
also work for this suite. Reports are saved after each case so a later interruption retains progress.

Digest generation uses Polly to retry HTTP 408, 429, and 5xx (including 520), network failures,
and timeouts. A valid `Retry-After` seconds/date header takes precedence, without shortening
the server's requested wait. Otherwise exponential backoff with jitter starts at 2 seconds and
is capped at 30 seconds. Configure `WeeklyDigest:MaxGenerationRetries` (default 3, zero disables),
`RetryBaseDelaySeconds`, and `RetryMaxDelaySeconds`; the delay cap applies only to the fallback.
Cancellation interrupts waits. Retries stop when the remaining token budget/reserve cannot
support another attempt. Each physical attempt checkpoints its own output allowance; uncertain
failed attempts retain that allowance. The scheduler's `MaxAttempts` is a separate run-level limit.
Recordings capture transient failures as well as successful responses, preserving replay budget
decisions. Replay skips actual retry waits. Existing successful recordings remain compatible.

Existing chat commands remain unchanged (`--suite chat` is the default).

### Chat suite

```bash
# Key-free regression run from recorded responses
dotnet run --project PerfumeTracker.Evals -- --mode replay

# Harness self-test (hand-written fixture, no key needed)
dotnet run --project PerfumeTracker.Evals -- --mode replay --recordings Fixtures/smoke --filter small-talk

# Real model run, then record the responses so CI can replay them
dotnet run --project PerfumeTracker.Evals -- --mode record --model gpt-5 --api-key $OPENAI_API_KEY --out results/recorded

# Compare against a previous run (exits non-zero on a pass -> fail regression)
dotnet run --project PerfumeTracker.Evals -- --mode replay --baseline results/results.json
```

`--help` lists every option.

## Collection facts and rebuy coverage

The fixture uses ratings on a 1–5 scale (zero means unrated). Default rebuy candidates have
**less than 10% remaining and rating above 3**, including finished bottles. A 2 ml sample
with 1.9 ml remaining is 95% full and does not qualify. Exactly 10% and rating exactly 3
are excluded. Capacity must be positive to calculate a percentage.

`filter_owned_perfumes` supports `remainingPercentBelow` (exclusive, 0–100), `ratingAbove`
(exclusive), and `onlyAvailable=false` to include empty bottles. Existing `minRating` and
`maxRating` remain inclusive. Tool results include `Ml` (capacity), `MlLeft`, and nullable
`RemainingPercent`. Explicit user thresholds override the default rebuy policy.

Cases cover rebuy boundaries, threshold overrides, any-rating inventory, small samples,
unknown capacity, neglected favorites, compound filters, no matches, and ambiguous vetiver
requests. A restock buy list is a collection query; searching marketplace offers requires
a shopping/offer request.

The judge receives tool arguments and bounded returned results as evidence, not just the final answer.

Case assertions may specify `requiredToolArguments`, a list of `{ "tool": "filter_owned_perfumes",
"arguments": { "remainingPercentBelow": 10, "onlyAvailable": false } }` objects. Each subset
must match one complete call; extra arguments are allowed. `forbiddenOwnedLinks` lists owned
display names that must not be linked. The judge still checks explanations and ambiguity.

```bash
# Save new recordings after prompt/tool/fixture changes, then replay them
dotnet run --project PerfumeTracker.Evals -- --mode record --model YOUR_MODEL --filter rebuy --out results/rebuy-recorded
dotnet run --project PerfumeTracker.Evals -- --mode replay --no-judge --filter rebuy --out results/rebuy-replay
```

Record the full suite to refresh all cases after these changes. Old fingerprints are intentionally
not reused. Use a unique `--out` directory for each report you want to retain.
The application's existing 0–10 API validation is not migrated by this eval expansion.

Requires the same PostgreSQL test database as `PerfumeTracker.xTests`
(`ConnectionStrings:PerfumeTrackerTest`, database name must contain `test`) and a real
`OpenAI:AssistantModel` for `real`/`record` modes - the repo default is a placeholder.

## Layout

| Path | Purpose |
| --- | --- |
| `Cases/chat-agent.cases.json` | Cases: message, expected/forbidden tools, expected owned links, rubric |
| `Harness/EvalHost.cs` | Boots the server in-process, swaps in the eval model client, disables background workers |
| `Harness/EvalWardrobe.cs` | Deterministic wardrobe + marketplace offers (ids derived from stable keys) |
| `Harness/ChatAgentRunner.cs` | Runs a case, rebuilds the trajectory from persisted `ChatMessage` rows |
| `Recording/` | Record/replay of `ResponseResult` keyed by prompt + tool fingerprint |
| `Scoring/` | Deterministic scorers + rubric judge |
| `Reporting/` | `results.json`, `summary.md`, baseline regression diff |
| `Recordings/` | Recorded model responses (committed) |
| `Fixtures/smoke/` | Hand-written recordings for the key-free self-test |

## Scoring

Deterministic (always run):

- **completion** - the run finished and produced an answer
- **tool-selection** - expected tools called, forbidden tools not called
- **tool-arguments** - tool names registered; `count`/`perfumes` within `ChatAgent:MaxResultsPerToolCall`;
  arguments parse as JSON
- **perfume-references** - every `/perfumes/<id>` link points at a perfume the user actually owns,
  and required perfumes are linked (catches invented ids)
- **iteration-budget** - stays inside `ChatAgent:MaxIterations`
- **title** - a real title was generated, short, not the truncated user message

Model-based (skipped when no judge model is configured):

- **llm-judge** - rubric grading via a second model call. Use `--judge-model` to grade with a
  different (ideally stronger) model than the one under test, and pin the model version so runs
  are comparable.

## How replay stays honest

Recordings are keyed by a fingerprint of the system prompt **and** the tool schemas, so any prompt
or tool change makes the recordings stale and the run reports
`No recorded response for fingerprint ... case ... call ...` instead of passing on old behaviour.
Scores are then derived from a fresh run of the real tools against the seeded database, not from the
recording.

Known limits:

- Cases tagged `requiresApi` (fuzzy search via embeddings + `ChatClient`) are skipped in replay
  mode because `IEncoder` is replaced with `NullEncoder` there.
- The fingerprint normalises line order before hashing, because the favourite-tags section comes
  from an unordered query in `UserStatsService`.

## Suggested CI wiring (not added yet)

- PR job: `--mode replay --baseline results/results.json` against a Postgres service (no secrets).
- Nightly `schedule` job: `--mode real --model <model> --judge-model <stronger model> --baseline ...`
  with `OpenAI__ApiKey` from secrets, failing on regressions. Reuses the Postgres setup of the
  existing `docker-image.yml` workflow.
