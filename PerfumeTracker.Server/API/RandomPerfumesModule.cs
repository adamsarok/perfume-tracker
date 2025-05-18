using System;
using Carter;
using PerfumeTracker.Server.Features.UserProfiles;
using PerfumeTracker.Server.Repo;

namespace PerfumeTracker.Server.API;

public class PerfumeRandomsModule : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/random-perfumes", async (RandomPerfumeRepo perfumeRepo, GetUserProfile getUserProfile) => {
			var settings = await getUserProfile.HandleAsync();
			return await perfumeRepo.GetRandomPerfume(settings.DayFilter, settings.MinimumRating);
		})
			.WithTags("PerfumeRandoms")
			.WithName("GetPerfumeSuggestion");
		app.MapPost("/api/random-perfumes/{randomsId}", async (int randomsId, RandomPerfumeRepo perfumeRepo) => {
			await perfumeRepo.AcceptRandomPerfume(randomsId);
			return Results.Created();
		})
			.WithTags("PerfumeRandoms")
			.WithName("AcceptPerfumeSuggestion");
	}
}
