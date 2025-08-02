using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.PerfumeRatings;
public record GetPerfumeRatingQuery(Guid PerfumeId) : IQuery<List<PerfumeRatingDownloadDto>>;
public class GetPerfumeRatingEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/perfumes/{perfumeId}/ratings", async (ISender sender, Guid perfumeId) => {
			var result = await sender.Send(new GetPerfumeRatingQuery(perfumeId));
			return Results.Ok(result);
		}).WithTags("PerfumeRatings")
			.WithName("GetPerfumeRatings")
			.RequireAuthorization(Policies.READ);
	}
}
public class GetPerfumeRatingHandler(PerfumeTrackerContext context) : IQueryHandler<GetPerfumeRatingQuery, List<PerfumeRatingDownloadDto>> {
	public async Task<List<PerfumeRatingDownloadDto>> Handle(GetPerfumeRatingQuery request, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		var ratings = await context.PerfumeRatings.Where(x => x.PerfumeId == request.PerfumeId).ToListAsync();
		return ratings.Adapt<List<PerfumeRatingDownloadDto>>();
	}
}