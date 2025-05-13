using System;
using Carter;
using PerfumeTracker.Server.Models;
using PerfumeTracker.Server.Repo;

namespace PerfumeTracker.Server.API;

public class SettingsModule : ICarterModule {
    public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/settings", async (UserProfilesRepo repo) =>
			await repo.GetUserProfileOrDefault())
			.WithTags("UserProfiles")
            .WithName("GetUserProfile");

        app.MapPut("/api/settings", async (UserProfile userProfile, UserProfilesRepo repo) =>
            await repo.UpsertUserProfile(userProfile))
            .WithTags("UserProfiles")
            .WithName("UpsertUserProfile");
    }
}
