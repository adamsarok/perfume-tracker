using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PerfumeTracker.Server.Features.Auth;
using PerfumeTracker.Server.Features.Users;

namespace PerfumeTracker.xTests;
public class TestBase : IClassFixture<WebApplicationFactory<Program>> {
	protected WebApplicationFactory<Program> Factory;
	protected static bool DbUp = false;
	private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
	protected static MockTenantProvider TenantProvider = new MockTenantProvider();
	public TestBase(WebApplicationFactory<Program> factory) {
		semaphore.Wait();
		try {
			this.Factory = factory;
			using var scope = GetTestScope();
			var logger = Mock.Of<ILogger<CreateUser>>();
			var userManager = scope.ServiceScope.ServiceProvider.GetRequiredService<UserManager<PerfumeIdentityUser>>();
			const string testMail = "test@example.com";
			var user = userManager.FindByEmailAsync(testMail).GetAwaiter().GetResult();
			if (user == null) {
				var createUserHandler = new CreateUser(logger, userManager, scope.PerfumeTrackerContext);
				user = createUserHandler.Create("test", "abcd1234ABCDxyz59697", Roles.USER, "test@example.com", false).GetAwaiter().GetResult();
			}
			TenantProvider.MockTenantId = user.Id;
		} finally {
			semaphore.Release();
		}
	}
	protected TestScope GetTestScope() => new TestScope(Factory, TenantProvider);
}
