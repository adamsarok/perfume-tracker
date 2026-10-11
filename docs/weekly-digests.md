# Weekly fragrance digests

The dedicated digest agent selects owned perfumes for the six standard recommendation categories and a weekly mood or occasion it chooses itself. Each category has five distinct picks when suitable candidates exist, a summary, and reasons. It prefers varied houses, families, and notes and considers the previous four digests. Small collections or insufficient suitable candidates are explained rather than filled with invented recommendations.

Read digests at `/perfumes/weekly-digest`. The page includes completed history and perfume links. It does not generate digests on demand, create chat conversations, or change recommendation mission/stat counts.

## Enable a user

The migration adds `AspNetUsers.WeeklyDigestEnabled`, defaulting to false for existing and new users. This operational setting is ignored by JSON serialization and has no user-facing settings control. Enable a selected user in the application's database:

```sql
UPDATE "AspNetUsers"
SET "WeeklyDigestEnabled" = TRUE
WHERE "Id" = '<user UUID>'::uuid;
```

Set it to false to stop future generation. Existing completed digests remain readable. Enable one user first and inspect the digest and usage before enabling more users. The server applies the included EF migration through its existing startup migration mechanism.

## Schedule and configuration

The hosted service checks on startup and hourly afterward. The newest due week starts Sunday at 00:00 UTC and becomes due at 06:00 UTC. A restart catches up that latest due week; older missed weeks are not backfilled. Before Sunday 06:00, the latest due week is the previous Sunday. Users are processed sequentially, and users without a profile or eligible collection are skipped without LLM usage.

Generation requires the existing `OpenAI:ApiKey` and `OpenAI:AssistantModel` configuration. `WeeklyDigest:Model` optionally overrides the assistant model for newly created runs; a persisted run retains its original model across retries.

| Setting under `WeeklyDigest` | Default | Purpose |
| --- | ---: | --- |
| `MaxTokens` | 150000 | Total generation input and output tokens per user/week, shared across attempts |
| `FinalizationReserve` | 40000 | Capacity withheld from exploration for final writing |
| `MaxOutputTokens` | 25000 | Maximum output, including reasoning, per generation request |
| `MaxExplorationRequests` | 50 | Additional safeguard against agent loops |
| `MaxAttempts` | 3 | Maximum scheduled attempts for transient failures or interrupted runs |

Token accounting uses input/output usage returned by generation responses; there are no input-token preflight requests. Exploration stops at `MaxTokens - FinalizationReserve`. At cutoff, no further tools execute and the model receives a fresh evidence prompt with tools disabled. Final evidence deduplicates perfumes across category pools. One repair is allowed while reported usage remains below the budget. Each response has an output cap; the cumulative budget is a soft limit because the next response's input usage is known only after completion. Candidate tools use database queries without nested LLM or embedding API calls.

Before sending each request, the runner checkpoints an output allowance, then replaces it with returned total usage. A crash or ambiguous network failure retains that allowance; unknown input usage can exceed it. Retries retain cumulative usage and are capped at three attempts. The dedicated HTTP client has no automatic generation retries. This is a generous cost guard rather than a strict token or dollar guarantee.

## Recovery and monitoring

`WeeklyDigest` has a unique `(UserId, WeekStart)` index. An atomic claim grants a two-minute lease, renewed every 30 seconds. Updates check lease ownership; a worker losing its lease cancels. Checkpoints preserve exploration history, encrypted reasoning, pending tool calls, collected evidence, and final-writing state.

Statuses are `Pending`, `Running`, `Completed`, `Skipped`, and `Failed`. Transient HTTP/network failures retry at later hourly checks within the same budget and attempt limit. Exhausted or terminal failures remain unpublished for that week. Interrupted work can resume when its lease expires. Completed payloads are validated again against current collection eligibility before publication.

Inspect `Status`, `Attempts`, `TokensUsed`, `LastError`, and `CompletedAt` in the database. `TokensUsed` includes output allowances for requests whose actual usage is unknown. Logs record outcomes, tokens, exploration requests, and tool calls. Traces use `weekly_digest.generate`; existing token metrics use operation `weekly_digest`.

Processing-start logs identify the digest, user, week, attempt, model, and starting token usage. Each model step logs its persisted step number and phase (`Exploration`, `FinalAnswer`, or `Repair`), requested tool count and names, input/output token usage, and cumulative tokens against the limit. Logs distinguish graceful completion, final answers following a token-limit exploration cutoff, and termination when the final-answer or response-output token limit is reached. Failed requests explicitly report retained output allowances.

Read APIs require the existing `Read` policy and tenant isolation:

- `GET /api/weekly-digests?page=1`: completed history, 20 summaries per page, with `items`, `total`, and `page`.
- `GET /api/weekly-digests/{id}`: completed digest with its week and publication time. Other tenants' and unpublished records return 404.

Snapshots retain perfume names. Deleted perfumes remain in historical digests with `isAvailable: false`, so the UI displays their names without links. No read endpoint invokes the model.

## Verification

Digest tests mock every LLM request. Database integration tests use only `ConnectionStrings:PerfumeTrackerTest`, require a database name containing `test`, apply migrations there, and remove only records belonging to their generated test users. They cover query translation, competing claims, expired-lease recovery, bounded retries, API authorization, tenant isolation, and deleted perfume snapshots.

```powershell
dotnet test PerfumeTracker.xTests/PerfumeTracker.xTests.csproj --filter FullyQualifiedName~WeeklyDigest
npm run build --prefix perfumetracker.client
```

Lease renewal stops by disposing its timer on normal completion. Actual renewal failures are logged and cancel generation; host cancellation preserves the checkpoint for recovery.
