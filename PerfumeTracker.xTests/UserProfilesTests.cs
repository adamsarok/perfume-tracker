using System;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.UserProfiles;
using PerfumeTracker.Server.Models;

namespace PerfumeTracker.xTests;

public class UserProfilesTests : TestBase, IClassFixture<WebApplicationFactory<Program>> {
	public UserProfilesTests(WebApplicationFactory<Program> factory) : base(factory) { }
    [Fact]
    public async Task GetUserProfiles() {
		using var scope = GetTestScope();
		var handler = new GetUserProfileHandler(scope.PerfumeTrackerContext);
		var result = await handler.Handle(new GetUserProfileQuery(), CancellationToken.None);
        Assert.NotNull(result);
    }
}

