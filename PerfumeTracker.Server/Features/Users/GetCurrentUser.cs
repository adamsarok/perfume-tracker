using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PerfumeTracker.Server.Features.Users;
public record UserResponse(string UserName, string Email, string Id, IEnumerable<string> Roles);
public class GetCurrentUser : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/identity/account/me", (HttpContext context) =>
		{
			var user = context.User;
			return Results.Ok(new UserResponse(
				UserName: user.Identity?.Name,
				Email: user.FindFirst(ClaimTypes.Email)?.Value,
				Id: user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
				Roles: user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)
			));
		})
			.WithTags("Users")
			.WithName("GetCurrentUser")
			.RequireAuthorization(Policies.READ);
	}
}
