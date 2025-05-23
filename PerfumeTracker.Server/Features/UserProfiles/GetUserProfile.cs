namespace PerfumeTracker.Server.Features.UserProfiles;

public class GetUserProfile(PerfumeTrackerContext context) {
	public async Task<UserProfileNew?> HandleAsync() {
		return await context.UserProfiles.FirstOrDefaultAsync();
	}
}

public class GetUserProfilesModule : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/user-profiles", async (GetUserProfile getUserProfile) =>
			await getUserProfile.HandleAsync())
			.WithTags("UserProfiles")
			.WithName("GetUserProfile");
	}
}
