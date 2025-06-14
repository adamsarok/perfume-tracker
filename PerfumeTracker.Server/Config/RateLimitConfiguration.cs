﻿namespace PerfumeTracker.Server.Config;
public class RateLimitConfiguration {
	public int General { get; init; }
	public int Auth { get; init; }
	public int Upload { get; init; }
	public RateLimitConfiguration(IConfiguration configuration) {
		General = configuration.GetValue<int?>("RateLimits:General") ?? throw new InvalidOperationException("R2 ExpirationHours is not configured");
		Auth = configuration.GetValue<int?>("RateLimits:Auth") ?? throw new InvalidOperationException("R2 MaxFileSizeKb is not configured");
		Upload = configuration.GetValue<int?>("RateLimits:Upload") ?? throw new InvalidOperationException("R2 MaxFileSizeKb is not configured");
	}
}