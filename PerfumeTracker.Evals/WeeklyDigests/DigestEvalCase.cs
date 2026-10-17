using System.Text.Json;
using PerfumeTracker.Server.Features.WeeklyDigests;

namespace PerfumeTracker.Evals.WeeklyDigests;

public sealed class DigestEvalCase {
	public string Id { get; set; } = "";
	public string Fixture { get; set; } = "diverse";
	public DateTimeOffset Now { get; set; } = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);
	public string Rubric { get; set; } = "";
	public bool Extended { get; set; }
	public WeeklyDigestOptions Budget => Extended ? new() : new() {
		MaxExplorationRequests = 3, MaxTokens = 60_000, FinalizationReserve = 20_000, MaxOutputTokens = 10_000
	};

	public static async Task<List<DigestEvalCase>> LoadAsync(string path, string? filter, CancellationToken ct) {
		var cases = JsonSerializer.Deserialize<List<DigestEvalCase>>(await File.ReadAllTextAsync(path, ct), DigestJson.Options)
			?? throw new InvalidDataException("Empty digest dataset.");
		if (cases.Count == 0 || cases.Any(c => string.IsNullOrWhiteSpace(c.Id) || string.IsNullOrWhiteSpace(c.Rubric)
			|| !DigestEvalFixtures.Names.Contains(c.Fixture) || c.Now.Offset != TimeSpan.Zero)
			|| cases.Select(c => c.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != cases.Count)
			throw new InvalidDataException("Digest cases need unique IDs, known fixtures, UTC time, and rubrics.");
		cases = cases.Where(c => c.Extended ? string.Equals(c.Id, filter, StringComparison.OrdinalIgnoreCase)
			: string.IsNullOrWhiteSpace(filter) || c.Id.Contains(filter, StringComparison.OrdinalIgnoreCase)
				|| c.Fixture.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
		if (cases.Count == 0) throw new InvalidDataException("No digest cases matched.");
		return cases;
	}
}

public sealed class FrozenDigestTime(DateTimeOffset now) : TimeProvider {
	public override DateTimeOffset GetUtcNow() => now;
}

public sealed record DigestCandidateTrace(string Category, int Offset, int Count, string? Search, DigestCandidatePage Result);
public sealed record DigestModelTrace(int Index, string Phase, string RequestHash, JsonElement Request, JsonElement Response);
public sealed record DigestSaveSnapshot(long TokensUsed, string Checkpoint);

public sealed class DigestEvalTrajectory {
	public string Fingerprint { get; set; } = "";
	public DigestPayload? Payload { get; set; }
	public DigestCheckpoint? Checkpoint { get; set; }
	public JsonElement Context { get; set; } = JsonSerializer.SerializeToElement(new { });
	public List<DigestCandidateTrace> CandidateCalls { get; set; } = [];
	public List<DigestModelTrace> ModelCalls { get; set; } = [];
	public List<DigestSaveSnapshot> Saves { get; set; } = [];
	public long TokensUsed { get; set; }
	public long DurationMs { get; set; }
	public string? Error { get; set; }
	public int InputTokens => ModelCalls.Sum(c => Usage(c.Response, "input_tokens"));
	public int OutputTokens => ModelCalls.Sum(c => Usage(c.Response, "output_tokens"));
	private static int Usage(JsonElement response, string name) => response.TryGetProperty("usage", out var usage)
		&& usage.TryGetProperty(name, out var value) && value.TryGetInt32(out var count) ? count : 0;
}
