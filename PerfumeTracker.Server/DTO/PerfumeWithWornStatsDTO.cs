namespace PerfumeTracker.Server.Dto {
	public record PerfumeWithWornStatsDto(PerfumeDto Perfume,
		DateTime? LastWorn,
		decimal BurnRatePerYearMl,
		decimal YearsLeft,
		string LastComment);
}
