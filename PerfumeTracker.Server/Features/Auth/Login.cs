
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace PerfumeTracker.Server.Features.Auth;

public class LoginEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/identity/account/login", async ([FromBody] LoginRequest request,
			UserManager<PerfumeIdentityUser> userManager, IJwtTokenGenerator jwtTokenGenerator) => {
				var user = await userManager.FindByEmailAsync(request.Email);
				if (user == null || !await userManager.CheckPasswordAsync(user, request.Password)) {
					return Results.Unauthorized();
				}
				var token = jwtTokenGenerator.GenerateToken(user);
				return Results.Ok(new { token });
			}).WithTags("Auth")
			.WithName("Login")
			.AllowAnonymous();
	}
}
