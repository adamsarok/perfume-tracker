using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeTrackerAPI.DTO;
using PerfumeTrackerAPI.Models;

namespace PerfumeTrackerAPI.Repo {
    public class PerfumeRepo {
        private readonly PerfumetrackerContext _context;

        public PerfumeRepo(PerfumetrackerContext context) {
            _context = context;
        }
        public async Task<List<PerfumeWornDTO>> GetPerfumesWithWorn(string fulltext) {
#warning TODO split query https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries
            var raw = await _context
                .Perfumes
                .Where(p => string.IsNullOrWhiteSpace(fulltext) 
                    || p.FullText.Matches(EF.Functions.ToTsQuery($"{fulltext}:*"))
                    || p.PerfumeTags.Any(pt => EF.Functions.ILike(pt.Tag.Tag1, fulltext))
                    )
                .Include(t => t.PerfumeTags)
                .ThenInclude(pt => pt.Tag)
                .Include(w => w.PerfumeWorns)
                .ToListAsync();
            var result = new List<PerfumeWornDTO>();
            foreach (var r in raw) {
                var dto = new PerfumeWornDTO {
                    Perfume = r.Adapt<PerfumeDTO>(),
                    LastWorn = r.PerfumeWorns.Any() ? r.PerfumeWorns.Max(x => x.WornOn) : null,
                    Tags = new List<TagDTO>(),
                    WornTimes = r.PerfumeWorns.Any() ? r.PerfumeWorns.Count() : 0
                };
                foreach (var tag in r.PerfumeTags) {
                    dto.Tags.Add(new TagDTO() {
                        Color = tag.Tag.Color,
                        Id = tag.Tag.Id,
                        Tag = tag.Tag.Tag1
                    });
                }
                result.Add(dto);
            }
            return result;
        }
    }
}
