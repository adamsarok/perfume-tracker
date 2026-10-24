# Agent task backlog

Complete T01 through T10 in numerical order, subject to the blocked-task rule below. This backlog supports a long autonomous run; elapsed time is not a completion criterion. Do not pad work to reach eight hours or invent additional features.

## Working rules

- Read applicable repository instructions and inspect the current implementation before changing it. Treat source code and executable configuration as authoritative when documentation disagrees.
- Work on a dedicated local branch. Preserve pre-existing user changes. Local commits are permitted after each verified task or coherent, verified subtask; include the task ID in the commit message. Never push, publish, merge into the default branch, amend pre-existing commits, or rewrite history.
- Record baseline build, lint, test, and coverage results. Distinguish pre-existing failures from regressions. Never mark an unverified requirement complete.
- Use statuses `pending`, `in progress`, `blocked`, and `done`. Update each task's completion notes with changes, commands, results, coverage figures, and remaining limitations. Record relevant commit hashes when available.
- Split large tasks into checkable subtasks under their existing task ID. Continue with the next unblocked task when a prerequisite cannot be resolved; document the blocker and return when possible.
- Use isolated local/test databases. Never migrate production databases, use production credentials, or call paid AI services for routine tests. Use deterministic fakes or recorded responses for external services.
- Do not weaken assertions, skip failing tests, narrow coverage scope to meet a target, add meaningless tests, or exclude difficult business logic. Refactor production code only when needed for behavior, correctness, or testability.
- Fix regressions introduced by the work. Run focused checks during development and the required full checks before marking each task done. Document setup and reproducible commands.
- Stop when all tasks are verified, or when all remaining work is blocked. Leave a concise handoff with outstanding work and evidence.

## Coverage contract

- T01 requires backend line coverage **at least 80%** and branch coverage **at least 80%**. T02 requires frontend statements, branches, functions, and lines **each at least 80%**. Percentages apply to aggregate eligible production code, not just files imported by tests.
- Include uncovered eligible files in reports. Backend scope is `PerfumeTracker.Server`; exclude EF-generated migrations and generated/build output. Do not include the eval harness or test assemblies in the backend denominator.
- Frontend scope is production `.ts` and `.tsx` code under `perfumetracker.client/src`; exclude declarations, generated code, test files, and test-only setup. Include application components, hooks, stores, services, and utilities, including reusable UI components with executable code.
- Any additional exclusions need a concrete justification recorded here; never use exclusions simply to reach a percentage. Preserve an auditable coverage configuration and machine-readable reports.
- Enforce thresholds in automated checks, and retain both targets after T03 adds authentication code. Do not rely on rounded summary values to pass a threshold.

## T01 — Raise backend coverage to 80%+

- **Status:** pending
- **Goal:** Protect backend behavior with meaningful tests and enforce the coverage contract.
- **Dependencies:** None.

### Requirements

- [ ] Establish a fresh baseline using the existing xUnit v3 test project and Coverlet MTP integration. Existing checked-in HTML coverage reports are not baseline evidence.
- [ ] Inspect the test fixture and CI database setup; document a reproducible isolated PostgreSQL/pgvector setup and required test configuration without recording secrets.
- [ ] Identify uncovered behavior and prioritize authentication/authorization, tenant isolation, soft deletion, validation, perfume/rating/usage operations, and background work failure handling before incidental paths.
- [ ] Add unit and integration tests as appropriate, covering successful operations, invalid inputs, boundaries, missing records, unauthorized access, and relevant failure/retry behavior. Assert observable outcomes and persisted state rather than implementation details.
- [ ] Keep database and background-service tests isolated and deterministic. Avoid real AI, object storage, or OAuth provider requests.
- [ ] Reach at least 80% line and branch coverage across the defined backend scope. Add a repeatable threshold check and wire it into existing CI using the installed coverage tooling.
- [ ] Document supported commands and correct directly relevant outdated test instructions.

### Verification / acceptance

- `dotnet build PerfumeTracker.sln --configuration Release` succeeds.
- The complete backend test suite passes against the isolated database. Start from the existing CI invocation: `dotnet test --configuration Release --coverlet --coverlet-output-format cobertura --coverlet-exclude-by-file "**/Migrations/*.cs"`; confirm supported arguments and record the final exact command and output location.
- A fresh machine-readable report and threshold check prove both backend metrics meet the target, including eligible untested files.
- Verify the threshold checker rejects an intentionally below-target report using an isolated fixture; do not leave intentional failures in the repository.

