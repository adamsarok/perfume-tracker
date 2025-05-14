namespace PerfumeTracker.Server.Features.UserProfiles;

public class UpsertUserProfile(PerfumeTrackerContext context, GetUserProfile getUserProfile) {
	public async Task<UserProfile> HandleAsync(UserProfile userProfile) {
		var found = await getUserProfile.HandleAsync();
		if (found != null) {
			context.Entry(found).CurrentValues.SetValues(userProfile);
		} else {
			await context.UserProfiles.AddAsync(userProfile);
		}
		await context.SaveChangesAsync();
		return found ?? userProfile;
	}
}
public class UpsertUserProfilesModule : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPut("/api/user-profiles", async (UserProfile userProfile, UpsertUserProfile upsertUserProfile) =>
			await upsertUserProfile.HandleAsync(userProfile))
			.WithTags("UserProfiles")
			.WithName("UpsertUserProfile");
	}
}
