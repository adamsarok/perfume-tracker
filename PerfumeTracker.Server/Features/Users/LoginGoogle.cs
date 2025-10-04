
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using PerfumeTracker.Server.Services.Auth;
using System.Security.Claims;
using System.Text;

namespace PerfumeTracker.Server.Features.Users;

public class LoginGoogle : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/auth/google/login", (HttpContext ctx, IConfiguration config) => {
			var clientUrl = config["Authentication:ClientUrl"] ?? "http://localhost:3000";
			var props = new AuthenticationProperties {
				RedirectUri = clientUrl
			};

			return Results.Challenge(props, new[] { "Google" });
		});

		app.MapGet("/api/auth/google/callback", async (HttpContext ctx, IConfiguration config,
			UserManager<PerfumeIdentityUser> userManager, IJwtTokenGenerator jwtTokenGenerator,
			ILogger<LoginGoogle> logger) => {
				logger.LogInformation("Google callback reached with params: code={Code}, state={State}",
					ctx.Request.Query["code"], ctx.Request.Query["state"]);

				var result = await ctx.AuthenticateAsync("External");

				if (!result.Succeeded || result.Principal is null) {
					logger.LogWarning("External authentication failed: {ErrorMessage}",
						result.Failure?.Message ?? "Unknown error");
					return Results.Unauthorized();
				}

				var identity = result.Principal.Identities.FirstOrDefault();
				if (identity == null) return Results.Unauthorized();
				var claims = identity.Claims;

				var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
				if (string.IsNullOrWhiteSpace(email)) return Results.Unauthorized();

				var user = await userManager.FindByEmailAsync(email);
				if (user == null) return Results.Unauthorized();

				await jwtTokenGenerator.WriteToken(user, ctx);

				var clientUrl = config["Authentication:ClientUrl"] ?? "http://localhost:3000";
				logger.LogInformation("Redirecting to {ClientUrl}", clientUrl);
				return Results.Redirect(clientUrl);
			});
	}
}