### Completion notes

- Baseline and final coverage: not measured.
- Changes / commands / evidence: pending.
- Blockers / limitations / commits: none recorded.

## T02 — Select frontend test tooling and reach 80%+ coverage

- **Status:** pending
- **Goal:** Establish maintainable React/TypeScript tests that enforce the frontend coverage contract.
- **Dependencies:** T01 for normal execution order; may proceed if T01 is externally blocked.

### Requirements

- [ ] Evaluate Vitest and Jest against the actual React 19, Vite, TypeScript, ESM, path-alias, and dependency versions. Record a short rationale here before implementing. Prefer Vitest with React Testing Library, user-event, jsdom, and V8 coverage if compatible; use Jest only if a demonstrated compatibility issue makes it a better fit.
- [ ] Install compatible test dependencies, update the lockfile, and configure the existing `@/` alias, browser environment, automatic cleanup, mocks, and coverage collection across all eligible source files.
- [ ] Add `npm test` for a single non-interactive run, `npm run test:watch` for development, and `npm run test:coverage` to produce reports and enforce all four thresholds.
- [ ] Test services and error handling, Zustand state, hooks, forms and validation, collection/rating/usage workflows, and important page/component loading, empty, success, and failure states.
- [ ] Use accessible queries and realistic user interactions. Mock network and SignalR boundaries, reset shared state between tests, and control timers where necessary. Avoid snapshot-only coverage and tests that merely mirror implementation.
- [ ] Reach at least 80% for statements, branches, functions, and lines across the defined frontend scope.
- [ ] Add frontend tests and coverage checks to CI alongside lint and build. Document actual commands; reconcile existing documentation that mentions frontend tests without configured scripts.

### Verification / acceptance

- From `perfumetracker.client`, `npm test`, `npm run test:coverage`, `npm run lint`, and `npm run build` succeed.
- Full coverage reports show all four aggregate metrics at or above 80%, including eligible files never imported by tests.
- Tests require no running backend, live provider accounts, or paid services. Run the full suite again to check for shared-state/order instability.
- Confirm below-target coverage makes the coverage command fail; restore normal configuration before committing.

### Completion notes

- Framework decision and rationale: pending evaluation.
- Baseline and final coverage: not measured.
- Changes / commands / evidence: pending.
- Blockers / limitations / commits: none recorded.

## T03 — Implement refresh token handling

- **Status:** pending
- **Goal:** Keep users signed in beyond the current 24-hour JWT lifetime while their refresh session remains valid, with explicit logout and bounded session duration.
- **Dependencies:** T01 and T02 test infrastructure; may investigate independently if either is blocked.

### Requirements

