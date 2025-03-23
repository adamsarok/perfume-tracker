namespace PerfumeTracker.Server.Dto {
	public record PerfumeDto(int Id, string House, string PerfumeName, double Rating, string Notes, decimal Ml, decimal MlLeft, string ImageObjectKey, bool Autumn, bool Spring, bool Summer, bool Winter, List<TagDto> Tags);
}
