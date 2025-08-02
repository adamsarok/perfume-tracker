
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.UserProfiles;
public record class UpsertUserProfileCommand(UserProfile UserProfile) : ICommand<UserProfile>;

public class UpsertUserProfilesModule : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPut("/api/user-profiles", async (UserProfile userProfile, ISender sender) =>
			await sender.Send(new UpsertUserProfileCommand(userProfile)))
			.WithTags("UserProfiles")
			.WithName("UpsertUserProfile")
			.RequireAuthorization(Policies.WRITE);
	}
}

public class UpsertUserProfileHandler(PerfumeTrackerContext context) : ICommandHandler<UpsertUserProfileCommand, UserProfile> {
	public async Task<UserProfile> Handle(UpsertUserProfileCommand request, CancellationToken cancellationToken) {
		var found = await context.UserProfiles.FirstOrDefaultAsync();
		if (found != null) {
			context.Entry(found).CurrentValues.SetValues(request.UserProfile);
		} else {
			await context.UserProfiles.AddAsync(request.UserProfile);
		}
		await context.SaveChangesAsync();
		return found ?? request.UserProfile;
	}
}