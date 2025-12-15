using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace PerfumeTracker.Server.Config;

public class R2Configuration {
	public string? AccessKey { get; set; }
	public string? SecretKey { get; set; }
	public string? AccountId { get; set; }
	public string? BucketName { get; set; }
	public int ExpirationHours { get; set; } = 1;
	public int MaxFileSizeKb { get; set; } = 256;
	
	public bool IsEnabled => !string.IsNullOrWhiteSpace(AccessKey)
		&& !string.IsNullOrWhiteSpace(SecretKey)
		&& !string.IsNullOrWhiteSpace(AccountId)
		&& !string.IsNullOrWhiteSpace(BucketName);
}

public class R2ConfigurationValidator : IValidateOptions<R2Configuration> {
	public ValidateOptionsResult Validate(string? name, R2Configuration options) {
		if (options.ExpirationHours <= 0) {
			return ValidateOptionsResult.Fail("ExpirationHours must be a positive integer");
		}
		if (options.MaxFileSizeKb <= 0) {
			return ValidateOptionsResult.Fail("MaxFileSizeKb must be a positive integer");
		}
		return ValidateOptionsResult.Success;
	}
}
