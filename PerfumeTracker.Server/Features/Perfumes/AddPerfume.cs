﻿using PerfumeTracker.Server.Features.Outbox;

namespace PerfumeTracker.Server.Features.Perfumes;
public record AddPerfumeCommand(PerfumeUploadDto Dto) : ICommand<PerfumeDto>;
public class AddPerfumeCommandValidator : AbstractValidator<AddPerfumeCommand> {
	public AddPerfumeCommandValidator() {
		RuleFor(x => x.Dto).SetValidator(new PerfumeUploadValidator());
	}
}
public class AddPerfumeEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/perfumes", async (PerfumeUploadDto dto, ISender sender) => {
			var perfume = dto.Adapt<PerfumeUploadDto>();
			var result = await sender.Send(new AddPerfumeCommand(perfume));
			return Results.CreatedAtRoute("GetPerfume", new { id = result.Id }, result);
		}).WithTags("Perfumes")
			.WithName("PostPerfume")
			.RequireAuthorization(Policies.WRITE);
	}
}
public record class PerfumeAddedNotification(Guid PerfumeId, Guid UserId) : IUserNotification;
public class AddPerfumeHandler(PerfumeTrackerContext context, ISideEffectQueue queue) : ICommandHandler<AddPerfumeCommand, PerfumeDto> {
	public async Task<PerfumeDto> Handle(AddPerfumeCommand request, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		//TODO: unique constraint on perfume name should be a soft constraint - eg if soft deleted, enable duplications 
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
		var message = OutboxMessage.From(new PerfumeAddedNotification(perfume.Id, userId));
		context.OutboxMessages.Add(message);
		await context.SaveChangesAsync();
		await transaction.CommitAsync(cancellationToken);
		queue.Enqueue(message);
		return perfume.Adapt<PerfumeDto>();
	}
}