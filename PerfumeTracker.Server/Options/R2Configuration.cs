using System.ComponentModel.DataAnnotations;

namespace PerfumeTracker.Server.Options;

public class R2Configuration {
	public string? AccessKey { get; set; }
	public string? SecretKey { get; set; }
	public string? AccountId { get; set; }
	public string? BucketName { get; set; }

	[Range(1, int.MaxValue)]
	public int ExpirationHours { get; set; } = 1;

	[Range(1, 1024 * 128)]
	public int MaxFileSizeKb { get; set; } = 256;
	public bool IsConfigured => !string.IsNullOrWhiteSpace(AccessKey)
		&& !string.IsNullOrWhiteSpace(SecretKey)
		&& !string.IsNullOrWhiteSpace(AccountId)
		&& !string.IsNullOrWhiteSpace(BucketName);
}