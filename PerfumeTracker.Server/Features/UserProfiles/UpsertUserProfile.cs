
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.UserProfiles;
public record class UpsertUserProfileCommand(UserProfile UserProfile) : ICommand<UserProfile>;
public class UpsertUserProfileValidator : AbstractValidator<UpsertUserProfileCommand> {
	public UpsertUserProfileValidator() {
		RuleFor(x => x.UserProfile).NotNull();
		RuleFor(x => x.UserProfile.DayFilter).InclusiveBetween(0, 365);
		RuleFor(x => x.UserProfile.Id).NotNull();
		RuleFor(x => x.UserProfile.MinimumRating).InclusiveBetween(0, 10);
		RuleFor(x => x.UserProfile.SprayAmountFullSizeMl).InclusiveBetween(0, 10);
		RuleFor(x => x.UserProfile.SprayAmountSamplesMl).InclusiveBetween(0, 10);
		RuleFor(x => x.UserProfile.Timezone).Must(x => {
			if (string.IsNullOrWhiteSpace(x)) return true;
			try {
				TimeZoneInfo tzi2 = TimeZoneInfo.FindSystemTimeZoneById(x);
			} catch {
				return false;
			}
			return true;
		});
	}
}
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