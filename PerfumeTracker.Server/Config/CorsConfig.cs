using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace PerfumeTracker.Server.Config;

public class CorsConfiguration {
	public string[] AllowedOrigins { get; init; }
	public CorsConfiguration(IConfiguration configuration) {
		var keyStr = configuration["Cors:AllowedOrigins"] ?? throw new ConfigEmptyException("Cors AllowedOrigins is not configured");
		AllowedOrigins = keyStr.Split(";");
	}
}
