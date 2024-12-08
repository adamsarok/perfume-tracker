using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeTrackerAPI.DTO;
using PerfumeTrackerAPI.Models;
using static PerfumeTrackerAPI.Repo.ResultType;

namespace PerfumeTrackerAPI.Repo {
    public class PerfumeRepo {
        private readonly PerfumetrackerContext _context;

        public PerfumeRepo(PerfumetrackerContext context) {
            _context = context;
        }
        public async Task<List<PerfumeWithWornStatsDTO>> GetPerfumesWithWorn(string fulltext = null) {
#warning TODO split query https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries
            var raw = await _context
                .Perfumes
                .Where(p => string.IsNullOrWhiteSpace(fulltext) 
                    || p.FullText.Matches(EF.Functions.ToTsQuery($"{fulltext}:*"))
                    || p.PerfumeTags.Any(pt => EF.Functions.ILike(pt.Tag.TagName, fulltext))
                    )
                .Include(t => t.PerfumeTags)
                .ThenInclude(pt => pt.Tag)
                .Include(w => w.PerfumeWorns)
                .ToListAsync();
            var result = new List<PerfumeWithWornStatsDTO>();
            foreach (var r in raw) {
                var dto = GetPerfumeWornDTO(r);
                if (dto != null) result.Add(dto);
            }
            return result;
        }
        public async Task<PerfumeWithWornStatsDTO?> GetPerfumeWithWorn(int id) {
#warning TODO split query https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries
            var raw = await _context
                .Perfumes
                .Where(p => p.Id == id)
                .Include(t => t.PerfumeTags)
                .ThenInclude(pt => pt.Tag)
                .Include(w => w.PerfumeWorns)
                .FirstOrDefaultAsync();
            return GetPerfumeWornDTO(raw);
        }
        public async Task<PerfumeStatDTO> GetPerfumeStats() {
            var raw = await _context
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
        private PerfumeWithWornStatsDTO? GetPerfumeWornDTO(Perfume? r) {
            if (r == null) return null;
            var dto = new PerfumeWithWornStatsDTO {
                Perfume = r.Adapt<PerfumeDTO>(),
                LastWorn = r.PerfumeWorns.Any() ? r.PerfumeWorns.Max(x => x.WornOn) : null,
                Tags = new List<TagDTO>(),
                WornTimes = r.PerfumeWorns.Any() ? r.PerfumeWorns.Count() : 0
            };
            foreach (var tag in r.PerfumeTags) {
                dto.Tags.Add(new TagDTO() {
                    Color = tag.Tag.Color,
                    Id = tag.Tag.Id,
                    TagName = tag.Tag.TagName
                });
            }
            return dto;
        }
        public record PerfumeResult(ResultTypes ResultType, PerfumeDTO? Perfume = null, string ErrorMsg = null);
        public async Task<PerfumeResult> AddPerfume(PerfumeDTO dto) {
            try {
                var perfume = dto.Adapt<Perfume>();
                if (perfume == null) return new PerfumeResult(ResultTypes.BadRequest);
                perfume.Created_At = DateTime.UtcNow;
                _context.Perfumes.Add(perfume);
                await _context.SaveChangesAsync();
                foreach (var tag in dto.Tags) {
                    _context.PerfumeTags.Add(new PerfumeTag() {
                        Created_At = DateTime.UtcNow,
                        PerfumeId = perfume.Id,
                        TagId = tag.Id,
                    });
                }
                await _context.SaveChangesAsync();
                return new PerfumeResult(ResultTypes.Ok, perfume.Adapt<PerfumeDTO>());
            } catch (Exception ex) {
                return new PerfumeResult(ResultTypes.BadRequest, null, ex.Message);
            }
        }
        public async Task<PerfumeResult> DeletePerfume(int id) {
            var perfume = await _context.Perfumes.FindAsync(id);
            if (perfume == null) return new PerfumeResult(ResultTypes.NotFound);
            _context.Perfumes.Remove(perfume);
            await _context.SaveChangesAsync();
            return new PerfumeResult(ResultTypes.Ok);
        }
        public async Task<PerfumeResult> UpdatePerfume(int id, PerfumeDTO dto) {
            var perfume = dto.Adapt<Perfume>();
            if (perfume == null || id != perfume.Id) {
                return new PerfumeResult(ResultTypes.BadRequest);
            }

            var find = await _context
                .Perfumes
                .Include(x => x.PerfumeTags)
                .ThenInclude(x => x.Tag)
                .FirstOrDefaultAsync(x => x.Id == perfume.Id);
            if (find == null) return new PerfumeResult(ResultTypes.NotFound);

            _context.Entry(find).CurrentValues.SetValues(perfume);
            find.Updated_At = DateTime.UtcNow;

            await UpdateTags(dto, find);

            return new PerfumeResult(ResultTypes.Ok, find.Adapt<PerfumeDTO>());
        }

        private async Task UpdateTags(PerfumeDTO dto, Perfume? find) {
            var tagsInDB = find.PerfumeTags
                .Select(x => x.Tag)
                .Select(x => x.TagName)
                .ToList();
            foreach (var remove in find.PerfumeTags.Where(x => !dto.Tags.Select(x => x.TagName).Contains(x.Tag.TagName))) {
                _context.PerfumeTags.Remove(remove);
            }
            foreach (var add in dto.Tags.Where(x => !tagsInDB.Contains(x.TagName))) {
                _context.PerfumeTags.Add(new PerfumeTag() {
                    PerfumeId = find.Id,
                    TagId = add.Id //TODO: this is not good, tag ID is coming back from client side
                });
            }
            await _context.SaveChangesAsync();
        }
    }
}
