using PerfumeTracker.Server.DTO;
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.GlobalPerfumes;

public record SearchGlobalPerfumesQuery(string SearchText) : IQuery<List<GlobalPerfumeDto>>;
public class SearchGlobalPerfumesEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/global-perfumes/search/{searchText}",
			async (string searchText, ISender sender, CancellationToken cancellationToken) =>
				await sender.Send(new SearchGlobalPerfumesQuery(searchText), cancellationToken))
			.WithTags("GlobalPerfumes")
			.WithName("SearchGlobalPerfumes")
			.RequireAuthorization(Policies.READ);
	}
}

public class SearchGlobalPerfumesHandler(PerfumeTrackerContext context)
	: IQueryHandler<SearchGlobalPerfumesQuery, List<GlobalPerfumeDto>> {

	public async Task<List<GlobalPerfumeDto>> Handle(SearchGlobalPerfumesQuery request, CancellationToken cancellationToken) {
		var normalized = request.SearchText.Trim();
		var tsQuery = string.Join(" & ", normalized
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Select(t => $"{t}:*"));

		var results = await context.GlobalPerfumes
			.Include(x => x.GlobalPerfumeTags)
			.ThenInclude(x => x.GlobalTag)
			.Where(p => p.FullText.Matches(EF.Functions.ToTsQuery(tsQuery)))
			.OrderBy(p => p.House)
			.ThenBy(p => p.PerfumeName)
			.Take(50)
			.AsNoTracking()
			.ToListAsync(cancellationToken);

		return results.Select(p => new GlobalPerfumeDto(
			p.Id,
			p.House,
			p.PerfumeName,
			p.Family,
			p.GlobalPerfumeTags.Select(pt => new TagDto(
				pt.GlobalTag.TagName,
				pt.GlobalTag.Color,
				pt.GlobalTag.Id,
				pt.GlobalTag.IsDeleted
			)).ToList()
		)).ToList();
	}
}