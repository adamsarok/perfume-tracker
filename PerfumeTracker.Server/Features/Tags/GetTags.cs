
namespace PerfumeTracker.Server.Features.Tags;
public record GetTagsQuery() : IQuery<List<TagDto>>;
public class GetTagsEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/tags", async (ISender sender) =>
			await sender.Send(new GetTagsQuery()))
			.WithTags("Tags")
			.WithName("GetTags")
			.RequireAuthorization(Policies.READ);
	}
}
public class GetTagsHandler(PerfumeTrackerContext context) : IQueryHandler<GetTagsQuery, List<TagDto>> {
	public async Task<List<TagDto>> Handle(GetTagsQuery request, CancellationToken cancellationToken) {
		return await context
			.Tags
			.ProjectToType<TagDto>()
			.ToListAsync();
	}
}