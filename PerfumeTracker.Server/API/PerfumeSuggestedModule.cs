using System;
using Carter;
using PerfumeTracker.Server.Repo;

namespace PerfumeTracker.Server.API;

public class PerfumeSuggestedModule : ICarterModule {
    public void AddRoutes(IEndpointRouteBuilder app) {
        app.MapGet("/api/perfumesuggesteds/{dayFilter}", async (int dayFilter, PerfumeSuggestedRepo repo) =>
            await repo.GetPerfumeSuggestion(dayFilter))
            .WithTags("PerfumeSuggesteds")
            .WithName("GetPerfumeSuggestion");
        
        // app.MapPost("/api/perfumesuggesteds/{perfumeId}", async (int perfumeId, PerfumeSuggestedRepo repo) =>
        //     await repo.AddSuggestedPerfume(perfumeId))
        //     .WithTags("PerfumeSuggesteds")
        //     .WithName("AddSuggestedPerfume");
    }
}
