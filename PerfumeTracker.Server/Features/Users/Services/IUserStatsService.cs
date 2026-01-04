namespace PerfumeTracker.Server.Features.Users.Services;

public interface IUserStatsService {
	Task<UserStatsResponse> GetUserStats(CancellationToken cancellationToken);
}
