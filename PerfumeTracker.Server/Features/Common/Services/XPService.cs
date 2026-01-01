using static PerfumeTracker.Server.Features.Common.Services.IXPService;

namespace PerfumeTracker.Server.Features.Common.Services;

public class XPService(PerfumeTrackerContext context) : IXPService {
	public const decimal MaxMultiplier = 3.0m;

	public async Task<XPResult> GetUserXP(Guid userId, CancellationToken cancellationToken) {
		var xp = await context.UserMissions
			.IgnoreQueryFilters()
			.Where(x => !x.IsDeleted && x.UserId == userId && x.CompletedAt != null)
			.SumAsync(x => x.XP_Awarded, cancellationToken);
		var level = Levels.GetLevels()
			.FirstOrDefault(x => x.MinXP <= xp && x.MaxXP >= xp);
		var streak = await GetUserStreak(userId, cancellationToken);
		return new XPResult(
			UserId: userId,
			Xp: xp,
			XpLastLevel: level?.MinXP ?? 0,
			XpNextLevel: level?.MaxXP + 1 ?? 0,
			Level: level?.LevelNum ?? 1,
			Math.Round(streak.XpMultiplier, 2),
			streak.StreakDays
		);
	}

	public async Task<StreakResult> GetUserStreak(Guid userId, CancellationToken token) {
		var streak = await context
			.UserStreaks
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(x => !x.IsDeleted && x.UserId == userId && x.StreakEndAt == null);
		if (streak != null) return new StreakResult(userId, GetXPMultiplier(streak.Progress), streak.Progress);
		return new StreakResult(userId, 1m, 0);
	}
	public decimal GetXPMultiplier(int streakDays) {
		if (streakDays <= 10) return 1m + streakDays * 0.03m;
		if (streakDays <= 50) return 1m + 0.3m + (streakDays - 10) * 0.02m;
		return Math.Min(MaxMultiplier, 2.1m + (streakDays - 50) * 0.01m);
	}
}
public class Levels {
	private static readonly long[] BreakPoints = [100, 500, 1000, 2500, 5000, 10000, 20000, 40000, 80000, 160000, 250000, 350000, 500000];
	public record Level(int LevelNum, long MinXP, long MaxXP);
	public static List<Level> GetLevels() {
		var levels = new List<Level>();
		foreach (var (breakPoint, index) in BreakPoints.Select((value, index) => (value, index))) {
			var levelNum = index + 1;
			var minXP = index == 0 ? 0 : BreakPoints[index - 1];
			var maxXP = breakPoint - 1;
			levels.Add(new Level(levelNum, minXP, maxXP));
		}
		return levels;
	}
}
