using Microsoft.AspNetCore.Http;
using PerfumeTracker.Server.Middleware;

namespace PerfumeTracker.xTests.Tests;

public class SecurityHeadersMiddlewareTests {

	[Fact]
	public async Task InvokeAsync_AppendsAllSecurityHeaders() {
		var context = new DefaultHttpContext();
		var nextCalled = false;
		var middleware = new SecurityHeadersMiddleware(_ => {
			nextCalled = true;
			return Task.CompletedTask;
		});

		await middleware.InvokeAsync(context);

		Assert.True(nextCalled);
		Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
		Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
		Assert.Equal("1; mode=block", context.Response.Headers["X-XSS-Protection"]);
		Assert.Equal("none", context.Response.Headers["X-Permitted-Cross-Domain-Policies"]);
		Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"]);
		Assert.Contains("camera=()", context.Response.Headers["Permissions-Policy"].ToString());
		Assert.Equal("require-corp", context.Response.Headers["Cross-Origin-Embedder-Policy"]);
		Assert.Equal("same-origin", context.Response.Headers["Cross-Origin-Opener-Policy"]);
		Assert.Equal("same-origin", context.Response.Headers["Cross-Origin-Resource-Policy"]);
	}
}
