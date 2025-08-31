namespace PerfumeTracker.Server.Features.Users;
public interface ICreateUser {
	Task<PerfumeIdentityUser?> Create(string userName, string password, string role, string email, bool isEmailConfirmed = false);
}
public class CreateUser(ILogger<CreateUser> logger, UserManager<PerfumeIdentityUser> userManager, PerfumeTrackerContext context) : ICreateUser {
	public async Task<PerfumeIdentityUser?> Create(string userName, string password, string role, string email, bool isEmailConfirmed = false) {
		var user = await userManager.FindByEmailAsync(email);
		if (user == null) {
			user = new PerfumeIdentityUser {
				UserName = userName,
				Email = email,
				EmailConfirmed = isEmailConfirmed
			};
			var result = await userManager.CreateAsync(user, password);
			if (!result.Succeeded) throw new InvalidOperationException($"Failed to create {role} user: " + string.Join(", ", result.Errors.Select(x => x.Description)));
			await userManager.AddToRoleAsync(user, role);
			context.UserProfiles.Add(new UserProfile(user.Id, userName, email));
			await context.SaveChangesAsync();
			return user;
		} else {
			logger.LogError("User with email {Email} already exists", email); //return ok to prevent email renumeration
			return null;
		}
	}
}
