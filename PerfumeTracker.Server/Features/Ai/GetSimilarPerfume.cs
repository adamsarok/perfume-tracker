

using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.Ai;
public class GetSimilarPerfumes {
	public record GetSimilarPerfumesQuery(Guid PerfumeId) : IQuery<GetSimilarPerfumesResponse>;
	public record GetSimilarPerfumesResponse(IEnumerable<Perfume> Perfumes);

	public class GetSimilarPerfumesEndpoint : ICarterModule {
		public void AddRoutes(IEndpointRouteBuilder app) {
			app.MapGet("/api/ai/similar-perfumes/{perfumeId}", async (Guid perfumeId, ISender sender, CancellationToken cancellationToken) => {
				var result = await sender.Send(new GetSimilarPerfumesQuery(perfumeId), cancellationToken);
				return Results.Ok(result);
			})
			.WithTags("Ai")
			.WithName("GetAiRecommendation")
			.RequireAuthorization(Policies.WRITE);
		}
	}

	public class GetSimilarPerfumesHandler : IQueryHandler<GetSimilarPerfumesQuery, GetSimilarPerfumesResponse> {
		public Task<GetSimilarPerfumesResponse> Handle(GetSimilarPerfumesQuery request, CancellationToken cancellationToken) {
			//1. search embeddings (sentiment, notes) - weighted 80%
			//2. search text (same house, similar name) - weighted 20%
			//3. ?
			throw new NotImplementedException();
		}
	}
}
