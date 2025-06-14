namespace PerfumeTracker.Server.Config;

public class CorsConfiguration {
	public string[] AllowedOrigins { get; init; }
	public CorsConfiguration(IConfiguration configuration) {
		var keyStr = configuration["Cors:AllowedOrigins"] ?? throw new ConfigEmptyException("Cors AllowedOrigins is not configured");
		AllowedOrigins = keyStr
				.Split(';', StringSplitOptions.RemoveEmptyEntries)
				.Select(o => o.Trim())
				.Where(o => !string.IsNullOrWhiteSpace(o))
				.ToArray();
		if (AllowedOrigins.Length == 0)
			throw new ConfigEmptyException("Cors AllowedOrigins is empty");
	}
}