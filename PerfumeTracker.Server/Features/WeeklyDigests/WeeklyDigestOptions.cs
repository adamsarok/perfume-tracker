namespace PerfumeTracker.Server.Features.WeeklyDigests;

public class WeeklyDigestOptions {
	public long MaxTokens { get; set; } = 150_000;
	public int FinalizationReserve { get; set; } = 40_000;
	public int MaxOutputTokens { get; set; } = 25_000;
	public int FinalInputTokens { get; set; } = 15_000;
	public int MaxExplorationRequests { get; set; } = 50;
	public int MaxAttempts { get; set; } = 3;
	public string? Model { get; set; }
	public bool IsValid() => MaxTokens > FinalizationReserve && FinalizationReserve >= (long)FinalInputTokens + MaxOutputTokens
		&& FinalInputTokens > 0 && MaxOutputTokens >= 1024 && MaxExplorationRequests > 0 && MaxAttempts > 0
		&& (Model == null || !string.IsNullOrWhiteSpace(Model));
}
