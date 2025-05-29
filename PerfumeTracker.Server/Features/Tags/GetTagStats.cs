

namespace PerfumeTracker.Server.Features.Tags;
public record GetTagStatsQuery() : IQuery<List<TagStatDto>>;
public class GetTagStatsEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/tags/stats", async (ISender sender) =>
				await sender.Send(new GetTagsQuery()))
				.WithTags("Tags")
				.WithName("GetTagStats");
	}
}
public class GetTagStatsHandler(PerfumeTrackerContext context) : IQueryHandler<GetTagStatsQuery, List<TagStatDto>> {
	public async Task<List<TagStatDto>> Handle(GetTagStatsQuery request, CancellationToken cancellationToken) {
		return await context
			.Tags
			.Select(x => new TagStatDto(
				x.Id,
				x.TagName,
				x.Color,
				x.PerfumeTags.Sum(pt => pt.Perfume.Ml),
				x.PerfumeTags.Sum(pt => pt.Perfume.PerfumeEvents.Count)
			 )).ToListAsync();
	}
}
