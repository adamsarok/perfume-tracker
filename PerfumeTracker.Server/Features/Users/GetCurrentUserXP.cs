using PerfumeTracker.Server.Services.Auth;
using PerfumeTracker.Server.Services.Common;

namespace PerfumeTracker.Server.Features.Users;

public record UserXPQuery() : IQuery<UserXPResponse>;
public record UserXPResponse(long Xp, long XpLastLevel, long XpNextLevel, int Level, decimal XpMultiplier, int StreakLength);
public class GetCurrentUserXPEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/users/xp", (ISender sender, CancellationToken cancellationToken) => {
			return sender.Send(new UserXPQuery(), cancellationToken);
		}).WithTags("Users")
			.WithName("GetCurrentUserXP")
			.RequireAuthorization(Policies.READ);
	}
}

public class Levels {
	private static readonly long[] BreakPoints = [100, 500, 1000, 2500, 5000, 10000];
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

public class GetCurrentUserXPHandler(PerfumeTrackerContext context, XPService xPService) : IQueryHandler<UserXPQuery, UserXPResponse> {
	public async Task<UserXPResponse> Handle(UserXPQuery request, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		var xp = await context.UserMissions
			.Where(x => x.CompletedAt != null)
			.SumAsync(x => x.XP_Awarded, cancellationToken);
		var level = Levels.GetLevels()
			.FirstOrDefault(x => x.MinXP <= xp && x.MaxXP >= xp);
		var xpMultiplier = await xPService.GetXPMultiplier(cancellationToken, userId);
		return new UserXPResponse(
			Xp: xp,
			XpLastLevel: level?.MinXP ?? 0,
			XpNextLevel: level?.MaxXP + 1 ?? 0,
			Level: level?.LevelNum ?? 1,
			Math.Round(xpMultiplier.XpMultiplier, 2),
			xpMultiplier.StreakDays
		);
	}
}