using Microsoft.AspNetCore.Identity;

namespace PerfumeTracker.Server.Features.Auth;

public static class SeedRoles {
	public static async Task SeedRolesAsync(IServiceProvider serviceProvider) {
		var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
		string[] roleNames = { Roles.ADMIN, Roles.DEMO, Roles.USER };
		foreach (var roleName in roleNames) {
			if (!await roleManager.RoleExistsAsync(roleName)) {
				await roleManager.CreateAsync(new IdentityRole(roleName));
			}
		}
	}
}
