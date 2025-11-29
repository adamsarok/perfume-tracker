namespace PerfumeTracker.Server.Config;

public class UserConfiguration {
	public string AdminUserName { get; init; }
	public string AdminPassword { get; init; }
	public string AdminEmail { get; init; }
	public string? DemoUserName { get; init; }
	public string? DemoPassword { get; init; }
	public string? DemoEmail { get; init; }
	public bool InviteOnlyRegistration { get; init; }
	public int StreakProtectionDays { get; init; }
	public UserConfiguration(IConfiguration configuration) {
		AdminUserName = configuration["Users:AdminUserName"] ?? throw new ConfigEmptyException("Users:AdminUserName is not configured");
		AdminEmail = configuration["Users:AdminEmail"] ?? throw new ConfigEmptyException("Users:AdminEmail is not configured");
		AdminPassword = configuration["Users:AdminPassword"] ?? throw new ConfigEmptyException("Users:AdminPassword is not configured");
		DemoUserName = configuration["Users:DemoUserName"];
		DemoEmail = configuration["Users:DemoEmail"];
		DemoPassword = configuration["Users:DemoPassword"];
		InviteOnlyRegistration = configuration.GetValue<bool?>("Users:InviteOnlyRegistration") ?? throw new ConfigEmptyException("Users:InviteOnlyRegistration is not configured");
		StreakProtectionDays = configuration.GetValue<int>("Users:StreakProtectionDays");
	}
}