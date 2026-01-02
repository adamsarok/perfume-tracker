using PerfumeTracker.Server.Features.Auth;
using PerfumeTracker.Server.Features.Common;
using PerfumeTracker.Server.Features.Perfumes.Services;

namespace PerfumeTracker.Server.Features.Perfumes;

public class GetSimilarPerfumes {
	public record GetSimilarPerfumesQuery(Guid PerfumeId, int Count) : IQuery<GetSimilarPerfumesResponse>;
	public record GetSimilarPerfumesResponse(IEnumerable<PerfumeWithWornStatsDto> Perfumes);
	public class GetSimilarPerfumesQueryValidator : AbstractValidator<GetSimilarPerfumesQuery> {
		public GetSimilarPerfumesQueryValidator() {
			RuleFor(x => x.PerfumeId).NotEmpty();
			RuleFor(x => x.Count).InclusiveBetween(1, 20);
		}
	}
	public class GetSimilarPerfumesEndpoint : ICarterModule {
		public void AddRoutes(IEndpointRouteBuilder app) {
			app.MapGet("/api/perfumes/similar-perfumes/{perfumeId}/{count}", async (Guid perfumeId, int count, ISender sender, CancellationToken cancellationToken) => {

				var result = await sender.Send(new GetSimilarPerfumesQuery(perfumeId, count), cancellationToken);
				return Results.Ok(result);
			})
			.WithTags("Ai")
			.WithName("GetSimilarPerfumes")
			.RequireAuthorization(Policies.WRITE);
		}
	}

	public class GetSimilarPerfumesHandler(IPerfumeRecommender perfumeRecommender, IUserProfileService userProfileService) : IQueryHandler<GetSimilarPerfumesQuery, GetSimilarPerfumesResponse> {
		public async Task<GetSimilarPerfumesResponse> Handle(GetSimilarPerfumesQuery request, CancellationToken cancellationToken) {
			var userProfile = await userProfileService.GetCurrentUserProfile(cancellationToken);
			var perfumes = await perfumeRecommender.GetSimilar(request.PerfumeId, request.Count, userProfile, cancellationToken);
			return new GetSimilarPerfumesResponse(perfumes);
		}
	}
}
