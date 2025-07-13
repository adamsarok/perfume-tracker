using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PerfumeTracker.Server.Features.Auth;
using PerfumeTracker.Server.Features.Outbox;
using PerfumeTracker.Server.Features.Users;
using System;
using Xunit.Sdk;
using static PerfumeTracker.Server.Features.Missions.ProgressMissions;
using static PerfumeTracker.Server.Features.Streaks.ProgressStreaks;

namespace PerfumeTracker.xTests;
public class TestBase : IClassFixture<WebApplicationFactory<Program>> {
	protected WebApplicationFactory<Program> Factory;
	protected bool DbUp = false;
	private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
	protected MockTenantProvider TenantProvider = new MockTenantProvider();
	protected Mock<IHubContext<MissionProgressHub>> MockMissionProgressHubContext = default!;
	protected Mock<IHubContext<StreakProgressHub>> MockStreakProgressHubContext = default!;
	protected Mock<IClientProxy> MockClientProxy = default!;
	protected List<HubMessage> HubMessages = new List<HubMessage>();
	protected Mock<ISideEffectQueue> MockSideEffectQueue = default!;
	public record HubMessage(string HubSentMethod, object[] HubSentArgs);
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

			var mockClients = new Mock<IHubClients>();
			MockClientProxy = new Mock<IClientProxy>();
			mockClients.Setup(clients => clients.All).Returns(MockClientProxy.Object);
			MockClientProxy
				.Setup(proxy => proxy.SendCoreAsync(
					It.IsAny<string>(),
					It.IsAny<object[]>(),
					It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask)
				.Callback<string, object[], CancellationToken>((method, args, token) => {
					HubMessages.Add(new HubMessage(method, args));
				});

			MockMissionProgressHubContext = new Mock<IHubContext<MissionProgressHub>>();
			MockMissionProgressHubContext.Setup(x => x.Clients).Returns(mockClients.Object);

			MockStreakProgressHubContext = new Mock<IHubContext<StreakProgressHub>>();
			MockStreakProgressHubContext.Setup(x => x.Clients).Returns(mockClients.Object);

			var userProxies = new Dictionary<string, Mock<IClientProxy>>();
			mockClients.Setup(clients => clients.User(It.IsAny<string>()))
				.Returns((string userId) => {
					if (!userProxies.ContainsKey(userId)) {
						userProxies[userId] = MockClientProxy;
					}
					return userProxies[userId].Object;
				});
			MockSideEffectQueue = new Mock<ISideEffectQueue>();
		} finally {
			semaphore.Release();
		}
	}
	protected TestScope GetTestScope() => new TestScope(Factory, TenantProvider);
}
