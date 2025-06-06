
namespace PerfumeTracker.Server.Features.PerfumeEvents;

public record GetWornPerfumesQuery(DateTime? Cursor, int PageSize) : IQuery<List<PerfumeEventDownloadDto>>;
public class GetWornPerfumesEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/perfume-events/worn-perfumes", async (string cursor, int pageSize, ISender sender) => {
			if (!string.IsNullOrWhiteSpace(cursor) && DateTime.TryParse(cursor, out DateTime date)) {
				return await sender.Send(new GetWornPerfumesQuery(date, pageSize));
			}
			return await sender.Send(new GetWornPerfumesQuery(null, pageSize));
		})
		.WithTags("PerfumeWorns")
		.WithName("GetPerfumeWorns")
		.RequireAuthorization(Policies.READ);
	}
}
public class GetWornPerfumesHandler(PerfumeTrackerContext context)
	: IQueryHandler<GetWornPerfumesQuery, List<PerfumeEventDownloadDto>> {
	public async Task<List<PerfumeEventDownloadDto>> Handle(GetWornPerfumesQuery request, CancellationToken cancellationToken) {
		return await context
			.PerfumeEvents
			.Where(x => x.Type == PerfumeEvent.PerfumeEventType.Worn && (!request.Cursor.HasValue || x.CreatedAt < request.Cursor.Value))
			.OrderByDescending(x => x.CreatedAt)
			.Take(request.PageSize)
			.Select(x => new PerfumeEventDownloadDto(
				x.Id,
				x.CreatedAt,
				x.Perfume.Id,
				x.Perfume.ImageObjectKey,
				"",
				x.Perfume.House,
				x.Perfume.PerfumeName,
				x.Perfume.PerfumeTags.Select(x => x.Tag.Adapt<TagDto>()).ToList()
			))
			.ToListAsync();
	}
}