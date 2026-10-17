namespace PerfumeTracker.Server.Features.WeeklyDigests;

public class WeeklyDigestOptions {
	public long MaxTokens { get; set; } = 150_000;
	public int FinalizationReserve { get; set; } = 40_000;
	public int MaxOutputTokens { get; set; } = 25_000;
	public int MaxExplorationRequests { get; set; } = 50;
	public int MaxAttempts { get; set; } = 3;
	public string? Model { get; set; }
	public int MaxGenerationRetries { get; set; } = 3;
	public double RetryBaseDelaySeconds { get; set; } = 2;
	public double RetryMaxDelaySeconds { get; set; } = 30;
	public bool IsValid() => MaxTokens > FinalizationReserve && FinalizationReserve >= MaxOutputTokens
		&& MaxOutputTokens >= 1024 && MaxExplorationRequests > 0 && MaxAttempts > 0
		&& (Model == null || !string.IsNullOrWhiteSpace(Model))
		&& MaxGenerationRetries is >= 0 and <= 10
		&& double.IsFinite(RetryBaseDelaySeconds) && double.IsFinite(RetryMaxDelaySeconds)
		&& RetryBaseDelaySeconds >= 0 && RetryMaxDelaySeconds >= RetryBaseDelaySeconds && RetryMaxDelaySeconds <= 86_400;
}
