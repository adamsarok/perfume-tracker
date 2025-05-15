using PerfumeTracker.Server.Features.UserProfiles;
using PerfumeTracker.Server.Models;

namespace PerfumeTracker.Server.Features.Perfumes;
public record GetPerfumesWithWornQuery(string? FullText = null) : IQuery<List<PerfumeWithWornStatsDto>>;
public class GetPerfumesWithWorn : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/perfumes/fulltext/{fulltext}", async (string fulltext, ISender sender) =>
			await sender.Send(new GetPerfumesWithWornQuery(fulltext)))
			.WithTags("Perfumes")
			.WithName("GetPerfumesFulltext");
		app.MapGet("/api/perfumes", async (ISender sender) =>
			await sender.Send(new GetPerfumesWithWornQuery()))
			.WithTags("Perfumes")
			.WithName("GetPerfumes");
	}
}

public class GetPerfumesWithWornHandler(PerfumeTrackerContext context, GetUserProfile getUserProfile) 
	: IQueryHandler<GetPerfumesWithWornQuery, List<PerfumeWithWornStatsDto>> {
	public async Task<List<PerfumeWithWornStatsDto>> Handle(GetPerfumesWithWornQuery request, CancellationToken cancellationToken) {
		var settings = await getUserProfile.HandleAsync();
		return await context
			.Perfumes
			.Include(x => x.PerfumeEvents)
			.Include(x => x.PerfumeTags)
			.ThenInclude(x => x.Tag)
			.Where(p => string.IsNullOrWhiteSpace(request.FullText)
				|| p.FullText.Matches(EF.Functions.PlainToTsQuery($"{request.FullText}:*"))
				|| p.PerfumeTags.Any(pt => EF.Functions.ILike(pt.Tag.TagName, request.FullText))
				)
			.Select(p => PerfumesCommon.ToPerfumeWithWornStatsDto(p, settings))
			.AsSplitQuery()
			.AsNoTracking()
			.ToListAsync();
	}
}
