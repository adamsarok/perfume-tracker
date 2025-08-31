using PerfumeTracker.Server.Services.Auth;
using PerfumeTracker.Server.Services.Common;

namespace PerfumeTracker.Server.Features.PerfumeEvents;

public record GetWornPerfumesQuery(int? Cursor, int PageSize) : IQuery<List<PerfumeEventDownloadDto>>;
public class GetWornPerfumesEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/perfume-events/worn-perfumes", async (int? cursor, int pageSize, ISender sender, CancellationToken cancellationToken) => {
			return await sender.Send(new GetWornPerfumesQuery(cursor, pageSize), cancellationToken);
		})
		.WithTags("PerfumeWorns")
		.WithName("GetPerfumeWorns")
		.RequireAuthorization(Policies.READ);
	}
}
public class GetWornPerfumesHandler(PerfumeTrackerContext context, IPresignedUrlService presignedUrlService)
	: IQueryHandler<GetWornPerfumesQuery, List<PerfumeEventDownloadDto>> {
	public async Task<List<PerfumeEventDownloadDto>> Handle(GetWornPerfumesQuery request, CancellationToken cancellationToken) {
		var query = context.PerfumeEvents.Where(x => x.Type == PerfumeEvent.PerfumeEventType.Worn);
		
		if (request.Cursor.HasValue && request.Cursor > 0) {
			query = query.Where(x => x.SequenceNumber < request.Cursor.Value);
		}

		return await query
			.OrderByDescending(x => x.SequenceNumber)
			.Take(request.PageSize)
			.Select(x => new PerfumeEventDownloadDto(
				x.Id,
				x.EventDate,
				x.Perfume.Id,
				x.Perfume.ImageObjectKeyNew,
				presignedUrlService.GetUrl(x.Perfume.ImageObjectKeyNew, Amazon.S3.HttpVerb.GET) != null
					? presignedUrlService.GetUrl(x.Perfume.ImageObjectKeyNew, Amazon.S3.HttpVerb.GET)!.ToString()
					: "",
				x.Perfume.House,
				x.Perfume.PerfumeName,
				x.Perfume.PerfumeTags.Select(x => x.Tag.Adapt<TagDto>()).ToList(),
				x.SequenceNumber,
				x.IsDeleted
			))
			.ToListAsync(cancellationToken);
	}
}