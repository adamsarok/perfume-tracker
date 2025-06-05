using Microsoft.AspNetCore.Identity;

namespace PerfumeTracker.Server.Features.Auth;
public static class SeedAdmin {
	public static async Task SeedAdminAsync(IServiceProvider serviceProvider) {
		var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
		var user = await userManager.FindByNameAsync("admin");

		if (user == null) {
			user = new ApplicationUser {
				UserName = "admin",
				Email = "admin@example.com",
				EmailConfirmed = true
			};
			var result = await userManager.CreateAsync(user, "admin");
			if (!result.Succeeded) {
				throw new InvalidOperationException("Failed to create admin user: " + string.Join(", ", result.Errors));
			}
			await userManager.AddToRoleAsync(user, Roles.ADMIN);
		}
	}
}
