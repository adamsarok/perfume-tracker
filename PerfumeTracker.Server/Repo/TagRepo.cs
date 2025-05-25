namespace PerfumeTracker.Server.Repo;
public class TagRepo(PerfumeTrackerContext context, IMediator mediator) {
	public record class TagAddedEvent(Tag tag) : INotification;
	public async Task<List<TagStatDto>> GetTagStats() {
		return await context
			.Tags
			.Select(x => new TagStatDto(
				x.Id,
				x.TagName,
				x.Color,
				x.PerfumeTags.Sum(pt => pt.Perfume.Ml),
				x.PerfumeTags.Sum(pt => pt.Perfume.PerfumeEvents.Count)
			 )).ToListAsync();
	}
	public async Task<List<TagDto>> GetTags() {
		return await context
			.Tags
			.ProjectToType<TagDto>()
			.ToListAsync();
	}

	public async Task<TagDto> AddTag(TagDto dto) {
		var tag = dto.Adapt<Tag>();
		if (tag == null) throw new MappingException();
		context.Tags.Add(tag);
		await context.SaveChangesAsync();
		await mediator.Publish(new TagAddedEvent(tag));
		return tag.Adapt<TagDto>();
	}
	public async Task DeleteTag(Guid id) {
		var tag = await context.Tags.FindAsync(id);
		if (tag == null) throw new NotFoundException();
		tag.IsDeleted = true;
		await context.SaveChangesAsync();
	}
	public async Task<TagDto> UpdateTag(Guid id, TagDto dto) {
		var tag = dto.Adapt<Tag>();
		if (tag == null) throw new MappingException();
		if (id != tag.Id) throw new NotFoundException();
		var find = await context
			.Tags
			.FindAsync(id);
		if (find == null) throw new NotFoundException();
		context.Entry(find).CurrentValues.SetValues(tag);
		await context.SaveChangesAsync();
		return find.Adapt<TagDto>();
	}
}
