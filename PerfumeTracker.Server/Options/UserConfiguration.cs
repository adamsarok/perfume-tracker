using System.ComponentModel.DataAnnotations;

namespace PerfumeTracker.Server.Options;

public interface IUserConfiguration {
	string AdminUserName { get; }
	string AdminPassword { get; }
	string AdminEmail { get; }
	string? DemoUserName { get; }
	string? DemoPassword { get; }
	string? DemoEmail { get; }
	bool InviteOnlyRegistration { get; }
	int StreakProtectionDays { get; }
}

public class UserConfiguration : IUserConfiguration {
	[Required]
	public string AdminUserName { get; init; }

	[Required]
	public string AdminPassword { get; init; }

	[Required]
	public string AdminEmail { get; init; }

	public string? DemoUserName { get; init; }
	public string? DemoPassword { get; init; }
	public string? DemoEmail { get; init; }
	public bool InviteOnlyRegistration { get; init; } = false;
	public int StreakProtectionDays { get; init; } = 2;
}