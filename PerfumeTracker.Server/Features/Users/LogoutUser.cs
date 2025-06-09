namespace PerfumeTracker.Server.Features.Auth;
public class LogoutEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/identity/account/logout", async (HttpContext httpContext) => {
			httpContext.Response.Cookies.Delete("jwt");
			httpContext.Response.Cookies.Delete("X-Username");
			httpContext.Response.Cookies.Delete("X-User-Id");
			return Results.Ok(new { message = "Logged out successfully" });
		}).WithTags("Auth")
			.WithName("Logout")
			.AllowAnonymous();
	}
}