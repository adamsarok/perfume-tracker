using System.Diagnostics;

namespace PerfumeTracker.Server.Startup;

public static class Diagnostics {
	public const string ActivitySourceName = "PerfumeTracker.Server";
	public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
