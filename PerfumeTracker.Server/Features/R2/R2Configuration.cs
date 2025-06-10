namespace PerfumeTracker.Server.Features.R2;

public class R2Configuration {
	public string AccessKey { get; init; }
	public string SecretKey { get; init; }
	public string AccountId { get; init; }
	public string BucketName { get; init; }
	public int ExpirationHours { get; init; }
	public R2Configuration(IConfiguration configuration) {
		AccessKey = configuration["R2:AccessKey"] ?? throw new InvalidOperationException("R2 AccessKey is not configured");
		SecretKey = configuration["R2:SecretKey"] ?? throw new InvalidOperationException("R2 SecretKey is not configured");
		AccountId = configuration["R2:AccountId"] ?? throw new InvalidOperationException("R2 AccountId is not configured");
		BucketName = configuration["R2:BucketName"] ?? throw new InvalidOperationException("R2 BucketName is not configured");
		var expirationHoursStr = configuration["R2:ExpirationHours"] ?? throw new InvalidOperationException("R2 ExpirationHours is not configured");
		if (!int.TryParse(expirationHoursStr, out int expirationHours)) throw new InvalidOperationException("R2 ExpirationHours is invalid");
		ExpirationHours = expirationHours;
	}
}
