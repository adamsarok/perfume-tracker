namespace PerfumeTracker.Server.Config;
public class UserConfiguration {
	public string AdminUserName { get; set; }
	public string AdminPassword { get; set; }
	public string AdminEmail { get; set; }
	public string? DemoUserName { get; set; }
	public string? DemoPassword { get; set; }
	public string? DemoEmail { get; set; }
	public bool InviteOnlyRegistration { get; set; }

	public UserConfiguration(IConfiguration configuration) {
		AdminUserName = configuration["Users:AdminUserName"] ?? throw new ConfigEmptyException("Users:AdminUserName is not configured");
		AdminEmail = configuration["Users:AdminEmail"] ?? throw new ConfigEmptyException("Users:AdminEmail is not configured");
		AdminPassword = configuration["Users:AdminPassword"] ?? throw new ConfigEmptyException("Users:AdminPassword is not configured");
		DemoUserName = configuration["Users:DemoUserName"];
		DemoEmail = configuration["Users:DemoEmail"];
		DemoPassword = configuration["Users:DemoPassword"];
		InviteOnlyRegistration = configuration.GetValue<bool?>("Users:InviteOnlyRegistration") ?? throw new ConfigEmptyException("Users:InviteOnlyRegistration is not configured");
	}
}
