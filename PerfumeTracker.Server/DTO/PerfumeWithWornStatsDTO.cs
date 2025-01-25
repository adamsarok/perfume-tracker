using PerfumeTracker.Server.Models;

namespace PerfumeTracker.Server.DTO {
	public record PerfumeWithWornStatsDTO(PerfumeDTO Perfume, int WornTimes, DateTime? LastWorn, List<TagDTO> Tags);
}
