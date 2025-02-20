using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeTrackerAPI.Dto;
using PerfumeTrackerAPI.Models;
using static PerfumeTrackerAPI.Repo.ResultType;

namespace PerfumeTrackerAPI.Repo {
    public class TagRepo(PerfumetrackerContext context) {
        public record TagResult(ResultTypes ResultType, TagDto? Tag = null, string ErrorMsg = null);
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
            var r = await context
                .Tags
                .FindAsync(id);
            return r.Adapt<TagDto>();
        }

        public async Task<TagResult> AddTag(TagDto dto) {
            try {
                var tag = dto.Adapt<Tag>();
                if (tag == null) return new TagResult(ResultTypes.BadRequest);
                tag.Created_At = DateTime.UtcNow;
                context.Tags.Add(tag);
                await context.SaveChangesAsync();
                return new TagResult(ResultTypes.Ok, tag.Adapt<TagDto>());
            } catch (Exception ex) {
                return new TagResult(ResultTypes.BadRequest, null, ex.Message);
            }
        }
        public async Task<TagResult> DeleteTag(int id) {
            var tag = await context.Tags.FindAsync(id);
            if (tag == null) return new TagResult(ResultTypes.NotFound);
            context.Tags.Remove(tag);
            await context.SaveChangesAsync();
            return new TagResult(ResultTypes.Ok);
        }
        public async Task<TagResult> UpdateTag(int id, TagDto dto) {
            var tag = dto.Adapt<Tag>();
            if (tag == null || id != tag.Id) {
                return new TagResult(ResultTypes.BadRequest);
            }
            var find = await context
                .Tags
                .FindAsync(id);
            if (find == null) return new TagResult(ResultTypes.NotFound);

            context.Entry(find).CurrentValues.SetValues(tag);
            find.Updated_At = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return new TagResult(ResultTypes.Ok, find.Adapt<TagDto>());
        }
    }
}
