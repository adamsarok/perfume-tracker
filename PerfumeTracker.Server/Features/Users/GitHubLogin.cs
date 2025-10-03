
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

				var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
				if (string.IsNullOrWhiteSpace(email)) return Results.Unauthorized();

				var user = await userManager.FindByEmailAsync(email);
				if (user == null) return Results.Unauthorized();

				await jwtTokenGenerator.WriteToken(user, ctx);

				var clientUrl = config["Authentication:ClientUrl"] ?? "http://localhost:3000";

				return Results.Redirect(clientUrl);
			});
	}
}

