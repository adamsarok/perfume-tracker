namespace PerfumeTracker.Server.Features.Common;

public class XPService(PerfumeTrackerContext context) {
	public const float MaxMultiplier = 3.0f;
	public record StreakResult(double XpMultiplier, int StreakDays);
	public async Task<StreakResult> GetXPMultiplier(CancellationToken token, Guid userId) {
		var streak = await context.UserStreaks.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.UserId == userId && x.StreakEndAt == null);
		if (streak != null) return new StreakResult(GetXPMultiplier(streak.Progress), streak.Progress);
		return new StreakResult(1f, 0);
	}
	public float GetXPMultiplier(int streakDays) {
		if (streakDays <= 10) return 1f + streakDays * 0.03f;
		if (streakDays <= 50) return 1f + 0.3f + (streakDays - 10) * 0.02f;
		return Math.Min(MaxMultiplier, 2.1f + (streakDays - 50) * 0.01f);
	}
}
