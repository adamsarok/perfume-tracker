namespace PerfumeTracker.Server.Config;

public class R2Configuration {
	public string? AccessKey { get; init; }
	public string? SecretKey { get; init; }
	public string? AccountId { get; init; }
	public string? BucketName { get; init; }
	public int ExpirationHours { get; init; }
	public int MaxFileSizeKb { get; init; }
	public bool IsEnabled { get ; init;}
	public R2Configuration(IConfiguration configuration) {
		AccessKey = configuration["R2:AccessKey"];
		SecretKey = configuration["R2:SecretKey"];
		AccountId = configuration["R2:AccountId"];
		BucketName = configuration["R2:BucketName"];
		ExpirationHours = configuration.GetValue<int?>("R2:ExpirationHours") ?? 1;
		MaxFileSizeKb = configuration.GetValue<int?>("R2:MaxFileSizeKb") ?? 256;
		if (ExpirationHours <= 0 || MaxFileSizeKb <= 0) throw new InvalidOperationException("Expiration and file size limit values must be positive integers");
		if (string.IsNullOrWhiteSpace(AccessKey) 
			|| string.IsNullOrWhiteSpace(SecretKey) 
			|| string.IsNullOrWhiteSpace(AccountId) 
			|| string.IsNullOrWhiteSpace(BucketName)) {
				IsEnabled = false;
		}
	}
}
