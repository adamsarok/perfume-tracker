using PerfumeTracker.Server.Features.UserProfiles;
using PerfumeTracker.Server.Models;
using PerfumeTracker.Server.Services.Auth;
using PerfumeTracker.Server.Services.Common;

namespace PerfumeTracker.Server.Features.Perfumes;
public record GetPerfumesWithWornQuery(string? FullText = null) : IQuery<List<PerfumeWithWornStatsDto>>;
public class GetPerfumesWithWornEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/perfumes/fulltext/{fulltext}", async (string fulltext, ISender sender) =>
			await sender.Send(new GetPerfumesWithWornQuery(fulltext)))
			.WithTags("Perfumes")
			.WithName("GetPerfumesFulltext")
			.RequireAuthorization(Policies.READ);
		app.MapGet("/api/perfumes", async (ISender sender) =>
			await sender.Send(new GetPerfumesWithWornQuery()))
			.WithTags("Perfumes")
			.WithName("GetPerfumes")
			.RequireAuthorization(Policies.READ);

	}
}

public class GetPerfumesWithWornHandler(PerfumeTrackerContext context, IPresignedUrlService presignedUrlService)
	: IQueryHandler<GetPerfumesWithWornQuery, List<PerfumeWithWornStatsDto>> {
	public async Task<List<PerfumeWithWornStatsDto>> Handle(GetPerfumesWithWornQuery request, CancellationToken cancellationToken) {
		var settings = await context.UserProfiles.FirstAsync(cancellationToken);
		var normalized = request.FullText?.Trim();
		var hasQuery = !string.IsNullOrWhiteSpace(normalized);
		var tsQuery = hasQuery
			? string.Join(" & ", normalized!
				.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.Select(t => $"{t}:*"))
			: "";
		return await context
			.Perfumes
			.Include(x => x.PerfumeEvents)
			.Include(x => x.PerfumeTags)
			.ThenInclude(x => x.Tag)
			.Include(x => x.PerfumeRatings)
			.Where(p => !hasQuery
				|| p.FullText.Matches(EF.Functions.ToTsQuery(tsQuery))
				|| p.PerfumeTags.Any(pt
					 => EF.Functions.ILike(pt.Tag.TagName, $"%{normalized}%"))
			)
			.Select(p => p.ToPerfumeWithWornStatsDto(settings, presignedUrlService))
			.AsSplitQuery()
			.AsNoTracking()
			.ToListAsync();
	}
}
