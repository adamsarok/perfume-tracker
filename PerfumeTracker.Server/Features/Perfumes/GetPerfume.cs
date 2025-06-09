namespace PerfumeTracker.Server.Features.Perfumes;
public record GetPerfumeQuery(Guid Id) : IQuery<PerfumeWithWornStatsDto>;
public class GetPerfumeEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/perfumes/{id}", async (Guid id, ISender sender) => {
			return await sender.Send(new GetPerfumeQuery(id));
		})
			.WithTags("Perfumes")
			.WithName("GetPerfume")
			.RequireAuthorization(Policies.READ);
	}
}
public class GetPerfumeHandler(PerfumeTrackerContext context)
		: IQueryHandler<GetPerfumeQuery, PerfumeWithWornStatsDto> {
	public async Task<PerfumeWithWornStatsDto> Handle(GetPerfumeQuery request, CancellationToken cancellationToken) {
		var settings = await context.UserProfiles.FirstAsync(cancellationToken);
		var p = await context
			.Perfumes
			.Include(x => x.PerfumeEvents)
			.Include(x => x.PerfumeTags)
			.ThenInclude(x => x.Tag)
			.Where(p => p.Id == request.Id)
			.Select(p => p.ToPerfumeWithWornStatsDto(settings))
			.AsSplitQuery()
			.AsNoTracking()
			.FirstOrDefaultAsync();
		return p ?? throw new NotFoundException();
	}
}