namespace PerfumeTracker.Server.Features.PerfumeEvents;
public record AddPerfumeEventCommand(PerfumeEventUploadDto Dto) : ICommand<PerfumeEventDownloadDto>;
public record PerfumeEventAddedNotification(Guid PerfumeEventId, Guid PerfumeId, Guid UserId) : INotification;
public record PerfumeRandomAcceptedNotification(Guid PerfumeId, Guid UserId) : INotification;
public class AddPerfumeEventEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/perfume-events", async (PerfumeEventUploadDto dto, ISender sender) => {
			var result = await sender.Send(new AddPerfumeEventCommand(dto));
			return Results.CreatedAtRoute("GetPerfumeEvent", new { id = result.Id }, result);
		}).WithTags("PerfumeWorns")
			.WithName("PostPerfumeWorn")
			.RequireAuthorization(Policies.WRITE);
	}
}
public class AddPerfumeEventHandler(PerfumeTrackerContext context) : ICommandHandler<AddPerfumeEventCommand, PerfumeEventDownloadDto> {
	public async Task<PerfumeEventDownloadDto> Handle(AddPerfumeEventCommand request, CancellationToken cancellationToken) {
		var evt = request.Dto.Adapt<PerfumeEvent>();
		var settings = await context.UserProfiles.FirstAsync(cancellationToken);
		context.PerfumeEvents.Add(evt);
		var perfume = await context.Perfumes.FindAsync(evt.PerfumeId);
		if (perfume == null) throw new NotFoundException("Perfume", evt.PerfumeId);
		if (evt.AmountMl == 0 && evt.Type == PerfumeEvent.PerfumeEventType.Worn) evt.AmountMl = -settings.SprayAmountForBottleSize(perfume.Ml);
		var result = evt.Adapt<PerfumeEventDownloadDto>();
		var userId = context.TenantProvider?.GetCurrentUserId();
		if (userId == null) throw new BadRequestException("Tenant not set");
		if (evt.Type == PerfumeEvent.PerfumeEventType.Worn) {
			context.OutboxMessages.Add(OutboxMessage.From(new PerfumeEventAddedNotification(result.Id, result.PerfumeId, userId.Value)));
			if (request.Dto.IsRandomPerfume) context.OutboxMessages.Add(OutboxMessage.From(new PerfumeRandomAcceptedNotification(result.PerfumeId, userId.Value)));
		}
		await context.SaveChangesAsync();
		return result;
	}
}