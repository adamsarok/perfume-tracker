
using PerfumeTracker.Server.Features.Outbox;

namespace PerfumeTracker.Server.Features.Perfumes;
public record UpdatePerfumeCommand(Guid Id, PerfumeUploadDto Dto) : ICommand<PerfumeDto>;
public class UpdatePerfumeCommandValidator : AbstractValidator<UpdatePerfumeCommand> {
	public UpdatePerfumeCommandValidator() {
		RuleFor(x => x.Dto).SetValidator(new PerfumeUploadValidator());
	}
}
public class UpdatePerfumeEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPut("/api/perfumes/{id}", async (Guid id, PerfumeUploadDto dto, ISender sender) =>
			await sender.Send(new UpdatePerfumeCommand(id, dto)))
			.WithTags("Perfumes")
			.WithName("UpdatePerfume")
			.RequireAuthorization(Policies.WRITE);
	}
}
public record class PerfumeUpdatedNotification(Guid PerfumeId, Guid UserId) : IUserNotification;
public record class PerfumeTagsAddedNotification(List<Guid> PerfumeTagIds, Guid UserId) : IUserNotification;
public class UpdatePerfumeHandler(PerfumeTrackerContext context, ISideEffectQueue queue) : ICommandHandler<UpdatePerfumeCommand, PerfumeDto> {
	public async Task<PerfumeDto> Handle(UpdatePerfumeCommand request, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		var find = await context
			.Perfumes
			.Include(x => x.PerfumeTags)
			.ThenInclude(x => x.Tag)
			.FirstOrDefaultAsync(x => x.Id == request.Id);
		if (find == null) throw new NotFoundException();
		context.Entry(find).CurrentValues.SetValues(request.Dto);
		var sum = await context.PerfumeEvents.Where(x => x.PerfumeId == find.Id).SumAsync(s => s.AmountMl);
		var mlLeftInDb = Math.Max(0, sum);
		if (request.Dto.MlLeft != mlLeftInDb) {
			context.PerfumeEvents.Add(new PerfumeEvent() {
				AmountMl = request.Dto.MlLeft - mlLeftInDb,
				CreatedAt = DateTime.UtcNow,
				EventDate = DateTime.UtcNow,
				PerfumeId = find.Id,
				Type = PerfumeEvent.PerfumeEventType.Adjusted,
				UpdatedAt = DateTime.UtcNow
			});
		}
		List<OutboxMessage> messages = new List<OutboxMessage>();
		var tagsInDB = find.PerfumeTags
			.Select(x => x.Tag)
			.Select(x => x.TagName)
			.ToList();
		foreach (var remove in find.PerfumeTags.Where(x => !request.Dto.Tags.Select(x => x.TagName).Contains(x.Tag.TagName))) {
			context.PerfumeTags.Remove(remove);
		}
		List<PerfumeTag> tagsToAdd = new List<PerfumeTag>();
		if (request.Dto.Tags != null && request.Dto.Tags.Any()) {
			foreach (var add in request.Dto.Tags.Where(x => !tagsInDB.Contains(x.TagName))) {
				tagsToAdd.Add(new PerfumeTag() {
					PerfumeId = find.Id,
					TagId = add.Id
				});
			}
		}
		if (tagsToAdd.Any()) {
			context.PerfumeTags.AddRange(tagsToAdd);
			messages.Add(OutboxMessage.From(new PerfumeTagsAddedNotification(tagsToAdd.Select(x => x.Id).ToList(), userId)));
		}
		messages.Add(OutboxMessage.From(new PerfumeUpdatedNotification(find.Id, userId)));
		await context.OutboxMessages.AddRangeAsync(messages);
		await context.SaveChangesAsync();
		foreach (var message in messages) queue.Enqueue(message);
		return find.Adapt<PerfumeDto>();
	}
}