- [ ] Preserve cookie-based authentication. Keep access and refresh tokens out of localStorage, sessionStorage, client-readable cookies, response bodies, and logs. Issue refresh sessions wherever login currently issues JWT cookies, including password, demo, OAuth, and any registration flow that signs users in.
- [ ] Retain the existing configurable access-token lifetime for compatibility. Make the JWT cookie expiry follow that lifetime instead of the current hard-coded 24 hours. Add a configurable refresh-session lifetime, defaulting to an absolute 30 days from login; rotation must not extend this deadline.
- [ ] Persist a refresh-session model and EF migration. Generate cryptographically random opaque tokens, store only token hashes, and track user, session/family, expiry, revocation, and replacement information needed for rotation and replay detection. Keep separate logins/devices independent.
- [ ] Add `POST /api/identity/account/refresh`, usable with an expired/missing access JWT and a valid refresh cookie. On success, atomically consume the current refresh token, rotate it, issue a fresh access JWT with current user claims/roles, and return 204. Invalid, expired, revoked, reused, or deleted-user sessions return 401 and clear authentication cookies.
- [ ] Consume tokens transactionally so concurrent requests cannot both rotate one token. Reuse revokes that session family; it must not revoke unrelated device sessions. Do not allow refresh to bypass account lockout or other existing account eligibility restrictions.
- [ ] Use an HTTP-only refresh cookie with the narrowest path covering refresh and logout. Require Secure cookies in deployed HTTPS environments and explicitly support the documented local development setup. Set SameSite and credentialed CORS consistently with the supported deployment topology.
- [ ] Protect refresh and logout against CSRF using server-validated antiforgery tokens or a strict trusted-origin check with explicit rejection of untrusted/missing origins for browser session mutations. Verify behavior with the actual frontend/API origin configuration; do not treat SameSite=None or Axios XSRF option names alone as protection. Rate-limit refresh without blocking ordinary refresh traffic.
- [ ] Extend logout to revoke the current refresh family and clear cookies with matching paths/options, even after access-token expiry. Logout is idempotent and must not trigger automatic refresh. Document that an already-issued access JWT remains valid until expiry unless existing server-side revocation invalidates it.
- [ ] Add a single-flight refresh flow to the shared Axios client: eligible API 401s trigger at most one shared refresh request and retry each original request once after success. Exclude login, registration, OAuth, refresh, and logout requests; never refresh on 403 or loop on repeated 401s.
- [ ] On terminal refresh rejection, clear authenticated frontend state and use the existing login flow. Network/5xx refresh failures must not trigger an infinite loop or permanently erase a valid session; report a recoverable request failure. A later request may try again.
- [ ] Coordinate simultaneous refresh attempts across browser tabs so legitimate tabs do not consume the same token and trigger replay revocation. Recheck session state after acquiring coordination; document supported-browser requirements for the chosen mechanism.
- [ ] Verify session recovery on application reload with an expired access token and check authenticated SignalR reconnection after refresh. Prevent cached authenticated state or an in-flight refresh from restoring a session after logout.
- [ ] Document configuration, migration/rollback implications, cookie/CSRF requirements, and expiry behavior. Existing JWT-only sessions may require one fresh login to obtain a refresh session; do not create refresh sessions solely from expired JWTs. Apply migrations only to isolated local/test databases during this task.

### Verification / acceptance

- Backend tests cover all login issuance paths, successful refresh after access expiry, configured cookie expiry, rotation, absolute refresh expiry, missing/invalid/revoked tokens, replay, concurrent consumption, account eligibility, device isolation, CSRF rejection, logout after access expiry, and idempotent logout.
- Frontend tests cover one shared refresh for concurrent 401s, successful retry, retry limits, excluded endpoints, 403, invalid refresh, transient failures, reload recovery, logout races, and cross-tab coordination.
- Use a controllable clock to test a session beyond 24 hours and the 30-day deadline without real waits. Include a short-lifetime local browser smoke test covering refresh, reload, SignalR reconnect, and logout; record results and any unavailable checks.
- Fresh and upgraded isolated databases both support the new session schema. Review generated migration SQL for unintended changes.
- Full backend and frontend tests, both coverage gates, frontend lint/build, and the Release solution build pass after the authentication changes.

### Completion notes

- Changes / migration / configuration: pending.
- Commands / coverage / browser evidence: pending.
- Blockers / limitations / commits: none recorded.

## Additional tasks from repository review

The findings below come from source inspection, not runtime reproduction. Begin each bug fix with a regression test demonstrating the problem. Preserve the coverage contract after every task. Run focused regression tests, full affected suites, frontend lint/build, and the Release solution build as applicable. Record migrations, reproduction evidence, and exact verification commands in completion notes.

## T04 — Prevent invalid cross-user relationships and partial writes

- **Status:** pending
- **Priority / type:** High / data integrity and access control.
- **Goal:** Reject unauthorized references before persistence and prevent partial writes.
- **Dependencies:** T01 test infrastructure; follows T03 in normal execution order.
- **Evidence:** `Features/PerfumeRatings/Services/RatingService.cs` saves a rating before checking ownership of its perfume. Perfume add/update handlers accept tag IDs without explicit ownership validation. `Features/R2/GetImage.cs` signs a supplied object ID without checking ownership. Profile updates bind directly to the persisted entity.

### Requirements

- [ ] Validate that referenced perfumes, tags, recommendations, and image objects belong to the authenticated user and are active before changing state or signing URLs. Do not disclose another user's record existence through detailed errors.
- [ ] Ensure a rating belongs to the perfume identified by its route. Reject inconsistent route/body identifiers and mismatched rating deletion requests.
- [ ] Save rating changes, cached latest rating, and outbox messages atomically. Enqueue side effects only after a successful commit.
- [ ] Replace the profile-update entity payload with a settings DTO. Derive profile identity from authentication; prevent clients from changing IDs, deletion flags, timestamps, or navigation collections. Update frontend types and callers together.
- [ ] Preserve authorized image display and upload flows. Validate ownership against persisted image references before signing a download URL.

