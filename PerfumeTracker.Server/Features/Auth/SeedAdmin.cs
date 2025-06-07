using Microsoft.AspNetCore.Identity;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace PerfumeTracker.Server.Features.Auth;
public static class SeedAdmin {
	public const string USERNAME = "admin";
	public const string PASSWORD = "admin";
	public const string EMAIL = "admin@example.com";
	public static async Task SeedAdminAsync(IServiceProvider serviceProvider, PerfumeTrackerContext context) {
		var userManager = serviceProvider.GetRequiredService<UserManager<PerfumeIdentityUser>>();
		var user = await userManager.FindByNameAsync(USERNAME);
		if (user == null) {
			user = new PerfumeIdentityUser {
				UserName = USERNAME,
				Email = EMAIL,
				EmailConfirmed = true
			};
			var result = await userManager.CreateAsync(user, PASSWORD);
			if (!result.Succeeded) {
				throw new InvalidOperationException("Failed to create admin user: " + string.Join(", ", result.Errors));
			}
			await userManager.AddToRoleAsync(user, Roles.ADMIN);
			context.UserProfiles.Add(new UserProfile(user.Id, USERNAME, EMAIL));
			await context.SaveChangesAsync();
		}
	}
}
