# Smoke fixtures

Hand-written recordings used to verify the harness end to end without an OpenAI key:

```bash
dotnet run --project PerfumeTracker.Evals -- --mode replay --recordings Fixtures/smoke --filter small-talk
```

The directory name (`613a2bc377fa157d`) is the prompt + tool fingerprint the recordings were taken
against. If the system prompt or a tool schema changes, the fingerprint changes and the harness
reports that recordings are missing - re-record with `--mode record`, then either replace these
fixtures or add the new fingerprint directory.

The responses are synthetic (no model was called); use `Recordings/` for real runs.
