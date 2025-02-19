using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeTracker.Server.DTO;
using PerfumeTrackerAPI.DTO;
using PerfumeTrackerAPI.Models;
using static PerfumeTrackerAPI.Repo.ResultType;

namespace PerfumeTrackerAPI.Repo {
	public class PerfumeRepo(PerfumetrackerContext context) {
		public async Task<List<PerfumeWithWornStatsDTO>> GetPerfumesWithWorn(string fulltext = null) {
			var raw = await context
				.Perfumes
				.Where(p => string.IsNullOrWhiteSpace(fulltext)
					|| p.FullText.Matches(EF.Functions.ToTsQuery($"{fulltext}:*"))
					|| p.PerfumeTags.Any(pt => EF.Functions.ILike(pt.Tag.TagName, fulltext))
					)
				.Select(p => new PerfumeWithWornStatsDTO(
					 new PerfumeDTO(
						p.Id,
						p.House,
						p.PerfumeName,
						p.Rating,
						p.Notes,
						p.Ml,
						p.ImageObjectKey,
						p.Autumn,
						p.Spring,
						p.Summer,
						p.Winter,
						p.PerfumeTags.Select(tag => new TagDTO(tag.Tag.TagName, tag.Tag.Color, tag.Tag.Id)).ToList()
					  ),
					  p.PerfumeWorns.Any() ? p.PerfumeWorns.Count : 0,
					  p.PerfumeWorns.Any() ? p.PerfumeWorns.Max(x => x.Created_At) : null
				))
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();
			return raw;
		}
		public async Task<PerfumeWithWornStatsDTO?> GetPerfume(int id) {
			return await context
				.Perfumes
				.Where(p => p.Id == id)
				.Select(p => new PerfumeWithWornStatsDTO(
					  new PerfumeDTO(
						p.Id,
						p.House,
						p.PerfumeName,
						p.Rating,
						p.Notes,
						p.Ml,
						p.ImageObjectKey,
						p.Autumn,
						p.Spring,
						p.Summer,
						p.Winter,
						p.PerfumeTags.Select(tag => new TagDTO(tag.Tag.TagName, tag.Tag.Color, tag.Tag.Id)).ToList()
					  ),
					  p.PerfumeWorns.Any() ? p.PerfumeWorns.Count : 0,
					  p.PerfumeWorns.Any() ? p.PerfumeWorns.Max(x => x.Created_At) : null
				))
				.AsSplitQuery()
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}
		public async Task<PerfumeStatDTO> GetPerfumeStats() {
			var raw = await context
				.Perfumes
				.GroupBy(g => 1)
				.Select(x => new PerfumeStatDTO(
					x.Sum(s => s.Ml),
					x.Sum(s => s.PerfumeWorns.Count),
					x.Count()))
				.ToListAsync();
			if (raw != null && raw.Count > 0) return raw[0];
			return new PerfumeStatDTO(0, 0, 0);
		}
		public record PerfumeResult(ResultTypes ResultType, PerfumeDTO? Perfume = null, string ErrorMsg = null);
		public async Task<PerfumeResult> AddPerfume(PerfumeDTO dto) {
			try {
				var perfume = dto.Adapt<Perfume>();
				if (perfume == null) return new PerfumeResult(ResultTypes.BadRequest);
				perfume.Created_At = DateTime.UtcNow;
				context.Perfumes.Add(perfume);
				await context.SaveChangesAsync();
				foreach (var tag in dto.Tags) {
					context.PerfumeTags.Add(new PerfumeTag() {
						Created_At = DateTime.UtcNow,
						PerfumeId = perfume.Id,
						TagId = tag.Id,
					});
				}
				await context.SaveChangesAsync();
				return new PerfumeResult(ResultTypes.Ok, perfume.Adapt<PerfumeDTO>());
			}
			catch (Exception ex) {
				return new PerfumeResult(ResultTypes.BadRequest, null, ex.Message);
			}
		}
		public async Task<PerfumeResult> DeletePerfume(int id) {
			var perfume = await context.Perfumes.FindAsync(id);
			if (perfume == null) return new PerfumeResult(ResultTypes.NotFound);
			context.Perfumes.Remove(perfume);
			await context.SaveChangesAsync();
			return new PerfumeResult(ResultTypes.Ok);
		}
		public async Task<PerfumeResult> UpdatePerfume(int id, PerfumeDTO dto) {
			var perfume = dto.Adapt<Perfume>();
			if (perfume == null || id != perfume.Id) {
				return new PerfumeResult(ResultTypes.BadRequest);
			}

			var find = await context
				.Perfumes
				.Include(x => x.PerfumeTags)
				.ThenInclude(x => x.Tag)
				.FirstOrDefaultAsync(x => x.Id == perfume.Id);
			if (find == null) return new PerfumeResult(ResultTypes.NotFound);

			context.Entry(find).CurrentValues.SetValues(perfume);
			find.Updated_At = DateTime.UtcNow;

			await UpdateTags(dto, find);

			return new PerfumeResult(ResultTypes.Ok, find.Adapt<PerfumeDTO>());
		}

		public async Task<PerfumeResult> UpdatePerfumeImageGuid(ImageGuidDTO dto) {
			var find = await context.Perfumes.FindAsync(dto.ParentObjectId);
			if (find == null) return new PerfumeResult(ResultTypes.NotFound);
			find.ImageObjectKey = dto.ImageObjectKey;
			find.Updated_At = DateTime.UtcNow;
			await context.SaveChangesAsync();
			return new PerfumeResult(ResultTypes.Ok, find.Adapt<PerfumeDTO>());
		}

		private async Task UpdateTags(PerfumeDTO dto, Perfume? find) {
			var tagsInDB = find.PerfumeTags
				.Select(x => x.Tag)
				.Select(x => x.TagName)
				.ToList();
			foreach (var remove in find.PerfumeTags.Where(x => !dto.Tags.Select(x => x.TagName).Contains(x.Tag.TagName))) {
				context.PerfumeTags.Remove(remove);
			}
			foreach (var add in dto.Tags.Where(x => !tagsInDB.Contains(x.TagName))) {
				context.PerfumeTags.Add(new PerfumeTag() {
					PerfumeId = find.Id,
					TagId = add.Id //TODO: this is not good, tag ID is coming back from client side
				});
			}
			await context.SaveChangesAsync();
		}
	}
}