### Verification / acceptance

- Two-user integration tests prove foreign, deleted, and missing references cannot produce writes or signed URLs.
- Failed rating requests leave ratings, aggregate values, and outbox records unchanged, including simulated persistence failures.
- Route/body mismatch tests and profile overposting tests pass. Existing legitimate settings, rating, tag assignment, image upload, and image display workflows still work.

### Completion notes

- Reproduction / changes / API compatibility: pending.
- Commands / coverage / evidence: pending.
- Blockers / limitations / commits: none recorded.

## T05 — Make usage corrections reliable and expose bottle history

- **Status:** pending
- **Priority / type:** High / bug fix and useful feature.
- **Goal:** Keep bottle aggregates consistent with usage history and let users inspect and correct that history.
- **Dependencies:** T01 and T02 test infrastructure; T04 reference validation.
- **Evidence:** `Features/PerfumeEvents/DeletePerfumeEvent.cs` does not recalculate remaining volume and decrements wear count for any event type. The application maintains a bottle event ledger, but the UI primarily exposes a wear feed.

### Requirements

- [ ] Recalculate remaining volume, wear count, and last-worn date from active events after additions and deletions, atomically. Preserve signed volume deltas and the existing zero-floor convention; only wear events contribute to wear statistics.
- [ ] Protect aggregate updates against lost changes when event requests overlap. Keep repeated deletion from applying a second change.
- [ ] Add a paginated history section to each perfume showing event date, event type, signed volume change, and deletion controls with confirmation. Keep all queries and mutations tenant scoped. Expose the minimum additional event DTO fields/query interface required for this view.
- [ ] Disable Spray On while its request is pending. Refresh displayed bottle statistics and history after successful wear logging or deletion without discarding unsaved editor fields.
- [ ] Provide an explicitly invoked, repeatable reconciliation command for existing incorrect aggregates. Default to a preview of changes and require an explicit apply option; use only isolated local/test databases during this task.

### Verification / acceptance

- Tests cover deleting wears, initial-stock events, and adjustments; deleting the latest or only wear; repeated deletion; backdated events; concurrent writes; and nonnegative remaining volume.
- History tests cover pagination, ordering, confirmation, failed deletion, pending submission, and refreshed statistics.
- Reconciliation preview does not change data, and applying reconciliation twice produces no changes on the second run.

### Completion notes

- Reproduction / changes / reconciliation command: pending.
- Commands / coverage / browser evidence: pending.
- Blockers / limitations / commits: none recorded.

## T06 — Stop the perfume editor from discarding unsaved changes

- **Status:** pending
- **Priority / type:** High / bug fix and quality of life.
- **Goal:** Preserve editable drafts across related actions and prevent accidental navigation loss.
- **Dependencies:** T02 frontend tests; coordinate statistics refresh with T05.
- **Evidence:** In `perfumetracker.client/src/app/perfumes/perfume-edit-form.tsx`, changing tags or an uploaded image updates `perfume`, triggering `form.reset()` and erasing text edits. Global arrow handlers navigate even while users edit text. New-perfume tag selection requires an existing perfume object. Settings Save does not update the baseline used by Cancel.

### Requirements

- [ ] Keep editable fields and selected tags in one draft model. Reset only when loading a different record or accepting a successful save.
- [ ] Support selecting tags before inserting a new perfume. Preserve drafts when uploading images or accepting identified metadata; update only intended fields.
- [ ] Ignore navigation shortcuts in inputs, textareas, editable elements, and open dialogs.
- [ ] Confirm navigation away from a dirty draft, including previous/next navigation, and support browser unload protection. Canceling navigation preserves the draft.
- [ ] Prevent duplicate saves and keep failed submissions editable. Refresh from the saved result after success, including when the URL does not change.
- [ ] Update the Settings saved baseline after a successful save so Cancel restores the latest saved values. Preserve unsaved changes after failed saves.

### Verification / acceptance

