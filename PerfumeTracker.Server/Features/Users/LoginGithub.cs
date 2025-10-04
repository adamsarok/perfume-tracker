
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using PerfumeTracker.Server.Services.Auth;
using System.Security.Claims;
using System.Text;

namespace PerfumeTracker.Server.Features.Users;

public class LoginGithub : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/auth/github/login", (HttpContext ctx, IConfiguration config) => {
			var clientUrl = config["Authentication:ClientUrl"] ?? "http://localhost:3000";
			var props = new AuthenticationProperties {
				RedirectUri = clientUrl
			};

			return Results.Challenge(props, new[] { "GitHub" });
		});
	}
}

