namespace PerfumeTracker.Server.Services.Auth;

public static class SeedRoles {
	public static async Task SeedRolesAsync(IServiceProvider serviceProvider) {
		var roleManager = serviceProvider.GetRequiredService<RoleManager<PerfumeIdentityRole>>();
		string[] roleNames = { Roles.ADMIN, Roles.DEMO, Roles.USER };
		foreach (var roleName in roleNames) {
			if (!await roleManager.RoleExistsAsync(roleName)) {
				var result = await roleManager.CreateAsync(new PerfumeIdentityRole(roleName));
				if (!result.Succeeded) throw new InvalidOperationException($"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
			}
		}
	}
}
