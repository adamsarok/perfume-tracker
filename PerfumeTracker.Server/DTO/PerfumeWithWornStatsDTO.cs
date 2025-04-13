namespace PerfumeTracker.Server.Dto {
	public record PerfumeWithWornStatsDto(PerfumeDto Perfume, int WornTimes, DateTime? LastWorn, decimal burnRatePerYearMl, decimal yearsLeft);
}
