using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.Perfumes;

public class GetPerfumeRecommendations {
	public record GetPerfumeRecommendationsQuery(int Count, string? OccasionOrMood) : IQuery<IEnumerable<PerfumeRecommendationDto>>;
	public record PerfumeRecommendationDto(Guid RecommendationId, PerfumeWithWornStatsDto Perfume, RecommendationStrategy Strategy);
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
			if (string.IsNullOrWhiteSpace(request.OccasionOrMood)) return await perfumeRecommender.GetAllStrategyRecommendations(request.Count, cancellationToken);
			return await perfumeRecommender.GetRecommendationsForOccasionMoodPrompt(request.Count, request.OccasionOrMood, cancellationToken);
		}
	}
}
