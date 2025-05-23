﻿
namespace PerfumeTracker.Server.Features.Perfumes;

public record UpdatePerfumeCommand(int Id, PerfumeDto Dto) : ICommand<PerfumeDto>;
public class UpdatePerfumeEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPut("/api/perfumes/{id}", async (int id, PerfumeDto dto, ISender sender) =>
			await sender.Send(new UpdatePerfumeCommand(id, dto)))
			.WithTags("Perfumes")
			.WithName("UpdatePerfume");

	}
}
public record class PerfumeUpdatedNotification() : INotification;
public record class PerfumeTagsAddedNotification() : INotification;
public class UpdatePerfumeHandler(PerfumeTrackerContext context) : ICommandHandler<UpdatePerfumeCommand, PerfumeDto> {
	public async Task<PerfumeDto> Handle(UpdatePerfumeCommand request, CancellationToken cancellationToken) {
		var perfume = request.Dto.Adapt<Perfume>();
		if (perfume == null || request.Id != perfume.Id) {
			throw new NotFoundException();
		}
		var find = await context
			.Perfumes
			.Include(x => x.PerfumeTags)
			.ThenInclude(x => x.Tag)
			.FirstOrDefaultAsync(x => x.Id == perfume.Id);
		if (find == null) throw new NotFoundException();
		context.Entry(find).CurrentValues.SetValues(perfume);
		var mlLeftInDb = Math.Max(0, context.PerfumeEvents.Where(x => x.PerfumeId == perfume.Id).Sum(s => s.AmountMl));
		if (request.Dto.MlLeft != mlLeftInDb) {
			context.PerfumeEvents.Add(new PerfumeWorn() {
				AmountMl = request.Dto.MlLeft - mlLeftInDb,
				CreatedAt = DateTime.UtcNow,
				EventDate = DateTime.UtcNow,
				PerfumeId = perfume.Id,
				Type = PerfumeWorn.PerfumeEventType.Adjusted,
				UpdatedAt = DateTime.UtcNow
			});
		}
		await UpdateTags(request.Dto, find);
		context.OutboxMessages.Add(OutboxMessage.From(new PerfumeUpdatedNotification()));
		await context.SaveChangesAsync();
		return find.Adapt<PerfumeDto>();
	}
	private async Task UpdateTags(PerfumeDto Dto, Perfume? find) {
		bool tagsAdded = false;
		if (find == null) return;
		var tagsInDB = find.PerfumeTags
			.Select(x => x.Tag)
			.Select(x => x.TagName)
			.ToList();
		foreach (var remove in find.PerfumeTags.Where(x => !Dto.Tags.Select(x => x.TagName).Contains(x.Tag.TagName))) {
			context.PerfumeTags.Remove(remove);
		}
		if (Dto.Tags != null && Dto.Tags.Any()) {
			foreach (var add in Dto.Tags.Where(x => !tagsInDB.Contains(x.TagName))) {
				context.PerfumeTags.Add(new PerfumeTag() {
					PerfumeId = find.Id,
					TagId = add.Id //TODO: this is not good, tag ID is coming back from client side
				});
				tagsAdded = true;
			}
		}
		if (tagsAdded) context.OutboxMessages.Add(OutboxMessage.From(new PerfumeTagsAddedNotification()));
	}
}