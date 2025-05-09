using System;
using Carter;
using PerfumeTracker.Server.Models;
using PerfumeTracker.Server.Repo;

namespace PerfumeTracker.Server.API;

public class SettingsModule : ICarterModule {
    public void AddRoutes(IEndpointRouteBuilder app) {
        app.MapGet("/api/settings", async (SettingsRepo repo) => 
            await repo.GetSettingsOrDefault("DEFAULT")) //TODO userID should come from auth token
			.WithTags("Settings")
            .WithName("GetSettings");

        app.MapPut("/api/settings", async (Settings settings, SettingsRepo repo) =>
            await repo.UpsertSettings(settings))
            .WithTags("Settings")
            .WithName("UpsertSettings");
    }
}
