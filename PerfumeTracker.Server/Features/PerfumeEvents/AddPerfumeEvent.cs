using PerfumeTracker.Server.Features.Outbox;
using static PerfumeTracker.Server.Models.PerfumeEvent;

namespace PerfumeTracker.Server.Features.PerfumeEvents;
public record PerfumeEventUploadDto(Guid PerfumeId, DateTime WornOn, PerfumeEventType Type, decimal? Amount, bool IsRandomPerfume);
public record AddPerfumeEventCommand(PerfumeEventUploadDto Dto) : ICommand<PerfumeEventDownloadDto>;
public record PerfumeEventAddedNotification(Guid PerfumeEventId, Guid PerfumeId, Guid UserId) : IUserNotification;
public record PerfumeRandomAcceptedNotification(Guid PerfumeId, Guid UserId) : IUserNotification;
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
public class AddPerfumeEventHandler(PerfumeTrackerContext context, ISideEffectQueue queue) : ICommandHandler<AddPerfumeEventCommand, PerfumeEventDownloadDto> {
	public async Task<PerfumeEventDownloadDto> Handle(AddPerfumeEventCommand request, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		var evt = request.Dto.Adapt<PerfumeEvent>();
		var settings = await context.UserProfiles.FirstAsync(cancellationToken);
		context.PerfumeEvents.Add(evt);
		var perfume = await context.Perfumes.FindAsync(evt.PerfumeId);
		if (perfume == null) throw new NotFoundException("Perfume", evt.PerfumeId);
		if (evt.AmountMl == 0 && evt.Type == PerfumeEvent.PerfumeEventType.Worn) evt.AmountMl = -settings.SprayAmountForBottleSize(perfume.Ml);
		var result = evt.Adapt<PerfumeEventDownloadDto>();
		List<OutboxMessage> messages = new List<OutboxMessage>();
		if (evt.Type == PerfumeEvent.PerfumeEventType.Worn) {
			messages.Add(OutboxMessage.From(new PerfumeEventAddedNotification(result.Id, result.PerfumeId, userId)));
			if (request.Dto.IsRandomPerfume) messages.Add(OutboxMessage.From(new PerfumeRandomAcceptedNotification(result.PerfumeId, userId)));
		}
		await context.OutboxMessages.AddRangeAsync(messages);
		await context.SaveChangesAsync();
		foreach (var message in messages) {
			queue.Enqueue(message);
		}
		return result;
	}
}