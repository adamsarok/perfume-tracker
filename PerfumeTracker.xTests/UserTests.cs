using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.Users;
using PerfumeTracker.Server.Services.Common;

namespace PerfumeTracker.xTests;
public class UserTests : TestBase, IClassFixture<WebApplicationFactory<Program>> {
	public UserTests(WebApplicationFactory<Program> factory) : base(factory) { }
	[Fact]
	public async Task GetCurrentUserXP_ReturnsXP() {
		using var scope = GetTestScope();
		var xpService = new XPService(scope.PerfumeTrackerContext);
		var handler = new GetCurrentUserXPHandler(scope.PerfumeTrackerContext, xpService);
		var result = await handler.Handle(new UserXPQuery(), CancellationToken.None);
		Assert.NotNull(result);
	}

	[Fact]
	public async Task RegisterUser() {
		using var scope = GetTestScope();
		var inviteHandler = new CreateInviteHandler(scope.PerfumeTrackerContext);
		const string test2Mail = "test2@user.com";
		var inviteResult = await inviteHandler.Handle(new CreateInviteCommand(test2Mail), CancellationToken.None);
		var createUser = scope.ServiceScope.ServiceProvider.GetRequiredService<ICreateUser>();
		var configuration = scope.ServiceScope.ServiceProvider.GetRequiredService<IConfiguration>();
		var registerHandler = new RegisterUserHandler(createUser, configuration, scope.PerfumeTrackerContext);
		var registerCommand = new RegisterUserCommand("TestUser2", test2Mail, "abc123DEF_-y-98756123", inviteResult.InviteCode);
		await registerHandler.Handle(registerCommand, CancellationToken.None);
		var userManager = scope.ServiceScope.ServiceProvider.GetRequiredService<UserManager<PerfumeIdentityUser>>();
		var result = await userManager.FindByEmailAsync(test2Mail);
		Assert.NotNull(result);
		await userManager.DeleteAsync(result);
	}
}

