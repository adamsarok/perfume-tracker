namespace PerfumeTracker.Server.Dto {
	public record PerfumeWithWornStatsDto(PerfumeDto Perfume,
		int WornTimes,
		DateTime? LastWorn,
		decimal BurnRatePerYearMl,
		decimal YearsLeft,
		decimal AverageRating,
		string LastComment);
}
