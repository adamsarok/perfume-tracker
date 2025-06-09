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
	static bool dbUp = false;
    private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
    private async Task PrepareData() {
        await semaphore.WaitAsync();
        try {
            if (!dbUp) {
			  //using var scope = GetTestScope();
              dbUp = true;
			}
		} finally {
			semaphore.Release();
		}
	}

    [Fact]
    public async Task GetUserProfiles() {
        await PrepareData();
		using var scope = GetTestScope();
		var handler = new GetUserProfileHandler(scope.PerfumeTrackerContext);
		var result = await handler.Handle(new GetUserProfileQuery(), new CancellationToken());
        Assert.NotNull(result);
    }
}

