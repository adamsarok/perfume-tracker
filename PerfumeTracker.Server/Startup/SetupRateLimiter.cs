using System.Globalization;
using System.Threading.RateLimiting;

namespace PerfumeTracker.Server.Startup;
public static partial class Startup {
	public static void SetupRateLimiting(IServiceCollection services) {
		services.AddRateLimiter(options => {
			options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext => {
				string path = httpContext.Request.Path.ToString();
				if (path.StartsWith("/api/identity/account/login") || path.StartsWith("/api/identity/account/register")) {
					return RateLimitPartition.GetFixedWindowLimiter(
						partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
						factory: _ => new FixedWindowRateLimiterOptions {
							PermitLimit = 10,
							Window = TimeSpan.FromMinutes(1)
						});
				}
				return RateLimitPartition.GetFixedWindowLimiter(
					partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
					factory: _ => new FixedWindowRateLimiterOptions {
						PermitLimit = 100,
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

				//context.HttpContext.RequestServices.GetService<ILoggerFactory>()?
				//	.CreateLogger("Microsoft.AspNetCore.RateLimitingMiddleware")
				//	.LogWarning("OnRejected: {GetUserEndPoint}", GetUserEndPoint(context.HttpContext));

				return new ValueTask();
			};
		});
	}
}
