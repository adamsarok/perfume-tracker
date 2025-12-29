using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.Perfumes;

public class GetPerfumeRecommendations {
	public record GetPerfumeRecommendationsQuery(int Count, string? OccasionOrMood) : IQuery<IEnumerable<PerfumeRecommendationDto>>;
	public record PerfumeRecommendationDto(PerfumeWithWornStatsDto Perfume, RecommendationStrategy Strategy);
	public class GetPerfumeRecommendationsQueryValidator : AbstractValidator<GetPerfumeRecommendationsQuery> {
		public GetPerfumeRecommendationsQueryValidator() {
			RuleFor(x => x.Count).InclusiveBetween(1, 20);
		}
	}
	public class GetPerfumeRecommendationsEndpoint : ICarterModule {
		public void AddRoutes(IEndpointRouteBuilder app) {
			app.MapGet("/api/perfumes/recommendations/{count}", async (int count, string? occasionOrMood, ISender sender, CancellationToken cancellationToken) => {
				var result = await sender.Send(new GetPerfumeRecommendationsQuery(count, occasionOrMood), cancellationToken);
				return Results.Ok(result);
			})
			.WithTags("Perfumes")
			.WithName("GetPerfumeRecommendations")
			.RequireAuthorization(Policies.READ);
		}
	}

	public class GetPerfumeRecommendationsHandler(IPerfumeRecommender perfumeRecommender) : IQueryHandler<GetPerfumeRecommendationsQuery, IEnumerable<PerfumeRecommendationDto>> {
		public async Task<IEnumerable<PerfumeRecommendationDto>> Handle(GetPerfumeRecommendationsQuery request, CancellationToken cancellationToken) {
			IEnumerable<PerfumeRecommendationDto> recommendations;
			if (string.IsNullOrWhiteSpace(request.OccasionOrMood)) recommendations = await GetAllStrategyRecommendations(request, cancellationToken);
			else recommendations = await perfumeRecommender.GetRecommendationsForOccasionMoodPrompt(request.Count, request.OccasionOrMood, cancellationToken);
			var dedup = recommendations
				.GroupBy(x => x.Perfume.Perfume.Id)
				.Select(g => g.First())
				.ToList();
			return dedup
				.OrderBy(_ => Random.Shared.Next())
				.Take(request.Count);
		}

		private async Task<IEnumerable<PerfumeRecommendationDto>> GetAllStrategyRecommendations(GetPerfumeRecommendationsQuery request, CancellationToken cancellationToken) {
			var validStrategies = Enum.GetValues<RecommendationStrategy>().Where(s => s != RecommendationStrategy.MoodOrOccasion).ToList();
			int cntPerStrategy = (int)Math.Ceiling((double)request.Count / validStrategies.Count);
			var recommendations = new List<PerfumeRecommendationDto>();
			foreach (var strategy in validStrategies) {
				recommendations.AddRange(await perfumeRecommender.GetRecommendationsForStrategy(strategy, cntPerStrategy, cancellationToken));
			}
			return recommendations;
		}
	}
}
