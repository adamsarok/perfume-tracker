using Microsoft.AspNetCore.Mvc.Testing;
using PerfumeTracker.Server.Features.UserProfiles;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

public class UserProfilesTests : TestBase, IClassFixture<WebApplicationFactory<Program>> {
	public UserProfilesTests(WebApplicationFactory<Program> factory) : base(factory) { }
    [Fact]
    public async Task GetUserProfiles() {
		using var scope = GetTestScope();
		var handler = new GetUserProfileHandler(scope.PerfumeTrackerContext);
		var result = await handler.Handle(new GetUserProfileQuery(), CancellationToken.None);
        Assert.NotNull(result);
    }

	[Fact]
	public async Task UpsertUserProfile() {
		using var scope = GetTestScope();
		var queryHandler = new GetUserProfileHandler(scope.PerfumeTrackerContext);
		var existing = await queryHandler.Handle(new GetUserProfileQuery(), CancellationToken.None);
		var handler = new UpsertUserProfileHandler(scope.PerfumeTrackerContext);
		existing.ShowFemalePerfumes = false;
		var result = await handler.Handle(new UpsertUserProfileCommand(existing), CancellationToken.None);
		Assert.NotNull(result);
		Assert.Equal(existing.ShowFemalePerfumes, result.ShowFemalePerfumes);
	}
}

