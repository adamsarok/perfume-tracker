namespace PerfumeTracker.Server.Features.PerfumeEvents;
public record AddPerfumeEventCommand(PerfumeEventUploadDto Dto) : ICommand<PerfumeWornDownloadDto>;
public record PerfumeEventAddedNotification(Guid PerfumeEventId, Guid PerfumeId) : INotification;
public record PerfumeRandomAcceptedNotification(Guid PerfumeId) : INotification;
public class AddPerfumeEventEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/perfume-events", async (PerfumeEventUploadDto dto, ISender sender) => {
			var result = await sender.Send(new AddPerfumeEventCommand(dto));
			return Results.Created();
		}).WithTags("PerfumeWorns")
			.WithName("PostPerfumeWorn");
	}
}
public class AddPerfumeEventHandler(PerfumeTrackerContext context, GetUserProfile getUserProfile) : ICommandHandler<AddPerfumeEventCommand, PerfumeWornDownloadDto> {
	public async Task<PerfumeWornDownloadDto> Handle(AddPerfumeEventCommand request, CancellationToken cancellationToken) {
		var evt = request.Dto.Adapt<PerfumeEvent>();
		var settings = await getUserProfile.HandleAsync();
		context.PerfumeEvents.Add(evt);
		var perfume = await context.Perfumes.FindAsync(evt.PerfumeId);
		if (perfume == null) throw new NotFoundException("Perfume", evt.PerfumeId);
		if (evt.AmountMl == 0 && evt.Type == PerfumeEvent.PerfumeEventType.Worn) evt.AmountMl = -settings.SprayAmountForBottleSize(perfume.Ml);
		var result = evt.Adapt<PerfumeWornDownloadDto>();
		if (evt.Type == PerfumeEvent.PerfumeEventType.Worn) {
			context.OutboxMessages.Add(OutboxMessage.From(new PerfumeEventAddedNotification(result.Id, result.PerfumeId)));
			if (request.Dto.IsRandomPerfume) context.OutboxMessages.Add(OutboxMessage.From(new PerfumeRandomAcceptedNotification(result.PerfumeId)));
		}
		await context.SaveChangesAsync();
		return result;
	}
}