namespace PerfumeTracker.Server.Dto {
	public record TagStatDto(Guid Id, string TagName, string Color, decimal Ml, int WornTimes);
}
