using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PerfumeTracker.Server.Features.Users;
using PerfumeTracker.Server.Services.Auth;
using PerfumeTracker.Server.Services.Outbox;
using System;
using Xunit.Sdk;
using static PerfumeTracker.Server.Services.Missions.ProgressMissions;
using static PerfumeTracker.Server.Services.Streaks.ProgressStreaks;
using static PerfumeTracker.xTests.Fixture.TestBase;

namespace PerfumeTracker.xTests.Fixture;
public class TestBase : IClassFixture<WebApplicationFactory<Program>> {
	protected WebApplicationFactory<Program> Factory;
	protected bool DbUp = false;
	private static readonly SemaphoreSlim semaphore = new(1);
	protected MockTenantProvider TenantProvider = new();
	protected Mock<IHubContext<MissionProgressHub>> MockMissionProgressHubContext;
	protected Mock<IHubContext<StreakProgressHub>> MockStreakProgressHubContext;
	protected Mock<IClientProxy> MockClientProxy;
	protected List<HubMessage> HubMessages = [];
	protected Mock<ISideEffectQueue> MockSideEffectQueue;
	public record HubMessage(string HubSentMethod, object[] HubSentArgs);

	public TestBase(WebApplicationFactory<Program> factory) {
		semaphore.Wait();
		try {
			Factory = factory;
			using var scope = GetTestScope();
			var logger = Mock.Of<ILogger<CreateUser>>();
			var userManager = scope.ServiceScope.ServiceProvider.GetRequiredService<UserManager<PerfumeIdentityUser>>();
			const string testMail = "test@example.com";
			var user = userManager.FindByEmailAsync(testMail).GetAwaiter().GetResult();
			if (user == null) {
				var createUserHandler = new CreateUser(logger, userManager, scope.PerfumeTrackerContext);
				user = createUserHandler.Create("test", "abcd1234ABCDxyz59697", Roles.USER, "test@example.com", false).GetAwaiter().GetResult();
			}
			if (user == null) throw new InvalidOperationException("User creation failed");
			TenantProvider.MockTenantId = user.Id;

			var mockClients = new Mock<IHubClients>();
			MockClientProxy = new Mock<IClientProxy>();
			_ = mockClients.Setup(clients => clients.All).Returns(MockClientProxy.Object);
			_ = MockClientProxy.Setup(proxy => proxy
				.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask)
				.Callback<string, object[], CancellationToken>((method, args, token) =>
					HubMessages.Add(new HubMessage(method, args)));

			MockMissionProgressHubContext = new Mock<IHubContext<MissionProgressHub>>();
			_ = MockMissionProgressHubContext.Setup(x => x.Clients).Returns(mockClients.Object);

			MockStreakProgressHubContext = new Mock<IHubContext<StreakProgressHub>>();
			_ = MockStreakProgressHubContext.Setup(x => x.Clients).Returns(mockClients.Object);

			var userProxies = new Dictionary<string, Mock<IClientProxy>>();
			_ = mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns((string userId) => {
				if (!userProxies.ContainsKey(userId)) userProxies[userId] = MockClientProxy;
				return userProxies[userId].Object;
			});
			MockSideEffectQueue = new Mock<ISideEffectQueue>();
		} finally {
			_ = semaphore.Release();
		}
	}

	protected TestScope GetTestScope() => new(Factory, TenantProvider);
}
