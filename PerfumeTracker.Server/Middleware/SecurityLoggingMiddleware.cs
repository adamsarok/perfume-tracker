using Serilog;

namespace PerfumeTracker.Server.Middleware;
public class SecurityLoggingMiddleware {
	private readonly RequestDelegate _next;
	public SecurityLoggingMiddleware(RequestDelegate next) {
		_next = next;
	}
	public async Task InvokeAsync(HttpContext context) {
		await _next(context);
		if ((context.Response.StatusCode == 401 || context.Response.StatusCode == 403)
			&& !context.Request.Path.StartsWithSegments("/api/identity/account/me")) {
			Log.Warning(
				"Failed authentication attempt from {IP} to {Path}. Status: {StatusCode}",
				context.Connection.RemoteIpAddress,
				context.Request.Path,
				context.Response.StatusCode);
		}
	}
}
public static class SecurityLoggingMiddlewareExtensions {
	public static IApplicationBuilder UseSecurityLogging(this IApplicationBuilder app) {
		return app.UseMiddleware<SecurityLoggingMiddleware>();
	}
}