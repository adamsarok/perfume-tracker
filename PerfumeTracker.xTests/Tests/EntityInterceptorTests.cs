using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("EntityInterceptor Tests")]
public class EntityInterceptorCollection : ICollectionFixture<EntityInterceptorFixture>;

public class EntityInterceptorFixture : DbFixture {
	public EntityInterceptorFixture() : base() { }
	public async override Task SeedTestData(PerfumeTrackerContext context) {
		await SeedPerfumes(1);
	}
}

[Collection("EntityInterceptor Tests")]
public class EntityInterceptorTests {
	private readonly EntityInterceptorFixture _fixture;
	public EntityInterceptorTests(EntityInterceptorFixture fixture) => _fixture = fixture;

	[Fact]
	public async Task AddEntity_SetsCreatedAtAndUpdatedAt() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var before = DateTime.UtcNow.AddSeconds(-1);
		var tag = new Tag {
			Id = Guid.NewGuid(),
			TagName = "InterceptorTest_" + Guid.NewGuid().ToString()[..8],
			Color = "#123456",
		};
		context.Tags.Add(tag);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		var after = DateTime.UtcNow.AddSeconds(1);

		Assert.InRange(tag.CreatedAt, before, after);
		Assert.InRange(tag.UpdatedAt, before, after);
	}

	[Fact]
	public async Task AddEntity_SetsUserIdOnIUserEntity() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var expectedUserId = _fixture.TenantProvider.MockTenantId!.Value;

		var tag = new Tag {
			Id = Guid.NewGuid(),
			TagName = "InterceptorUserTest_" + Guid.NewGuid().ToString()[..8],
			Color = "#654321",
		};
		context.Tags.Add(tag);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		Assert.Equal(expectedUserId, tag.UserId);
	}

	[Fact]
	public async Task ModifyEntity_UpdatesUpdatedAt() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var perfume = await context.Perfumes.FirstAsync(TestContext.Current.CancellationToken);
		var originalUpdatedAt = perfume.UpdatedAt;

		await Task.Delay(50, TestContext.Current.CancellationToken);
		perfume.PerfumeName = "Modified_" + Guid.NewGuid().ToString()[..8];
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		Assert.True(perfume.UpdatedAt >= originalUpdatedAt);
	}
}
