using Microsoft.AspNetCore.Identity;

namespace PerfumeTracker.Server.Features.Auth;
public interface ICreateUser {
	Task<PerfumeIdentityUser?> Create(string? userName, string? password, string? role, string? email, bool isEmailConfirmed = false);
}
public class CreateUser(ILogger<CreateUser> logger, UserManager<PerfumeIdentityUser> userManager, PerfumeTrackerContext context) : ICreateUser {
	public async Task<PerfumeIdentityUser?> Create(string? userName, string? password, string? role, string? email, bool isEmailConfirmed = false) {
		if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentNullException(nameof(userName));
		if (string.IsNullOrWhiteSpace(password)) throw new ArgumentNullException(nameof(password));
		if (string.IsNullOrWhiteSpace(role)) throw new ArgumentNullException(nameof(role));
		if (string.IsNullOrWhiteSpace(email)) throw new ArgumentNullException(nameof(email));
		var user = await userManager.FindByEmailAsync(email);
		if (user == null) {
			user = new PerfumeIdentityUser {
				UserName = userName,
				Email = email,
				EmailConfirmed = isEmailConfirmed
			};
			var result = await userManager.CreateAsync(user, password);
			if (!result.Succeeded) {
				throw new InvalidOperationException($"Failed to create {role} user: " + string.Join(", ", result.Errors.Select(x => x.Description)));
			}
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
