using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using PerfumeTracker.Server.Services.Auth;
using System.Security.Claims;

namespace PerfumeTracker.Server.Features.Users;
public record UserResponse(string UserName, string Email, string Id, IEnumerable<string> Roles);
public class GetCurrentUser : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/identity/account/me", (HttpContext context) =>
		{
			ClaimsPrincipal user = context.User;
			return Results.Ok(new UserResponse(
				UserName: user.Identity?.Name ?? string.Empty,
				Email: user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
				Id: user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
				Roles: user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)
			));
		})
			.WithTags("Users")
			.WithName("GetCurrentUser")
			.RequireAuthorization(Policies.READ);
	}
}
