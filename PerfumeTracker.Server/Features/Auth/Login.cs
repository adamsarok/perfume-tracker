
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace PerfumeTracker.Server.Features.Auth;

public class LoginEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/identity/account/login", async ([FromBody] LoginRequest request,
		  UserManager<PerfumeIdentityUser> userManager, IJwtTokenGenerator jwtTokenGenerator, HttpContext context) => {
			  var user = await userManager.FindByEmailAsync(request.Email);
			  if (user == null || !await userManager.CheckPasswordAsync(user, request.Password)) {
				  return Results.Unauthorized();
			  }
			  var token = await jwtTokenGenerator.GenerateToken(user);

			  var cookieOptions = new CookieOptions {
				  HttpOnly = true,
				  Secure = true,
				  SameSite = SameSiteMode.Strict,
				  Expires = DateTime.UtcNow.AddHours(24)
			  };

			  context.Response.Cookies.Append("jwt", token, cookieOptions);
			  context.Response.Cookies.Append("X-Username", user.UserName ?? string.Empty, cookieOptions);

			  return Results.Ok(new { message = "Logged in successfully" });
		  }).WithTags("Auth")
			.WithName("Login")
			.AllowAnonymous();
	}
}
