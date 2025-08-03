
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.UserProfiles;
public record GetUserProfileQuery() : IQuery<UserProfile>;

public class GetUserProfilesEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/user-profiles", async (ISender sender) =>
			await sender.Send(new GetUserProfileQuery()))
			.WithTags("UserProfiles")
			.WithName("GetUserProfile")
			.RequireAuthorization(Policies.READ);
	}
}

public class GetUserProfileHandler(PerfumeTrackerContext context) : IQueryHandler<GetUserProfileQuery, UserProfile> {
	public async Task<UserProfile> Handle(GetUserProfileQuery request, CancellationToken cancellationToken) {
		return await context.UserProfiles.FirstAsync();
	}
}
