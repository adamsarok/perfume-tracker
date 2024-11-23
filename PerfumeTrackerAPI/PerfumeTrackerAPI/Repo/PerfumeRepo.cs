using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PerfumeTrackerAPI.DTO;
using PerfumeTrackerAPI.Models;

namespace PerfumeTrackerAPI.Repo {
    public class PerfumeRepo {
        private readonly PerfumetrackerContext _context;
        private readonly IMapper _mapper;

        public PerfumeRepo(PerfumetrackerContext context, IMapper mapper) {
            _context = context;
            _mapper = mapper;
        }
        public async Task<List<PerfumeWornDTO>> GetPerfumesWithWorn(string fulltext) {
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
                    Perfume = _mapper.Map<PerfumeDTO>(r),
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
