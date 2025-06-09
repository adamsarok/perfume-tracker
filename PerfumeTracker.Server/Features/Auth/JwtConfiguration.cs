using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace PerfumeTracker.Server.Features.Auth;

public class JwtConfiguration {
	public string Issuer { get; init; }
	public string Audience { get; init; }
	public SymmetricSecurityKey Key { get; init; }
	public int ExpirationHours { get; init; }
	public JwtConfiguration(IConfiguration configuration) {
		var keyStr = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
		Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
		Issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
		Audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured");
		var expirationHoursStr = configuration["Jwt:ExpirationHours"] ?? throw new InvalidOperationException("JWT ExpirationHours is not configured");
		if (!int.TryParse(expirationHoursStr, out int expirationHours)) throw new InvalidOperationException("JWT ExpirationHours is invalid");
		ExpirationHours = expirationHours;
	}
}
