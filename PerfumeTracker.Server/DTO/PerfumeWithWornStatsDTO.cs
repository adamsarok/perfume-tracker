using PerfumeTrackerAPI.Models;

namespace PerfumeTrackerAPI.DTO {
	public record PerfumeWithWornStatsDTO(PerfumeDTO Perfume, int WornTimes, DateTime? LastWorn);
}
