using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace PerfumeTracker.Server.Features.Auth;

public class JwtTokenGenerator(UserManager<PerfumeIdentityUser> userManager, IConfiguration config, 
	ILogger<JwtTokenGenerator> logger) : IJwtTokenGenerator {
	public async Task<string> GenerateToken(PerfumeIdentityUser user) {
		var jwtConfiguration = new JwtConfiguration(config);
		var claims = new List<Claim>
		{
			new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			new Claim(ClaimTypes.Name, user.UserName ?? ""),
			new Claim(ClaimTypes.Email, user.Email ?? ""),
        };
		var roles = await userManager.GetRolesAsync(user);
		foreach (var role in roles) {
			claims.Add(new Claim(ClaimTypes.Role, role));
		}

		var creds = new SigningCredentials(jwtConfiguration.Key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			jwtConfiguration.Issuer,
			jwtConfiguration.Audience,
			claims: claims,
			expires: DateTime.UtcNow.AddHours(jwtConfiguration.ExpirationHours),
			signingCredentials: creds
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
	public async Task WriteToken(PerfumeIdentityUser user, HttpContext context) {
		var token = await GenerateToken(user);
		bool isSecure = context.Request.IsHttps || 
				string.Equals(context.Request.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase);
		logger.Log(LogLevel.Information, "Secure cookies: {isSecure}", isSecure);
		var cookieOptions = new CookieOptions {
			HttpOnly = true,
			Secure = context.Request.IsHttps || 
				string.Equals(context.Request.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase),
			SameSite = SameSiteMode.Strict,
			Expires = DateTime.UtcNow.AddHours(24)
		};

		context.Response.Cookies.Append("jwt", token, cookieOptions);
		context.Response.Cookies.Append("X-Username", user.UserName ?? string.Empty, cookieOptions);
	}
}

public interface IJwtTokenGenerator {
	Task<string> GenerateToken(PerfumeIdentityUser user);
	Task WriteToken(PerfumeIdentityUser user, HttpContext context);
}