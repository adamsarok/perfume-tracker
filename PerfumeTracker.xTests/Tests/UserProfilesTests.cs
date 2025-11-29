using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.UserProfiles;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("UserProfiles Tests")]
public class UserProfilesCollection : ICollectionFixture<UserProfilesFixture>;

public class UserProfilesFixture : DbFixture {
	public UserProfilesFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		await Task.CompletedTask;
	}
}

[Collection("UserProfiles Tests")]
public class UserProfilesTests {
	private readonly UserProfilesFixture _fixture;

	public UserProfilesTests(UserProfilesFixture fixture) {
		_fixture = fixture;
	}

	[Fact]
	public async Task GetUserProfiles() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		
		var handler = new GetUserProfileHandler(context);
		var result = await handler.Handle(new GetUserProfileQuery(), CancellationToken.None);
		Assert.NotNull(result);
	}

	[Fact]
	public async Task UpsertUserProfile() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		
		var queryHandler = new GetUserProfileHandler(context);
		var existing = await queryHandler.Handle(new GetUserProfileQuery(), CancellationToken.None);
		var handler = new UpsertUserProfileHandler(context);
		existing.ShowFemalePerfumes = false;
		var result = await handler.Handle(new UpsertUserProfileCommand(existing), CancellationToken.None);
		Assert.NotNull(result);
		Assert.Equal(existing.ShowFemalePerfumes, result.ShowFemalePerfumes);
	}
}

