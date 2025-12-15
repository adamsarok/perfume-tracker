using System.ComponentModel.DataAnnotations;

namespace PerfumeTracker.Server.Options;

public class RateLimitConfiguration {
	[Range(1, int.MaxValue)]
	public int General { get; init; }

	[Range(1, int.MaxValue)]
	public int Auth { get; init; }

	[Range(1, int.MaxValue)]
	public int Upload { get; init; }
}