
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
public record class PerfumeUpdatedNotification(Guid PerfumeId) : INotification;
public record class PerfumeTagsAddedNotification(List<Guid> PerfumeTagIds, Guid UserId) : INotification;
public class UpdatePerfumeHandler(PerfumeTrackerContext context) : ICommandHandler<UpdatePerfumeCommand, PerfumeDto> {
	public async Task<PerfumeDto> Handle(UpdatePerfumeCommand request, CancellationToken cancellationToken) {
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
		await UpdateTags(request.Dto, find);
		context.OutboxMessages.Add(OutboxMessage.From(new PerfumeUpdatedNotification(find.Id)));
		await context.SaveChangesAsync();
		return find.Adapt<PerfumeDto>();
	}
	private async Task UpdateTags(PerfumeUploadDto Dto, Perfume? find) {
		if (find == null) return;
		var tagsInDB = find.PerfumeTags
			.Select(x => x.Tag)
			.Select(x => x.TagName)
			.ToList();
		foreach (var remove in find.PerfumeTags.Where(x => !Dto.Tags.Select(x => x.TagName).Contains(x.Tag.TagName))) {
			context.PerfumeTags.Remove(remove);
		}
		List<PerfumeTag> tagsToAdd = new List<PerfumeTag>();
		if (Dto.Tags != null && Dto.Tags.Any()) {
			foreach (var add in Dto.Tags.Where(x => !tagsInDB.Contains(x.TagName))) {
				tagsToAdd.Add(new PerfumeTag() {
					PerfumeId = find.Id,
					TagId = add.Id
				});
			}
		}
		var userId = context.TenantProvider?.GetCurrentUserId();
		if (userId == null) throw new BadRequestException("Tenant not set");
		if (tagsToAdd.Any()) {
			context.PerfumeTags.AddRange(tagsToAdd);
			context.OutboxMessages.Add(OutboxMessage.From(new PerfumeTagsAddedNotification(tagsToAdd.Select(x => x.Id).ToList(), userId.Value)));
		}
	}
}