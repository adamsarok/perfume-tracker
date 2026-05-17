using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace PerfumeTracker.Server.Startup;

public static class Diagnostics {
	public const string ActivitySourceName = "PerfumeTracker.Server";
	public const string MeterName = "PerfumeTracker.Server";
	public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
	public static readonly Meter Meter = new(MeterName);
	public static readonly Counter<long> MissionsCompletedCounter = Meter.CreateCounter<long>(
		"missions.completed",
		unit: "{mission}",
		description: "Number of missions completed.");
}
