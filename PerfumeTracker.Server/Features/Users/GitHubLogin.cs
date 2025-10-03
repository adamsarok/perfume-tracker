
using Microsoft.AspNetCore.Authentication;
using PerfumeTracker.Server.Services.Auth;
using System.Security.Claims;

namespace PerfumeTracker.Server.Features.Users;

public class GitHubLogin : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/identity/account/github/login", (HttpContext ctx) => {
			var props = new AuthenticationProperties {
				RedirectUri = "/api/identity/account/github/callback"
			};
			return Results.Challenge(props, new[] { "GitHub" });
		})
		.AllowAnonymous()
		.WithName("GitHubLogin");

		app.MapGet("/api/identity/account/github/callback", async (
			HttpContext ctx,
			UserManager<PerfumeIdentityUser> userManager,
			IJwtTokenGenerator tokenWriter) => {
				var result = await ctx.AuthenticateAsync("External");
				if (!result.Succeeded || result.Principal == null)
					return Results.Unauthorized();

				var principal = result.Principal;
				var email = principal.FindFirst(ClaimTypes.Email)?.Value
							?? principal.Claims.FirstOrDefault(c => c.Type == "urn:github:email")?.Value;
				var name = principal.Identity?.Name
						   ?? principal.Claims.FirstOrDefault(c => c.Type == "urn:github:login")?.Value
						   ?? email;

				if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(name))
					return Results.Unauthorized();

				PerfumeIdentityUser? user = null;
				if (!string.IsNullOrWhiteSpace(email))
					user = await userManager.FindByEmailAsync(email);
				if (user == null && !string.IsNullOrWhiteSpace(name))
					user = await userManager.FindByNameAsync(name);

				if (user == null) {
					user = new PerfumeIdentityUser {
						UserName = name ?? email!,
						Email = email
					};
					var createRes = await userManager.CreateAsync(user);
					if (!createRes.Succeeded)
						return Results.Unauthorized();
					// optionally assign default role(s)
					await userManager.AddToRoleAsync(user, Roles.USER);
				}

				await tokenWriter.WriteToken(user, ctx);

				// cleanup temp cookie
				await ctx.SignOutAsync("External");

				// redirect back to SPA
				return Results.Redirect("/");
			})
		.AllowAnonymous()
		.WithName("GitHubCallback");
	}
}

