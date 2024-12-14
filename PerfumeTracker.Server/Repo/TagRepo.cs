using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeTrackerAPI.DTO;
using PerfumeTrackerAPI.Models;
using static PerfumeTrackerAPI.Repo.ResultType;

namespace PerfumeTrackerAPI.Repo {
    public class TagRepo {
        private readonly PerfumetrackerContext _context;
        public TagRepo(PerfumetrackerContext context) {
            _context = context;
        }
        public record TagResult(ResultTypes ResultType, TagDTO? Tag = null, string ErrorMsg = null);
        public async Task<List<TagStatDTO>> GetTagStats() {
            return await _context
                .Tags
                .Select(x => new TagStatDTO(
                    x.Id,
                    x.TagName,
                    x.Color,
                    x.PerfumeTags.Sum(pt => pt.Perfume.Ml),
                    x.PerfumeTags.Sum(pt => pt.Perfume.PerfumeWorns.Count)
                 )).ToListAsync();
        }
        public async Task<List<TagDTO>> GetTags() {
            return await _context
                .Tags
                .ProjectToType<TagDTO>()
                .ToListAsync();
        }

        public async Task<TagDTO> GetTag(int id) {
            var r = await _context
                .Tags
                .FindAsync(id);
            return r.Adapt<TagDTO>();
        }

        public async Task<TagResult> AddTag(TagDTO dto) {
            try {
                var tag = dto.Adapt<Tag>();
                if (tag == null) return new TagResult(ResultTypes.BadRequest);
                tag.Created_At = DateTime.UtcNow;
                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();
                return new TagResult(ResultTypes.Ok, tag.Adapt<TagDTO>());
            } catch (Exception ex) {
                return new TagResult(ResultTypes.BadRequest, null, ex.Message);
            }
        }
        public async Task<TagResult> DeleteTag(int id) {
            var tag = await _context.Tags.FindAsync(id);
            if (tag == null) return new TagResult(ResultTypes.NotFound);
            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
            return new TagResult(ResultTypes.Ok);
        }
        public async Task<TagResult> UpdateTag(int id, TagDTO dto) {
            var tag = dto.Adapt<Tag>();
            if (tag == null || id != tag.Id) {
                return new TagResult(ResultTypes.BadRequest);
            }
            var find = await _context
                .Tags
                .FindAsync(id);
            if (find == null) return new TagResult(ResultTypes.NotFound);

            _context.Entry(find).CurrentValues.SetValues(tag);
            find.Updated_At = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new TagResult(ResultTypes.Ok, find.Adapt<TagDTO>());
        }
    }
}
