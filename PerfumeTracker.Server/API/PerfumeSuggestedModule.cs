using System;
using Carter;
using PerfumeTracker.Server.Repo;

namespace PerfumeTracker.Server.API;

public class PerfumeSuggestedModule : ICarterModule {
    public void AddRoutes(IEndpointRouteBuilder app) {
        app.MapGet("/api/perfumesuggesteds", async (int dayFilter, int minimumRating, PerfumeSuggestedRepo repo) =>
            await repo.GetPerfumeSuggestion(dayFilter, minimumRating))
            .WithTags("PerfumeSuggesteds")
            .WithName("GetPerfumeSuggestion");
    }
}
