using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace PerfumeTracker.Server.Config;

public class JwtConfiguration {
	public string Issuer { get; init; }
	public string Audience { get; init; }
	public SymmetricSecurityKey Key { get; init; }
	public int ExpirationHours { get; init; }
	public JwtConfiguration(IConfiguration configuration) {
		var keyStr = configuration["Jwt:Key"] ?? throw new ConfigEmptyException("JWT Key is not configured");
		Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
		Issuer = configuration["Jwt:Issuer"] ?? throw new ConfigEmptyException("JWT Issuer is not configured");
		Audience = configuration["Jwt:Audience"] ?? throw new ConfigEmptyException("JWT Audience is not configured");
		ExpirationHours = configuration.GetValue<int?>("Jwt:ExpirationHours") ?? throw new ConfigEmptyException("JWT ExpirationHours is not configured");
	}
}
