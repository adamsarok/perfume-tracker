namespace PerfumeTracker.Server.Middleware;

public class SecurityHeadersMiddleware {
	private readonly RequestDelegate _next;

	public SecurityHeadersMiddleware(RequestDelegate next) {
		_next = next;
	}

	public async Task InvokeAsync(HttpContext context) {
		context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
		context.Response.Headers.Append("X-Frame-Options", "DENY");
		context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
		context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");
		context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
		context.Response.Headers.Append("Permissions-Policy",
			"accelerometer=(), " +
			"camera=(), " +
			"geolocation=(), " +
			"gyroscope=(), " +
			"magnetometer=(), " +
			"microphone=(), " +
			"payment=(), " +
			"usb=(), " +
			"interest-cohort=()");
		context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
		context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
		context.Response.Headers.Append("Cross-Origin-Resource-Policy", "same-origin");
		await _next(context);
	}
}
public static class SecurityHeadersMiddlewareExtensions {
	public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) {
		return app.UseMiddleware<SecurityHeadersMiddleware>();
	}
}