namespace PerfumeTracker.Server.Features.Perfumes;
public record GetPerfumeQuery(int Id) : IQuery<PerfumeWithWornStatsDto>;
public class GetPerfumeEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/perfumes/{id}", async (int id, ISender sender) => {
			return await sender.Send(new GetPerfumeQuery(id));
		})
			.WithTags("Perfumes")
			.WithName("GetPerfume");
	}
}
public class GetPerfumeHandler(PerfumeTrackerContext context, GetUserProfile getUserProfile)
		: IQueryHandler<GetPerfumeQuery, PerfumeWithWornStatsDto> {
	public async Task<PerfumeWithWornStatsDto> Handle(GetPerfumeQuery request, CancellationToken cancellationToken) {
		var settings = await getUserProfile.HandleAsync();
		var p = await context
			.Perfumes
			.Include(x => x.PerfumeEvents)
			.Include(x => x.PerfumeTags)
			.ThenInclude(x => x.Tag)
			.Where(p => p.Id == request.Id)
			.Select(p => PerfumesCommon.ToPerfumeWithWornStatsDto(p, settings))
			.AsSplitQuery()
			.AsNoTracking()
			.FirstOrDefaultAsync();
		return p ?? throw new NotFoundException();
	}
}