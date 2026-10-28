using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.Auth;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

public class JwtTokenGeneratorFixture : DbFixture {
	public override Task SeedTestData(PerfumeTrackerContext context) => Task.CompletedTask;
}

public class JwtTokenGeneratorTests(JwtTokenGeneratorFixture fixture) : IClassFixture<JwtTokenGeneratorFixture> {
	[Fact]
	public async Task GenerateToken_IncludesIdentityAndRoleClaims() {
		using var scope = fixture.Factory.Services.CreateScope();
		var generator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PerfumeIdentityUser>>();
		var user = await userManager.FindByIdAsync(fixture.TenantProvider.MockTenantId!.Value.ToString());
		Assert.NotNull(user);

		var token = await generator.GenerateToken(user);
		var handler = new JwtSecurityTokenHandler();
		var decoded = handler.ReadJwtToken(token);

		Assert.Equal(user.Id.ToString(), decoded.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
		Assert.Equal(user.UserName, decoded.Claims.Single(c => c.Type == ClaimTypes.Name).Value);
		Assert.Equal(user.Email, decoded.Claims.Single(c => c.Type == ClaimTypes.Email).Value);
		Assert.Contains(Roles.USER, decoded.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value));
		Assert.True(decoded.ValidTo > DateTime.UtcNow);
	}

	[Fact]
	public async Task GenerateToken_HandlesUserWithoutUserNameOrEmail() {
		using var scope = fixture.Factory.Services.CreateScope();
		var generator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
		var transientUser = new PerfumeIdentityUser { Id = Guid.NewGuid() };

		var token = await generator.GenerateToken(transientUser);
		var decoded = new JwtSecurityTokenHandler().ReadJwtToken(token);

		Assert.Equal(transientUser.Id.ToString(), decoded.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
		Assert.Equal(string.Empty, decoded.Claims.Single(c => c.Type == ClaimTypes.Name).Value);
		Assert.Equal(string.Empty, decoded.Claims.Single(c => c.Type == ClaimTypes.Email).Value);
	}

	[Fact]
	public async Task WriteToken_AppendsJwtAndUsernameCookies() {
		using var scope = fixture.Factory.Services.CreateScope();
		var generator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PerfumeIdentityUser>>();
		var user = await userManager.FindByIdAsync(fixture.TenantProvider.MockTenantId!.Value.ToString());
		Assert.NotNull(user);

		var httpContext = new DefaultHttpContext();
		await generator.WriteToken(user, httpContext);

		var setCookie = httpContext.Response.Headers["Set-Cookie"].ToString();
		Assert.Contains("jwt=", setCookie);
		Assert.Contains($"X-Username={user.UserName}", setCookie);
		Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("secure", setCookie, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task WriteToken_MarksCookiesSecureForForwardedHttps() {
		using var scope = fixture.Factory.Services.CreateScope();
		var generator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
		var transientUser = new PerfumeIdentityUser { Id = Guid.NewGuid(), UserName = "forwarded-user" };

		var httpContext = new DefaultHttpContext();
		httpContext.Request.Headers["X-Forwarded-Proto"] = "https";
		await generator.WriteToken(transientUser, httpContext);

		var setCookie = httpContext.Response.Headers["Set-Cookie"].ToString();
		Assert.Contains("jwt=", setCookie);
		Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
	}
}
