using PerfumeTrackerAPI.Models;

namespace PerfumeTrackerAPI.Dto {
	public record PerfumeWithWornStatsDto(PerfumeDto Perfume, int WornTimes, DateTime? LastWorn);
}
