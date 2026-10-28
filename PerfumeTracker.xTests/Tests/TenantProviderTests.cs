using Microsoft.AspNetCore.Http;
using PerfumeTracker.Server.Features.Auth;
using System.Security.Claims;

namespace PerfumeTracker.xTests.Tests;

public class TenantProviderTests {
	[Fact]
	public void GetCurrentUserId_WhenAuthenticationCompletesAfterConstruction_ReturnsUserId() {
		var context = new DefaultHttpContext();
		var provider = new TenantProvider(new HttpContextAccessor { HttpContext = context });
		Assert.Null(provider.GetCurrentUserId());

		var userId = Guid.NewGuid();
		context.User = new ClaimsPrincipal(new ClaimsIdentity(
			[new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Identity.Application"));

		Assert.Equal(userId, provider.GetCurrentUserId());
		context.User = new ClaimsPrincipal(new ClaimsIdentity());
		Assert.Null(provider.GetCurrentUserId());
	}

	[Theory]
	[InlineData(null)]
	[InlineData("invalid-user-id")]
	public void GetCurrentUserId_WhenUserIdIsMissingOrInvalid_ReturnsNull(string? userId) {
		var context = new DefaultHttpContext();
		if (userId is not null) {
			context.User = new ClaimsPrincipal(new ClaimsIdentity(
				[new Claim(ClaimTypes.NameIdentifier, userId)], "Identity.Application"));
		}
		var provider = new TenantProvider(new HttpContextAccessor { HttpContext = context });

		Assert.Null(provider.GetCurrentUserId());
	}

	[Fact]
	public void GetCurrentUserId_WithoutHttpContext_ReturnsNull() {
		var provider = new TenantProvider(new HttpContextAccessor());

		Assert.Null(provider.GetCurrentUserId());
	}
}
