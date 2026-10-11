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

The hosted service checks on startup and hourly afterward. The newest due week starts Monday at 00:00 UTC and becomes due at 06:00 UTC. A restart catches up that latest due week; older missed weeks are not backfilled. Before Monday 06:00, the latest due week is the previous Monday. Users are processed sequentially, and users without a profile or eligible collection are skipped without LLM usage.

Generation requires the existing `OpenAI:ApiKey` and `OpenAI:AssistantModel` configuration. `WeeklyDigest:Model` optionally overrides the assistant model for newly created runs; a persisted run retains its original model across retries.

| Setting under `WeeklyDigest` | Default | Purpose |
| --- | ---: | --- |
| `MaxTokens` | 150000 | Total generation input and output tokens per user/week, shared across attempts |
| `FinalizationReserve` | 40000 | Capacity withheld from exploration for final writing |
| `FinalInputTokens` | 15000 | Maximum input size of a final-writing request |
| `MaxOutputTokens` | 25000 | Maximum output, including reasoning, per generation request |
| `MaxExplorationRequests` | 50 | Additional safeguard against agent loops |
| `MaxAttempts` | 3 | Maximum scheduled attempts for transient failures or interrupted runs |

Every generation request is preflighted with `POST /v1/responses/input_tokens`, including tool definitions and the final output schema. Exploration continues only when the input and maximum output fit below the finalization reserve. At cutoff, no further tools execute; the model receives a fresh, bounded evidence prompt with tools disabled. One output-repair request is allowed only within the remaining budget. Candidate tools query the database without nested LLM or embedding API calls; similarity uses stored embeddings.

The runner durably reserves input plus maximum output before sending a generation request, then reconciles to returned usage. Replayed input and reasoning are counted. A crash or ambiguous network failure retains the full reservation, preventing a retry from resetting spend. The dedicated HTTP client has no automatic generation retries. Token consumption bounds cost for a configured model; this is not a dollar-price guarantee.

## Recovery and monitoring

`WeeklyDigest` has a unique `(UserId, WeekStart)` index. An atomic claim grants a two-minute lease, renewed every 30 seconds. Updates check lease ownership; a worker losing its lease cancels. Checkpoints preserve exploration history, encrypted reasoning, pending tool calls, collected evidence, and final-writing state.

Statuses are `Pending`, `Running`, `Completed`, `Skipped`, and `Failed`. Transient HTTP/network failures retry at later hourly checks within the same budget and attempt limit. Exhausted or terminal failures remain unpublished for that week. Interrupted work can resume when its lease expires. Completed payloads are validated again against current collection eligibility before publication.

Inspect `Status`, `Attempts`, `TokensUsed`, `LastError`, and `CompletedAt` in the database. `TokensUsed` includes conservative reservations for requests whose actual usage is unknown. Logs record outcomes, tokens, exploration requests, and tool calls. Traces use `weekly_digest.generate`; existing token metrics use operation `weekly_digest`.

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
