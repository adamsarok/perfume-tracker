using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.Perfumes;

public class GetPerfumeRecommendations {
	public record GetPerfumeRecommendationsQuery(int Count) : IQuery<IEnumerable<PerfumeRecommendationDto>>;
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

	public class GetPerfumeRecommendationsHandler(IPerfumeRecommender perfumeRecommender) : IQueryHandler<GetPerfumeRecommendationsQuery, IEnumerable<PerfumeRecommendationDto>> {
		public async Task<IEnumerable<PerfumeRecommendationDto>> Handle(GetPerfumeRecommendationsQuery request, CancellationToken cancellationToken) {
			// warning just for testing

			var strategyCnt = Enum.GetValues<RecommendationStrategy>().Length;
			int cntPerStrategy = (int)Math.Ceiling((double)request.Count / strategyCnt);
			var recommendations = new List<PerfumeRecommendationDto>();
			foreach (var strategy in Enum.GetValues<RecommendationStrategy>()) {
				var recs = await perfumeRecommender.GetRecommendationsForStrategy(strategy, cntPerStrategy, cancellationToken);
				recommendations.AddRange(recs.Select(x => new PerfumeRecommendationDto(x, strategy)));
			}
			var dedup = recommendations
				.GroupBy(x => x.perfume.Perfume.Id)
				.Select(g => g.First())
				.ToList();
			return dedup
				.OrderBy(_ => Random.Shared.Next())
				.Take(request.Count);
		}
	}
}
