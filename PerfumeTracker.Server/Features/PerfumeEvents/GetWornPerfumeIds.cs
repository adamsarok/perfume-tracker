
using PerfumeTracker.Server.Features.Auth;

namespace PerfumeTracker.Server.Features.PerfumeEvents;

public record GetWornPerfumeIdsQuery(int DaysFilter) : IQuery<List<Guid>>;
public class GetWornPerfumeIdsEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/perfume-events/worn-perfumes/{daysBefore}", async (int daysBefore, ISender sender, CancellationToken cancellationToken) =>
			await sender.Send(new GetWornPerfumeIdsQuery(daysBefore), cancellationToken))
			.WithTags("PerfumeWorns")
			.WithName("GetPerfumesBefore")
			.RequireAuthorization(Policies.READ);
	}
}
public class GetWornPerfumeIdsHandler(PerfumeTrackerContext context)
	: IQueryHandler<GetWornPerfumeIdsQuery, List<Guid>> {
	public async Task<List<Guid>> Handle(GetWornPerfumeIdsQuery request, CancellationToken cancellationToken) {
		return await context
			.PerfumeEvents
			.Where(x => x.Type == PerfumeEvent.PerfumeEventType.Worn && x.EventDate >= DateTimeOffset.UtcNow.AddDays(-request.DaysFilter))
			.Select(x => x.PerfumeId)
			.Distinct()
			.ToListAsync();
	}
}
