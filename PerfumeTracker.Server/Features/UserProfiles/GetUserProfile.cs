
using PerfumeTracker.Server.Services.Auth;
using PerfumeTracker.Server.Services.Common;

namespace PerfumeTracker.Server.Features.UserProfiles;

public record GetUserProfileQuery() : IQuery<UserProfile>;

public class GetUserProfilesEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/user-profiles", async (ISender sender, CancellationToken cancellationToken) =>
			await sender.Send(new GetUserProfileQuery(), cancellationToken))
			.WithTags("UserProfiles")
			.WithName("GetUserProfile")
			.RequireAuthorization(Policies.READ);
	}
}

public class GetUserProfileHandler(IUserProfileService userProfileService) : IQueryHandler<GetUserProfileQuery, UserProfile> {
	public async Task<UserProfile> Handle(GetUserProfileQuery request, CancellationToken cancellationToken) {
		return await userProfileService.GetCurrentUserProfile(cancellationToken);
	}
}
