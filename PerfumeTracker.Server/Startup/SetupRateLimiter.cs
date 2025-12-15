using System.Globalization;
using System.Threading.RateLimiting;

namespace PerfumeTracker.Server.Startup;

public static partial class Startup {
	public static void SetupRateLimiting(IServiceCollection services, RateLimitConfiguration rateLimiterConfig) {
		services.AddRateLimiter(options => {
			options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext => {
				string path = httpContext.Request.Path.ToString();
				if (httpContext.Request.Path.StartsWithSegments("/api/identity/account/login") || httpContext.Request.Path.StartsWithSegments("/api/identity/account/register")) {
					return RateLimitPartition.GetFixedWindowLimiter(
						partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
						factory: _ => new FixedWindowRateLimiterOptions {
							PermitLimit = rateLimiterConfig.Auth,
							Window = TimeSpan.FromMinutes(1)
						});
				}
				if (httpContext.Request.Path.StartsWithSegments("/api/images/upload")) {
					return RateLimitPartition.GetFixedWindowLimiter(
						partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
						factory: _ => new FixedWindowRateLimiterOptions {
							PermitLimit = rateLimiterConfig.Upload,
							Window = TimeSpan.FromMinutes(1)
						});
				}
				return RateLimitPartition.GetFixedWindowLimiter(
					partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
					factory: _ => new FixedWindowRateLimiterOptions {
						PermitLimit = rateLimiterConfig.General,
						Window = TimeSpan.FromMinutes(1)
					});
			});
			options.OnRejected = (context, cancellationToken) => {
				var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
				if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)) {
					context.HttpContext.Response.Headers.RetryAfter =
						((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
				}
				context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

				logger.LogWarning("Rate limit exceeded for user {User} {RemoteIpAddress} at {RequestPath}. Retry after: {RetryAfter} seconds.",
					context.HttpContext.User.Identity?.Name ?? "Anonymous",
					context.HttpContext.Connection.RemoteIpAddress,
					context.HttpContext.Request.Path,
					retryAfter.TotalSeconds);
				return new ValueTask();
			};
		});
	}
}