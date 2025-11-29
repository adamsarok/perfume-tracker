using Microsoft.Build.Tasks;
using PerfumeTracker.Server.Models;
using PerfumeTracker.Server.Services.Auth;
using PerfumeTracker.Server.Services.Outbox;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;
using static PerfumeTracker.Server.Models.PerfumeEvent;

namespace PerfumeTracker.Server.Features.PerfumeEvents;
public record PerfumeEventUploadDto(Guid PerfumeId, DateTime? WornOn, PerfumeEventType Type, decimal? Amount, Guid? RandomsId);
public record AddPerfumeEventCommand(PerfumeEventUploadDto Dto) : ICommand<PerfumeEventDownloadDto>;
public record PerfumeEventAddedNotification(Guid PerfumeEventId, Guid PerfumeId, Guid UserId, PerfumeEvent.PerfumeEventType Type) : IUserNotification;
public record PerfumeRandomAcceptedNotification(Guid PerfumeId, Guid UserId) : IUserNotification;
public class AddPerfumeEventCommandValidator : AbstractValidator<AddPerfumeEventCommand> {
	public AddPerfumeEventCommandValidator() {
		RuleFor(x => x.Dto.PerfumeId).NotEmpty();
		RuleFor(x => x.Dto.Type).IsInEnum();
	}
}
public class AddPerfumeEventEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/perfume-events", async (PerfumeEventUploadDto dto, ISender sender, CancellationToken cancellationToken) => {
			var result = await sender.Send(new AddPerfumeEventCommand(dto), cancellationToken);
			return Results.CreatedAtRoute("GetPerfumeEvent", new { id = result.Id }, result);
		}).WithTags("PerfumeWorns")
			.WithName("PostPerfumeWorn")
			.RequireAuthorization(Policies.WRITE);
	}
}
public class AddPerfumeEventHandler(PerfumeTrackerContext context, ISideEffectQueue queue) : ICommandHandler<AddPerfumeEventCommand, PerfumeEventDownloadDto> {
	public async Task<PerfumeEventDownloadDto> Handle(AddPerfumeEventCommand request, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		var evt = new PerfumeEvent() {
			AmountMl = request.Dto.Amount ?? 0,
			PerfumeId = request.Dto.PerfumeId,
			EventDate = request.Dto.WornOn ?? DateTime.UtcNow,
			Type = request.Dto.Type
		};
		context.PerfumeEvents.Add(evt);
		var perfume = await context.Perfumes.FindAsync([evt.PerfumeId], cancellationToken) ?? throw new NotFoundException("Perfumes", evt.PerfumeId);
		var result = evt.Adapt<PerfumeEventDownloadDto>();
		List<OutboxMessage> messages = [];
		messages.Add(OutboxMessage.From(new PerfumeEventAddedNotification(result.Id, result.PerfumeId, userId, evt.Type)));
		switch (evt.Type) {
			case PerfumeEvent.PerfumeEventType.Worn:
				await HandleWorn(messages, context, evt, perfume!, request, userId, cancellationToken);
				break;
			default:
				break;
		}
		await context.OutboxMessages.AddRangeAsync(messages, cancellationToken);
		await context.SaveChangesAsync(cancellationToken);
		foreach (var message in messages) {
			queue.Enqueue(message);
		}
		return result;
	}

	async Task HandleWorn(List<OutboxMessage> messages, PerfumeTrackerContext context, PerfumeEvent evt, Perfume perfume,
			AddPerfumeEventCommand request, Guid userId, CancellationToken cancellationToken) {
		if (evt.AmountMl == 0) {
			var settings = await context.UserProfiles.FirstAsync(cancellationToken);
			evt.AmountMl = -settings.SprayAmountForBottleSize(perfume.Ml);
		}
		if (request.Dto.RandomsId == null) return;
		messages.Add(OutboxMessage.From(new PerfumeRandomAcceptedNotification(perfume.Id, userId)));
		var randoms = await context.PerfumeRandoms.FindAsync(request.Dto.RandomsId);
		if (randoms != null) randoms.IsAccepted = true;
	}
}