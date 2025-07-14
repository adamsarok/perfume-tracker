namespace PerfumeTracker.Server.Features.Common;

public class XPService(PerfumeTrackerContext context) {
	public const decimal MaxMultiplier = 3.0m;
	public record StreakResult(decimal XpMultiplier, int StreakDays);
	public async Task<StreakResult> GetXPMultiplier(CancellationToken token, Guid userId) {
		var streak = await context.UserStreaks.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.UserId == userId && x.StreakEndAt == null);
		if (streak != null) return new StreakResult(GetXPMultiplier(streak.Progress), streak.Progress);
		return new StreakResult(1m, 0);
	}
	public decimal GetXPMultiplier(int streakDays) {
		if (streakDays <= 10) return 1m + streakDays * 0.03m;
		if (streakDays <= 50) return 1m + 0.3m + (streakDays - 10) * 0.02m;
		return Math.Min(MaxMultiplier, 2.1m + (streakDays - 50) * 0.01m);
	}
}
