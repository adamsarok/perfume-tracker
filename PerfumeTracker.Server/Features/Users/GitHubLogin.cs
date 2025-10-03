
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using PerfumeTracker.Server.Services.Auth;
using System.Security.Claims;
using System.Text;

namespace PerfumeTracker.Server.Features.Users;

public class GitHubLogin : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/auth/github/login", (HttpContext ctx) => {
			var props = new AuthenticationProperties {
				RedirectUri = "/api/auth/github/callback"
			};

			return Results.Challenge(props, new[] { "GitHub" });
		});

		app.MapGet("/api/auth/github/callback", async (HttpContext ctx, IConfiguration config,
			UserManager<PerfumeIdentityUser> userManager, IJwtTokenGenerator jwtTokenGenerator) => {
				var result = await ctx.AuthenticateAsync("External");

				if (!result.Succeeded || result.Principal is null)
					return Results.Unauthorized();

				var claims = result.Principal.Identities.First().Claims;

				var userId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? Guid.NewGuid().ToString();
				var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "";

				var user = await userManager.FindByEmailAsync(email);
				if (user == null) return Results.Unauthorized();
				//var jwt = TEMP_GenerateJwt(userId, email, config);

				// Option A: return JSON with token
				// return Results.Json(new { token = jwt });

				// Option B: set cookie and redirect to NextJS
				//ctx.Response.Cookies.Append("jwt", jwt, new CookieOptions {
				//	HttpOnly = true,
				//	Secure = true,
				//	SameSite = SameSiteMode.Strict,
				//	Expires = DateTimeOffset.UtcNow.AddHours(1)
				//});

				await jwtTokenGenerator.WriteToken(user, ctx);
				//return new LoginResult(Results.Ok(new { message = "Logged in successfully" }));

				return Results.Redirect("http://localhost:3000");
			});
	}
	private static string TEMP_GenerateJwt(string userId, string email, IConfiguration config) { //todo
		var claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, userId),
			new Claim(JwtRegisteredClaimNames.Email, email),
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
		};

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: config["Jwt:Issuer"],
			audience: config["Jwt:Audience"],
			claims: claims,
			expires: DateTime.UtcNow.AddHours(1),
			signingCredentials: creds);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}

