namespace PerfumeTracker.Server.Dto {
	public record TagDto(string TagName, string Color, Guid Id);
	public record TagAddDto(string TagName, string Color);
}
