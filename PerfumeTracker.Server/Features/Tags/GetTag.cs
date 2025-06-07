
namespace PerfumeTracker.Server.Features.Tags;
public record GetTagQuery(Guid Id) : IQuery<TagDto>;
public class GetTagEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/tags/{id}", async (Guid id, ISender sender) =>
				await sender.Send(new GetTagQuery(id)))
				.WithTags("Tags")
				.WithName("GetTag")
				.RequireAuthorization(Policies.READ);
	}
}
public class GetTagHandler(PerfumeTrackerContext context) : IQueryHandler<GetTagQuery, TagDto> {
	public async Task<TagDto> Handle(GetTagQuery request, CancellationToken cancellationToken) {
		var t = await context
			.Tags
			.FindAsync(request.Id);
		if (t == null) throw new NotFoundException();
		var r = t.Adapt<TagDto>();
		if (r == null) throw new MappingException();
		return r;
	}
}