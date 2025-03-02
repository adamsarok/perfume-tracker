using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeTracker.Server.Exceptions;
using PerfumeTrackerAPI.Dto;
using PerfumeTrackerAPI.Models;

namespace PerfumeTrackerAPI.Repo {
	public class TagRepo(PerfumetrackerContext context) {
		public async Task<List<TagStatDto>> GetTagStats() {
			return await context
				.Tags
				.Select(x => new TagStatDto(
					x.Id,
					x.TagName,
					x.Color,
					x.PerfumeTags.Sum(pt => pt.Perfume.Ml),
					x.PerfumeTags.Sum(pt => pt.Perfume.PerfumeWorns.Count)
				 )).ToListAsync();
		}
		public async Task<List<TagDto>> GetTags() {
			return await context
				.Tags
				.ProjectToType<TagDto>()
				.ToListAsync();
		}

		public async Task<TagDto> GetTag(int id) {
			var t = await context
				.Tags
				.FindAsync(id);
			if (t == null) throw new NotFoundException();
			var r = t.Adapt<TagDto>();
			if (r == null) throw new MappingException();
			return r;
		}

		public async Task<TagDto> AddTag(TagDto dto) {
			var tag = dto.Adapt<Tag>();
			if (tag == null) throw new MappingException();
			tag.Created_At = DateTime.UtcNow;
			context.Tags.Add(tag);
			await context.SaveChangesAsync();
			return tag.Adapt<TagDto>();
		}
		public async Task DeleteTag(int id) {
			var tag = await context.Tags.FindAsync(id);
			if (tag == null) throw new NotFoundException();
			context.Tags.Remove(tag);
			await context.SaveChangesAsync();
		}
		public async Task<TagDto> UpdateTag(int id, TagDto dto) {
			var tag = dto.Adapt<Tag>();
			if (tag == null) throw new MappingException();
			if (id != tag.Id) throw new NotFoundException();
			var find = await context
				.Tags
				.FindAsync(id);
			if (find == null) throw new NotFoundException();

			context.Entry(find).CurrentValues.SetValues(tag);
			find.Updated_At = DateTime.UtcNow;

			await context.SaveChangesAsync();

			return find.Adapt<TagDto>();
		}
	}
}
