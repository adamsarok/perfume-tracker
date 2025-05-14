using System;
using Carter;
using PerfumeTracker.Server.Features.UserProfiles;
using PerfumeTracker.Server.Repo;

namespace PerfumeTracker.Server.API;

public class PerfumeSuggestedModule : ICarterModule {
    public void AddRoutes(IEndpointRouteBuilder app) {
        app.MapGet("/api/perfumesuggesteds", async (PerfumeSuggestedRepo perfumeRepo, GetUserProfile getUserProfile) => {
			var settings = await getUserProfile.HandleAsync();
			return await perfumeRepo.GetPerfumeSuggestion(settings.DayFilter, settings.MinimumRating);
			})
            .WithTags("PerfumeSuggesteds")
            .WithName("GetPerfumeSuggestion");
    }
}
