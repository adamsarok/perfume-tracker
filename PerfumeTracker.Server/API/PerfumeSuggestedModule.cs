using System;
using Carter;
using PerfumeTracker.Server.Repo;

namespace PerfumeTracker.Server.API;

public class PerfumeSuggestedModule : ICarterModule {
    public void AddRoutes(IEndpointRouteBuilder app) {
        app.MapGet("/api/perfumesuggesteds", async (PerfumeSuggestedRepo perfumeRepo, UserProfilesRepo userProfilesRepo) => {
			var settings = await userProfilesRepo.GetUserProfileOrDefault();
			return await perfumeRepo.GetPerfumeSuggestion(settings.DayFilter, settings.MinimumRating);
			})
            .WithTags("PerfumeSuggesteds")
            .WithName("GetPerfumeSuggestion");
    }
}
