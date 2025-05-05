using System;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Models;

namespace PerfumeTracker.xTests;

public class SettingsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>> {
    static bool dbUp = false;
    private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
    private async Task PrepareData() {          //fixtures don't have DI
        await semaphore.WaitAsync();
        try {
            if (!dbUp) {
                using var scope = factory.Services.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<PerfumetrackerContext>();
                if (!context.Database.GetDbConnection().Database.ToLower().Contains("test")) throw new Exception("Live database connected!");
                var sql = "truncate table \"public\".\"Settings\" cascade";
                await context.Database.ExecuteSqlRawAsync(sql);
                context.Settings.AddRange(settingsSeed);
                await context.SaveChangesAsync();
                dbUp = true;
            }
        }
        finally {
            semaphore.Release();
        }
    }

    static List<Settings> settingsSeed = new List<Settings> {
        new Settings {
            UserId = "User1",
            MinimumRating = 8f,
            DayFilter = 30,
            ShowMalePerfumes = true,
            ShowUnisexPerfumes = true,
            ShowFemalePerfumes = true,
            SprayAmountFullSizeMl = 0.2m,
			SprayAmountSamplesMl = 0.1m
		},
            new Settings {
            UserId = "User2",
            MinimumRating = 8f,
            DayFilter = 30,
            ShowMalePerfumes = true,
            ShowUnisexPerfumes = true,
            ShowFemalePerfumes = true,
			SprayAmountFullSizeMl = 0.2m,
			SprayAmountSamplesMl = 0.1m
		},
    };

    [Fact]
    public async Task GetSettings() {
        await PrepareData();
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/settings");
        response.EnsureSuccessStatusCode();
        var tags = await response.Content.ReadFromJsonAsync<Settings>();
        Assert.NotNull(tags);
    }

    [Fact]
    public async Task InsertSettings() {
        await PrepareData();
        var client = factory.CreateClient();
        var setting = new Settings() {
            UserId = "User999",
            MinimumRating = 8f,
            DayFilter = 30,
            ShowMalePerfumes = true,
            ShowUnisexPerfumes = true,
            ShowFemalePerfumes = true,
			SprayAmountFullSizeMl = 0.2m,
			SprayAmountSamplesMl = 0.1m
		};
        var content = JsonContent.Create(setting);
        var response = await client.PutAsync($"/api/settings", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Settings>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateTag() {
        await PrepareData();
        var client = factory.CreateClient();
        settingsSeed[1].MinimumRating = 9f;
        var content = JsonContent.Create(settingsSeed[1]);
        var response = await client.PutAsync($"/api/settings", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Settings>();
        Assert.NotNull(result);
        Assert.Equal(9f, result.MinimumRating);
    }
}

