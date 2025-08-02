using Microsoft.AspNetCore.Mvc.Testing;
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
		var result = await handler.Handle(new UserXPQuery(), new CancellationToken());
		Assert.NotNull(result); //TODO: add completed mission and assert
	}
}

