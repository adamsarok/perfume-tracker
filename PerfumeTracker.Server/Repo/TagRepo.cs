using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeTrackerAPI.DTO;
using PerfumeTrackerAPI.Models;

namespace PerfumeTrackerAPI.Repo {
    public class TagRepo {
        private readonly PerfumetrackerContext _context;
        public TagRepo(PerfumetrackerContext context) {
            _context = context;
        }
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
    }
}
