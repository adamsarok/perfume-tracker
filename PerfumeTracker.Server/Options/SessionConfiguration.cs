using System.ComponentModel.DataAnnotations;

namespace PerfumeTracker.Server.Options;

public class SessionConfiguration {
	[Range(1, 168)]
	public int IdleTimeoutHours { get; set; } = 24;

	[Range(1, 4080)]
	public int AbsoluteLifetimeHours { get; set; } = 336;

	public TimeSpan IdleTimeout => TimeSpan.FromHours(IdleTimeoutHours);
	public TimeSpan AbsoluteLifetime => TimeSpan.FromHours(AbsoluteLifetimeHours);
}