- Component tests cover typing followed by tag changes, upload, identification, failed save, successful save, new-record tags, keyboard shortcuts, and discard confirmation.
- Confirm image and wear-statistic refreshes do not reset edited fields. Settings Save/Edit/Cancel restores the latest successful save.
- Local browser smoke checks cover editing an existing perfume, adding one with tags, and navigating away from a dirty draft.

### Completion notes

- Reproduction / draft-state changes: pending.
- Commands / coverage / browser evidence: pending.
- Blockers / limitations / commits: none recorded.

## T07 — Make collection search safe and filters dependable

- **Status:** pending
- **Priority / type:** Medium / bug fix and quality of life.
- **Goal:** Handle ordinary search text reliably and preserve the user's collection view.
- **Dependencies:** T01 and T02 tests; T04 tenant-safety behavior.
- **Evidence:** `Features/Perfumes/GetPerfumes.cs` assembles raw PostgreSQL tsquery syntax from user input and checks capacity (`Ml`) rather than remaining volume (`MlLeft`) for positive-stock filtering. Collection view filters are transient component state.

### Requirements

- [ ] Treat search input as ordinary text using safely normalized prefix terms with AND semantics. Preserve tag-name matching and existing endpoints.
- [ ] Handle punctuation, apostrophes, accents, whitespace, and empty input without database syntax errors. This is a query-language robustness fix; do not characterize parameterized SQL as SQL injection.
- [ ] Correct positive-stock filtering to use remaining volume.
- [ ] Persist search text, selected category, and selected tag IDs in URL search parameters. Refresh and back navigation must restore the view; unknown/deleted tag IDs must not break rendering.
- [ ] Provide clear/reset controls and distinct loading, request-error/retry, empty-collection, and no-match states.
- [ ] Prevent stale responses from replacing newer results. Maintain current favorites, buy-list, untagged, and all-selected-tags semantics.

### Verification / acceptance

- PostgreSQL integration tests cover reserved punctuation, apostrophes, accents, multiple terms, empty input, tag matching, and positive capacity with zero remaining stock.
- Frontend tests cover URL restoration, back navigation, clear/reset, unknown tag IDs, failed requests, and responses arriving out of order.
- Existing API callers and collection category behavior remain compatible.

### Completion notes

- Reproduction / search and URL changes: pending.
- Commands / coverage / browser evidence: pending.
- Blockers / limitations / commits: none recorded.

## T08 — Correct mission and streak progression

- **Status:** pending
- **Priority / type:** High / user-visible correctness.
- **Goal:** Make progress reflect qualifying wear events and local calendar days without duplicate rewards.
- **Dependencies:** T04 ownership rules and T05 event lifecycle; T01/T02 tests.
- **Evidence:** `Features/Missions/ProgressMissions.cs` checks for an existing wear after the triggering event has already been persisted, so the different-perfumes check finds that event itself. Wear-note progression checks for any tag rather than `RequiredId`. `Features/Streaks/ProgressStreaks.cs` processes every event type and uses processing time and integer-hour offsets.

### Requirements

- [ ] Base wear mission eligibility on the persisted triggering event and its event date, bounded by mission start (inclusive) and end (exclusive). Ignore non-wear or deleted triggering events.
- [ ] Exclude the triggering event when examining prior wears. Honor the mission's required note and exclude deleted records/relationships from eligibility queries.
- [ ] Make progression idempotent for notification retries, including recommendation-related missions. Prevent duplicate completion rewards and persist deduplication with the corresponding state change.
- [ ] Progress streaks only from active wear events using local calendar dates converted through the user's timezone. Support DST and fractional-hour offsets; do not apply a single current offset to historical timestamps.
- [ ] Add a timezone selector to Settings, retaining UTC as the default. Validate supported timezone identifiers and preserve unrelated settings.
- [ ] Recalculate affected wear-based progress after wear deletion. Recompute incomplete mission progress; keep completed mission rewards historical. Recalculate streaks from active wear dates while preserving configured protection days.
- [ ] Keep progress updates and completion notifications consistent with committed state. Document migrations or reconciliation required for existing state; use only local/test databases during verification.

### Verification / acceptance

- Tests use real persisted triggering events, not notifications for nonexistent events.
- Cover first/repeated perfume use, matching/unmatched notes, mission-window boundaries, replayed notifications, deletions, backdated wears, and duplicate reward prevention.
- Cover non-wear events, same local day, local midnight, DST transitions, a fractional-offset timezone, and timezone changes. Use a controllable clock rather than delays.
- Verify frontend timezone persistence and accurate refreshed progress after corrections.

