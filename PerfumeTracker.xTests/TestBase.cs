using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PerfumeTracker.Server.Features.Auth;

namespace PerfumeTracker.xTests;
public class TestBase(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>> {
	protected WebApplicationFactory<Program> Factory => factory;
	protected static bool DbUp = false;
	protected static bool UserUp = false;
	private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
	protected static MockTenantProvider TenantProvider = new MockTenantProvider();
	protected async Task PrepareUser() {
		await semaphore.WaitAsync();
		try {
			if (!UserUp) {
				using var scope = factory.Services.CreateScope();
				using var context = GetTestContext();
				var logger = Mock.Of<ILogger<CreateUser>>();
				using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PerfumeIdentityUser>>();
				const string testMail = "test@example.com";
				var user = await userManager.FindByEmailAsync(testMail);
				if (user == null) {
					var createUserHandler = new CreateUser(logger, userManager, context);
					user = await createUserHandler.Create("test", "abcd1234ABCDxyz59697", Roles.USER, "test@example.com", false);
				}
				TenantProvider.MockTenantId = user.Id;
				UserUp = true;
			}
		} finally {
			semaphore.Release();
		}
	}

	protected PerfumeTrackerContext GetTestContext() {
		var scope = factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		if (!context.Database.GetDbConnection().Database.ToLower().Contains("test")) {
			context.Dispose();
			scope.Dispose();
			throw new Exception("Live database connected!");
		}
		context.TenantProvider = TenantProvider;
		return context;
	}
}
