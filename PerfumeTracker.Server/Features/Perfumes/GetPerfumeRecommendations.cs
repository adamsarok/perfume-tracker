using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Services.Auth;
using System.ComponentModel.DataAnnotations;

namespace PerfumeTracker.Server.Features.Perfumes;
public class GetPerfumeRecommendations {
	public record GetPerfumeRecommendationsQuery(int Count) : IQuery<GetPerfumeRecommendationsResponse>;
	public record GetPerfumeRecommendationsResponse(IEnumerable<Perfume> Perfumes);
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
			// TODO: strategy should be user selectable?
			//foreach (var strategy in RecommendationStrategy)) {
			//	var perfumes = await perfumeRecommender.GetRecommendationsForStrategy(request.Count, cancellationToken);
			//}
			//return new GetPerfumeRecommendationsResponse(perfumes);
			throw new NotImplementedException();
		}
	}
}
