namespace PerfumeTracker.Server.Config;

public class R2Configuration {
	public string AccessKey { get; init; }
	public string SecretKey { get; init; }
	public string AccountId { get; init; }
	public string BucketName { get; init; }
	public int ExpirationHours { get; init; }
	public int MaxFileSizeKb { get; init; }
	public R2Configuration(IConfiguration configuration) {
		AccessKey = configuration["R2:AccessKey"] ?? throw new ConfigEmptyException("R2 AccessKey is not configured");
		SecretKey = configuration["R2:SecretKey"] ?? throw new ConfigEmptyException("R2 SecretKey is not configured");
		AccountId = configuration["R2:AccountId"] ?? throw new ConfigEmptyException("R2 AccountId is not configured");
		BucketName = configuration["R2:BucketName"] ?? throw new ConfigEmptyException("R2 BucketName is not configured");
		ExpirationHours = configuration.GetValue<int?>("R2:ExpirationHours") ?? throw new ConfigEmptyException("R2 ExpirationHours is not configured");
		MaxFileSizeKb = configuration.GetValue<int?>("R2:MaxFileSizeKb") ?? throw new ConfigEmptyException("R2 MaxFileSizeKb is not configured");
		if (ExpirationHours <= 0 || MaxFileSizeKb <= 0) throw new InvalidOperationException("Expiration and file size limit values must be positive integers");
	}
}