### Completion notes

- Reproduction / progression policy / migration: pending.
- Commands / coverage / evidence: pending.
- Blockers / limitations / commits: none recorded.

## T09 — Keep semantic recommendations synchronized with active data

- **Status:** pending
- **Priority / type:** Medium / reliability and recommendation quality.
- **Goal:** Keep semantic documents current without reintroducing deleted data or wasting embedding requests.
- **Dependencies:** T04 reference safety, T05 event corrections, and existing outbox infrastructure.
- **Evidence:** `Features/Embedding/UpdateEmbeddings.cs` already handles outbox notifications but disables all query filters when loading related records. Tag edits/deletions and wear changes do not consistently request refreshes. The embedding background service backfills missing documents rather than stale ones.

### Requirements

- [ ] Extend the existing outbox mechanism rather than introduce a second update pipeline.
- [ ] Build embedding text only from active perfumes and active ratings, events, tags, and tag relationships, with explicit tenant scope when background processing bypasses filters.
- [ ] Queue refreshes for changes affecting embedding text, including tag edits/deletions, wear additions/deletions, and rating changes. Tag changes must refresh affected active perfumes.
- [ ] Skip unchanged text. Prevent concurrent refreshes from overwriting newer content or inserting duplicate documents, and prevent deleted perfumes from becoming recommendation candidates.
- [ ] Add a bounded reconciliation operation for missing/stale documents, with observable retry failures. Document invocation and recovery; avoid indefinite retry loops or unbounded model calls.
- [ ] Use a fake encoder for all automated verification; do not call paid services.

### Verification / acceptance

- Tests prove deleted content disappears, relevant changes refresh documents, unchanged content avoids regeneration, retries recover, and concurrent processing leaves one current document.
- Verify tenant isolation, deletion during refresh, tag fan-out, reconciliation batching, and operation with embeddings disabled.
- Existing recommendation/eval checks pass using deterministic fakes or recordings.

### Completion notes

- Reproduction / outbox changes / reconciliation command: pending.
- Commands / coverage / fake-encoder evidence: pending.
- Blockers / limitations / commits: none recorded.

## T10 — Export collection data for backup and analysis

- **Status:** pending
- **Priority / type:** Medium / new feature.
- **Goal:** Let users download their collection and personal tracking history in usable, documented formats.
- **Dependencies:** T04 tenant safety and T05 consistent event aggregates; T01/T02 tests.
- **Evidence:** Repository review found no collection export endpoint or Settings export action despite persisted collection, rating, event, and preference data.

### Requirements

- [ ] Add authenticated collection-CSV and personal-data-JSON download endpoints and corresponding Settings actions. Export all eligible records independent of UI filters or pagination.
- [ ] CSV includes perfume ID, house, name, family, parfumeur, capacity, remaining volume, latest rating, wear count, last-worn UTC timestamp, and tags.
- [ ] Versioned JSON includes active perfumes, tags and relationships, ratings, usage events, and user preferences, with a UTC export timestamp. Preserve identifiers for reference and document relationships and field meanings.
- [ ] Export only the authenticated user's active data. Exclude credentials, authentication tokens, generated embeddings, signed image URLs, and image binaries. Do not include references to excluded/deleted parent records.
- [ ] Use consistent database reads, Unicode-safe output, correct CSV escaping, spreadsheet-formula protection for textual cells, and attachment download headers.
- [ ] Provide pending, error, and retry states in Settings without duplicate download requests.
- [ ] Document format versions, included data, and limitations. Image-file export and import/restore are outside this task; do not present the JSON download as a complete restorable backup.

### Verification / acceptance

- Integration tests prove tenant isolation, relational consistency, consistent reads, and completeness beyond the collection UI's current filters/page.
- Cover empty exports, Unicode, quoted/multiline comments, formula-like text, UTC timestamps, and exclusion of secrets and signed URLs.
- Frontend tests cover both downloads, pending controls, errors, and retry. A local browser smoke check confirms usable CSV and JSON attachments.

### Completion notes

- Endpoint / format / documentation changes: pending.
- Commands / coverage / export and browser evidence: pending.
- Blockers / limitations / commits: none recorded.
