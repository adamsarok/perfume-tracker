using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.Perfumes;

public class GetPerfumeRecommendations {
	public record GetPerfumeRecommendationsQuery(int Count) : IQuery<GetPerfumeRecommendationsResponse>;
	public record GetPerfumeRecommendationsResponse(IEnumerable<PerfumeRecommendationDto> PerfumeRecommendations);
	public record PerfumeRecommendationDto(PerfumeWithWornStatsDto perfume, RecommendationStrategy strategy);
	public class GetPerfumeRecommendationsQueryValidator : AbstractValidator<GetPerfumeRecommendationsQuery> {
		public GetPerfumeRecommendationsQueryValidator() {
			RuleFor(x => x.Count).InclusiveBetween(1, 20);
		}
	}
	public class GetPerfumeRecommendationsEndpoint : ICarterModule {
		public void AddRoutes(IEndpointRouteBuilder app) {
			app.MapGet("/api/perfumes/recommendations/{count}", async (int count, ISender sender, CancellationToken cancellationToken) => {
				var result = await sender.Send(new GetPerfumeRecommendationsQuery(count), cancellationToken);
				return Results.Ok(result);
			})
			.WithTags("Perfumes")
			.WithName("GetPerfumeRecommendations")
			.RequireAuthorization(Policies.READ);
		}
	}

	public class GetPerfumeRecommendationsHandler(IPerfumeRecommender perfumeRecommender) : IQueryHandler<GetPerfumeRecommendationsQuery, GetPerfumeRecommendationsResponse> {
		public async Task<GetPerfumeRecommendationsResponse> Handle(GetPerfumeRecommendationsQuery request, CancellationToken cancellationToken) {
			var strategyCnt = Enum.GetValues<RecommendationStrategy>().Length;
			int cntPerStrategy = (int)Math.Ceiling((double)request.Count / strategyCnt);
			var recommendations = new List<PerfumeRecommendationDto>();
			foreach (var strategy in Enum.GetValues<RecommendationStrategy>()) {
				var recs = await perfumeRecommender.GetRecommendationsForStrategy(strategy, cntPerStrategy, cancellationToken);
				recommendations.AddRange(recs.Select(x => new PerfumeRecommendationDto(x, strategy)));
			}
			var result = recommendations
				.OrderBy(_ => Random.Shared.Next())
				.Take(request.Count);
			return new GetPerfumeRecommendationsResponse(result);
		}
	}
}
