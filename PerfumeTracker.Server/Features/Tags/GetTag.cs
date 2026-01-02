
using PerfumeTracker.Server.Features.Auth;

namespace PerfumeTracker.Server.Features.Tags;

public record GetTagQuery(Guid Id) : IQuery<TagDto>;
public class GetTagEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/tags/{id}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
				await sender.Send(new GetTagQuery(id), cancellationToken))
				.WithTags("Tags")
				.WithName("GetTag")
				.RequireAuthorization(Policies.READ);
	}
}
public class GetTagHandler(PerfumeTrackerContext context) : IQueryHandler<GetTagQuery, TagDto> {
	public async Task<TagDto> Handle(GetTagQuery request, CancellationToken cancellationToken) {
		var t = await context
			.Tags
			.FindAsync([request.Id], cancellationToken) ?? throw new NotFoundException("Tags", request.Id);
		var r = t.Adapt<TagDto>();
		if (r == null) throw new MappingException();
		return r;
	}
}