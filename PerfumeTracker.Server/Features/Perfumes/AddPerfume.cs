namespace PerfumeTracker.Server.Features.Perfumes;
public record AddPerfumeCommand(PerfumeDto Dto) : ICommand<PerfumeDto>;
public class AddPerfumeEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/perfumes", async (PerfumeDto dto, ISender sender) => {
			var result = await sender.Send(new AddPerfumeCommand(dto));
			return Results.CreatedAtRoute("GetPerfume", new { id = result.Id }, result);
		}).WithTags("Perfumes")
			.WithName("PostPerfume");
	}
}
public record class PerfumeAddedNotification(Guid PerfumeId) : INotification;
public class AddPerfumeHandler(PerfumeTrackerContext context) : ICommandHandler<AddPerfumeCommand, PerfumeDto> {
	public async Task<PerfumeDto> Handle(AddPerfumeCommand request, CancellationToken cancellationToken) {
		using var transaction = await context.Database.BeginTransactionAsync(cancellationToken); //TODO change to GUID, remove double savechanges
		var perfume = request.Dto.Adapt<Perfume>();
		if (perfume == null) throw new InvalidOperationException("Perfume mapping failed");
		context.Perfumes.Add(perfume);
		await context.SaveChangesAsync();
		foreach (var tag in request.Dto.Tags) {
			context.PerfumeTags.Add(new PerfumeTag() {
				PerfumeId = perfume.Id,
				TagId = tag.Id,
			});
		}
		if (request.Dto.MlLeft > 0) {
			context.PerfumeEvents.Add(new PerfumeEvent() {
				AmountMl = request.Dto.MlLeft,
				CreatedAt = DateTime.UtcNow,
				EventDate = DateTime.UtcNow,
				Perfume = perfume,
				Type = PerfumeEvent.PerfumeEventType.Added,
				UpdatedAt = DateTime.UtcNow
			});
		}
		context.OutboxMessages.Add(OutboxMessage.From(new PerfumeAddedNotification(perfume.Id)));
		await context.SaveChangesAsync();
		await transaction.CommitAsync(cancellationToken);
		return perfume.Adapt<PerfumeDto>();
	}
}