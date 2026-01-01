using PerfumeTracker.Server.Features.Common.Services;
using PerfumeTracker.Server.Services.Auth;

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

public class GetCurrentUserXPHandler(PerfumeTrackerContext context, IXPService xPService) : IQueryHandler<UserXPQuery, UserXPResponse> {
	public async Task<UserXPResponse> Handle(UserXPQuery request, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		var result = await xPService.GetUserXP(userId, cancellationToken);
		return result.Adapt<UserXPResponse>();
	}
}