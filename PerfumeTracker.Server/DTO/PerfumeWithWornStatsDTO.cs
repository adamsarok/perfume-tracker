namespace PerfumeTracker.Server.Dto {
	public record PerfumeWithWornStatsDto(PerfumeDto Perfume,
		decimal BurnRatePerYearMl,
		decimal YearsLeft,
		string LastComment);
}
