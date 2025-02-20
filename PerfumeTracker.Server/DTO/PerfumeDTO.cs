namespace PerfumeTrackerAPI.Dto {
	public record PerfumeDto(int Id, string House, string PerfumeName, double Rating, string Notes, int Ml, string ImageObjectKey, bool Autumn, bool Spring, bool Summer, bool Winter, List<TagDto> Tags);
}
