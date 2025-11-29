using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.Users;
using PerfumeTracker.Server.Services.Common;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("User Tests")]
public class UserCollection : ICollectionFixture<UserFixture>;

public class UserFixture : DbFixture {
	public UserFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		await Task.CompletedTask;
	}
}

[Collection("User Tests")]
public class UserTests {
	private readonly UserFixture _fixture;

	public UserTests(UserFixture fixture) {
		_fixture = fixture;
	}

	[Fact]
	public async Task GetCurrentUserXP_ReturnsXP() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var xpService = new XPService(context);
		var handler = new GetCurrentUserXPHandler(context, xpService);
		var result = await handler.Handle(new UserXPQuery(), CancellationToken.None);
		Assert.NotNull(result);
	}

	[Fact]
	public async Task RegisterUser() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var inviteHandler = new CreateInviteHandler(context);
		const string test2Mail = "test2@user.com";
		var inviteResult = await inviteHandler.Handle(new CreateInviteCommand(test2Mail), CancellationToken.None);
		var createUser = scope.ServiceProvider.GetRequiredService<ICreateUser>();
		var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
		var registerHandler = new RegisterUserHandler(createUser, configuration, context);
		var registerCommand = new RegisterUserCommand("TestUser2", test2Mail, "abc123DEF_-y-98756123", inviteResult.InviteCode.ToString());
		await registerHandler.Handle(registerCommand, CancellationToken.None);
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PerfumeIdentityUser>>();
		var result = await userManager.FindByEmailAsync(test2Mail);
		Assert.NotNull(result);
		await userManager.DeleteAsync(result);
	}
}

