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
