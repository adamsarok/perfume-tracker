using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace PerfumeTracker.Server.Services.Auth;

public class JwtTokenGenerator(UserManager<PerfumeIdentityUser> userManager, IOptions<JwtConfiguration> jwtOptions,
	ILogger<JwtTokenGenerator> logger) : IJwtTokenGenerator {
	private readonly JwtConfiguration jwtConfiguration = jwtOptions.Value;
	
	public async Task<string> GenerateToken(PerfumeIdentityUser user) {
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

		var creds = new SigningCredentials(jwtConfiguration.GetSecurityKey(), SecurityAlgorithms.HmacSha256);

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

		var cookieOptions = new CookieOptions {
			HttpOnly = true,
			Secure = context.Request.IsHttps ||
				string.Equals(context.Request.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase),
			SameSite = SameSiteMode.None,
			Expires = DateTime.UtcNow.AddHours(24),
			Path = "/"
		};

		context.Response.Cookies.Append("jwt", token, cookieOptions);
		context.Response.Cookies.Append("X-Username", user.UserName ?? string.Empty, cookieOptions);

		logger.LogInformation(
			"Setting JWT cookie with options: Domain={Domain}, Path={Path}, SameSite={SameSite}, Secure={Secure}, HttpOnly={HttpOnly}",
			cookieOptions.Domain ?? "(not set)",
			cookieOptions.Path,
			cookieOptions.SameSite,
			cookieOptions.Secure,
			cookieOptions.HttpOnly);
	}
}

public interface IJwtTokenGenerator {
	Task<string> GenerateToken(PerfumeIdentityUser user);
	Task WriteToken(PerfumeIdentityUser user, HttpContext context);
}