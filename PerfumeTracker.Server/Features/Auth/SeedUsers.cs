using PerfumeTracker.Server.Features.Users;

namespace PerfumeTracker.Server.Features.Auth;
public interface ISeedUsers {
	Task SeedAdminAsync();
	Task SeedDemoUserAsync();
}
public class SeedUsers(IConfiguration configuration, ICreateUser createUser, ILogger<SeedUsers> logger) : ISeedUsers {
	public async Task SeedAdminAsync() {
		var userName = configuration["Users:AdminUserName"] ?? throw new InvalidOperationException("Users:AdminUserName is not configured");
		var email = configuration["Users:AdminEmail"] ?? throw new InvalidOperationException("Users:AdminEmail is not configured");
		var password = configuration["Users:AdminPassword"] ?? throw new InvalidOperationException("Users:AdminPassword is not configured");
		await createUser.Create(userName, password, Roles.ADMIN, email, true);
	}
	public async Task SeedDemoUserAsync() {
		try {
			var userName = configuration["Users:DemoUserName"] ?? throw new InvalidOperationException("Users:DemoUserName is not configured");
			var email = configuration["Users:DemoEmail"] ?? throw new InvalidOperationException("Users:DemoEmail is not configured");
			var password = configuration["Users:DemoPassword"] ?? throw new InvalidOperationException("Users:DemoPassword is not configured");
			await createUser.Create(userName, password, Roles.DEMO, email, true);
		} catch (Exception ex) {
			logger.Log(LogLevel.Warning, ex, "Demo user not created");
		}
	}
}
