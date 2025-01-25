using System;
using Carter;
using PerfumeTracker.Server.DTO;
using PerfumeTracker.Server.Repo;
using static PerfumeTracker.Server.Repo.ResultType;

namespace PerfumeTracker.Server.API;

public class RecommendationModule : ICarterModule {
    public void AddRoutes(IEndpointRouteBuilder app) {
        //TODO: the difference between perfume recommandation and perfume suggestion is unclear and misleading

        app.MapGet("/api/recommendations", async (int dayFilter, RecommendationsRepo repo) =>
            await repo.GetRecommendations())
            .WithTags("Recommendations")
            .WithName("GetRecommendations");

        app.MapPost("/api/recommendations", async (RecommendationUploadDTO dto, RecommendationsRepo repo) => {
            var result = await repo.AddRecommendation(dto);
            return result.ResultType switch {
                ResultTypes.Ok => Results.CreatedAtRoute("GetRecommendations", new { id = result?.Recommendation?.Id }, result?.Recommendation),
                ResultTypes.NotFound => Results.NotFound(),
                ResultTypes.BadRequest => Results.BadRequest(result.ErrorMsg),
                _ => Results.InternalServerError()
            };
        }).WithTags("Recommendations")
             .WithName("AddRecommendation");
    }
}
