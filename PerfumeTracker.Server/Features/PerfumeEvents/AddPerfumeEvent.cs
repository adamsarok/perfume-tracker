using PerfumeTracker.Server.Features.UserProfiles;
using static PerfumeTracker.Server.Repo.PerfumeEventsRepo;

namespace PerfumeTracker.Server.Features.PerfumeEvents;
public record AddPerfumeEventCommand(PerfumeEventUploadDto Dto) : ICommand<PerfumeWornDownloadDto>;
public record PerfumeEventAddedNotification(PerfumeWornDownloadDto Dto) : INotification;
public record PerfumeRandomAcceptedNotification(PerfumeWornDownloadDto Dto) : INotification;
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
		var evt = request.Dto.Adapt<PerfumeWorn>();
		var settings = await getUserProfile.HandleAsync();
		context.PerfumeEvents.Add(evt);
		var perfume = await context.Perfumes.FindAsync(evt.PerfumeId);
		if (perfume == null) throw new NotFoundException("Perfume", evt.PerfumeId);
		if (evt.AmountMl == 0 && evt.Type == PerfumeWorn.PerfumeEventType.Worn) evt.AmountMl = -settings.SprayAmountForBottleSize(perfume.Ml);
		var result = evt.Adapt<PerfumeWornDownloadDto>();
		if (evt.Type == PerfumeWorn.PerfumeEventType.Worn) {
			context.OutboxMessages.Add(OutboxMessage.From(new PerfumeEventAddedNotification(result)));
			if (request.Dto.IsRandomPerfume) context.OutboxMessages.Add(OutboxMessage.From(new PerfumeRandomAcceptedNotification(result)));
		}
		await context.SaveChangesAsync();
		return result;
	}
}