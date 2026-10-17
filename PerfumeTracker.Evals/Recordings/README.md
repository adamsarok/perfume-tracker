# Recordings

Recorded model responses, laid out as `Recordings/<prompt-fingerprint>/<case-id>/<call-index>.json`.

The fingerprint is a hash of the system prompt + tool schemas (see `EvalHost.ComputePromptFingerprintAsync`),
so changing the prompt, a tool description or a tool schema changes the directory and the harness will
report missing recordings instead of silently reusing stale ones.

Record (needs an API key, spends tokens):

```bash
dotnet run --project PerfumeTracker.Evals -- --mode record --model <real-model> --api-key <key>
```

Then the key-free regression run:

```bash
dotnet run --project PerfumeTracker.Evals -- --mode replay --baseline results/baseline/results.json --out results/current
```

Commit the recordings when they are stable: they are what makes the PR-time replay check possible.
`../Fixtures/smoke` holds hand-written recordings used to verify the harness itself without a key.
