namespace PerfumeTracker.Server.Features.Perfumes;
public record GetPerfumeEventQuery(Guid Id) : IQuery<PerfumeEvent>;
public class GetPerfumeEventEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/perfume-events/{id}", async (Guid id, ISender sender) => {
			return await sender.Send(new GetPerfumeQuery(id));
		})
			.WithTags("PerfumeEvents")
			.WithName("GetPerfumeEvent")
			.RequireAuthorization(Policies.READ);
	}
}
public class GetPerfumeEventHandler(PerfumeTrackerContext context, ISender sender)
		: IQueryHandler<GetPerfumeEventQuery, PerfumeEvent> {
	public async Task<PerfumeEvent> Handle(GetPerfumeEventQuery request, CancellationToken cancellationToken) {
		return await context.PerfumeEvents.FindAsync(request.Id);
	}
}