namespace PerfumeTracker.Server.Features.Common.Services;

public interface IXPService {
	public record StreakResult(Guid UserId, decimal XpMultiplier, int StreakDays);
	public record XPResult(Guid UserId, long Xp, long XpLastLevel, long XpNextLevel, int Level, decimal XpMultiplier, int StreakLength);
	Task<StreakResult> GetUserStreak(Guid userId, CancellationToken token);
	decimal GetXPMultiplier(int streakDays);
	Task<XPResult> GetUserXP(Guid userId, CancellationToken cancellationToken);
}
