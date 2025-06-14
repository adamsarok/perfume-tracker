namespace PerfumeTracker.Server.Config;
public class RateLimitConfiguration {
	public int General { get; init; }
	public int Auth { get; init; }
	public int Upload { get; init; }
	public RateLimitConfiguration(IConfiguration configuration) {
		General = configuration.GetValue<int?>("RateLimits:General") ?? throw new ConfigEmptyException("RateLimits General is not configured");
		Auth = configuration.GetValue<int?>("RateLimits:Auth") ?? throw new ConfigEmptyException("RateLimits Auth is not configured");
		Upload = configuration.GetValue<int?>("RateLimits:Upload") ?? throw new ConfigEmptyException("RateLimits Upload is not configured");
		if (General <= 0 || Auth <= 0 || Upload <= 0) throw new InvalidOperationException("Rate limit values must be positive integers");
	}
}