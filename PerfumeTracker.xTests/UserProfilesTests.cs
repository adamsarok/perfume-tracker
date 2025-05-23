using System;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Models;

namespace PerfumeTracker.xTests;

public class UserProfilesTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>> {
    static bool dbUp = false;
    private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
    private async Task PrepareData() {
        await semaphore.WaitAsync();
        try {
            if (!dbUp) {
                using var scope = factory.Services.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
                if (!context.Database.GetDbConnection().Database.ToLower().Contains("test")) throw new Exception("Live database connected!");
                var sql = "truncate table \"public\".\"UserProfiles\" cascade";
                await context.Database.ExecuteSqlRawAsync(sql);
                context.UserProfiles.AddRange(userProfilesSeed);
                await context.SaveChangesAsync();
                dbUp = true;
            }
        }
        finally {
            semaphore.Release();
        }
    }

    static List<UserProfile> userProfilesSeed = new List<UserProfile> {
        new UserProfile {
			UserId = PerfumeTrackerContext.DEFAULT_USERID,
			UserName = PerfumeTrackerContext.DEFAULT_USERID,
			Email = "",
            MinimumRating = 8f,
            DayFilter = 30,
            ShowMalePerfumes = true,
            ShowUnisexPerfumes = true,
            ShowFemalePerfumes = true,
            SprayAmountFullSizeMl = 0.2m,
			SprayAmountSamplesMl = 0.1m
		}
    };

    [Fact]
    public async Task GetUserProfiles() {
        await PrepareData();
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/user-profiles");
        response.EnsureSuccessStatusCode();
        var tags = await response.Content.ReadFromJsonAsync<UserProfile>();
        Assert.NotNull(tags);
    }

	//  [Fact] since the user profile is seeded, this test is not needed - add when multi-user is implemented
	//  public async Task InsertUserProfiles() {
	//      await PrepareData();
	//      var client = factory.CreateClient();
	//      var setting = new UserProfile() {
	//          MinimumRating = 8f,
	//          DayFilter = 30,
	//          ShowMalePerfumes = true,
	//          ShowUnisexPerfumes = true,
	//          ShowFemalePerfumes = true,
	//	SprayAmountFullSizeMl = 0.2m,
	//	SprayAmountSamplesMl = 0.1m
	//};
	//      var content = JsonContent.Create(setting);
	//      var response = await client.PutAsync($"/api/user-profiles", content);
	//      response.EnsureSuccessStatusCode();

	//      var result = await response.Content.ReadFromJsonAsync<UserProfile>();
	//      Assert.NotNull(result);
	//  }

	[Fact]
    public async Task UpdateUserProfiles() {
        await PrepareData();
        var client = factory.CreateClient();
        userProfilesSeed[0].MinimumRating = 9f;
        var content = JsonContent.Create(userProfilesSeed[0]);
        var response = await client.PutAsync($"/api/user-profiles", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<UserProfile>();
        Assert.NotNull(result);
        Assert.Equal(9f, result.MinimumRating);
    }
}

