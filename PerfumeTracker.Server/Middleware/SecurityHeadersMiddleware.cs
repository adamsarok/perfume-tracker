using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace PerfumeTracker.Server.Middleware;

public class SecurityHeadersMiddleware {
	private readonly RequestDelegate _next;
	private readonly IHostEnvironment _environment;

	public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment) {
		_next = next;
		_environment = environment;
	}

	public async Task InvokeAsync(HttpContext context) {

		context.Response.Headers.Append(
			"Content-Security-Policy",
			"default-src 'self'; " +
			"img-src 'self' https://*.r2.cloudflarestorage.com; " +
			"script-src 'self'; " +
			"style-src 'self'; " +
			"font-src 'self' data: https:; " +
			"connect-src 'self' https://*.r2.cloudflarestorage.com; " +
			"frame-ancestors 'none'; " +
			"form-action 'self'; " +
			"base-uri 'self'; " +
			"object-src 'none'; " +
			"upgrade-insecure-requests;"
		);

		context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
		context.Response.Headers.Append("X-Frame-Options", "DENY");
		context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
		context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
		context.Response.Headers.Append("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");

		await _next(context);
	}
}