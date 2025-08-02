using PerfumeTracker.Server.Features.Users;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace PerfumeTracker.Server.Services.Auth;
public interface ISeedUsers {
	Task<Guid> SeedAdminAsync();
	Task<Guid?> SeedDemoUserAsync();
}
public class SeedUsers(IConfiguration configuration, ICreateUser createUser, ILogger<SeedUsers> logger, UserManager<PerfumeIdentityUser> userManager) : ISeedUsers {
	public async Task<Guid> SeedAdminAsync() {
		var userName = configuration["Users:AdminUserName"] ?? throw new InvalidOperationException("Users:AdminUserName is not configured");
		var email = configuration["Users:AdminEmail"] ?? throw new InvalidOperationException("Users:AdminEmail is not configured");
		var password = configuration["Users:AdminPassword"] ?? throw new InvalidOperationException("Users:AdminPassword is not configured");
		var user = await userManager.FindByEmailAsync(email);
		if (user == null) user = await createUser.Create(userName, password, Roles.ADMIN, email, true);
		return user?.Id ?? throw new InvalidOperationException("Admin user creation failed");
	}
	public async Task<Guid?> SeedDemoUserAsync() {
		try {
			var userName = configuration["Users:DemoUserName"] ?? throw new InvalidOperationException("Users:DemoUserName is not configured");
			var email = configuration["Users:DemoEmail"] ?? throw new InvalidOperationException("Users:DemoEmail is not configured");
			var password = configuration["Users:DemoPassword"] ?? throw new InvalidOperationException("Users:DemoPassword is not configured");
			var user = await userManager.FindByEmailAsync(email);
			if (user == null) user = await createUser.Create(userName, password, Roles.DEMO, email, true);
			return user?.Id;
		} catch (Exception ex) {
			logger.Log(LogLevel.Warning, ex, "Demo user not created");
		}
		return null;
	}
}
