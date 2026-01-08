using PerfumeTracker.Server.Features.Auth;
using PerfumeTracker.Server.Features.Perfumes.Services;

namespace PerfumeTracker.Server.Features.Perfumes;

public class GetPerfumeRecommendations {
	public record GetPerfumeRecommendationsRequest(string? OccasionOrMood, List<RecommendationStrategy>? Strategies);
	public record GetPerfumeRecommendationsQuery(int Count, string? OccasionOrMood, List<RecommendationStrategy>? Strategies) : IQuery<IEnumerable<PerfumeRecommendationDto>>;
	public record PerfumeRecommendationDto(Guid RecommendationId, PerfumeWithWornStatsDto Perfume, RecommendationStrategy Strategy);
	public class GetPerfumeRecommendationsQueryValidator : AbstractValidator<GetPerfumeRecommendationsQuery> {
		public GetPerfumeRecommendationsQueryValidator() {
			RuleFor(x => x.Count).InclusiveBetween(1, 20);
			RuleFor(x => x.Strategies)
				.Must(s => s == null || s.All(strat => strat != RecommendationStrategy.MoodOrOccasion))
				.WithMessage("MoodOrOccasion strategy cannot be manually selected");
		}
	}
	public class GetPerfumeRecommendationsEndpoint : ICarterModule {
		public void AddRoutes(IEndpointRouteBuilder app) {
			app.MapPost("/api/perfumes/recommendations/{count}", async (int count, GetPerfumeRecommendationsRequest request, ISender sender, CancellationToken cancellationToken) => {
				var result = await sender.Send(new GetPerfumeRecommendationsQuery(count, request.OccasionOrMood, request.Strategies), cancellationToken);
				return Results.Ok(result);
			})
			.WithTags("Perfumes")
			.WithName("GetPerfumeRecommendations")
			.RequireAuthorization(Policies.READ);
		}
	}

	public class GetPerfumeRecommendationsHandler(IPerfumeRecommender perfumeRecommender) : IQueryHandler<GetPerfumeRecommendationsQuery, IEnumerable<PerfumeRecommendationDto>> {
		public async Task<IEnumerable<PerfumeRecommendationDto>> Handle(GetPerfumeRecommendationsQuery request, CancellationToken cancellationToken) {
			if (string.IsNullOrWhiteSpace(request.OccasionOrMood)) return await perfumeRecommender.GetAllStrategyRecommendations(request.Count, request.Strategies, cancellationToken);
			return await perfumeRecommender.GetRecommendationsForOccasionMoodPrompt(request.Count, request.OccasionOrMood, cancellationToken);
		}
	}
}
