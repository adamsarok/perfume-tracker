using System;
using Carter;
using PerfumeTracker.Server.Dto;
using PerfumeTracker.Server.Repo;

namespace PerfumeTracker.Server.API;

public class RecommendationModule : ICarterModule {
    public void AddRoutes(IEndpointRouteBuilder app) {
        //TODO: the difference between perfume recommandation and perfume suggestion is unclear and misleading

        app.MapGet("/api/recommendations", async (int dayFilter, RecommendationsRepo repo) =>
            await repo.GetRecommendations())
            .WithTags("Recommendations")
            .WithName("GetRecommendations");

        app.MapPost("/api/recommendations", async (RecommendationUploadDto dto, RecommendationsRepo repo) => {
            var result = await repo.AddRecommendation(dto);
			return Results.CreatedAtRoute("GetRecommendations", new { id = result.Id }, result);
        }).WithTags("Recommendations")
             .WithName("AddRecommendation");
    }
}
