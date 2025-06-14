namespace PerfumeTracker.Server.Dto {
	public record PerfumeDto(Guid Id, string House, string PerfumeName, double Rating, string Notes, decimal Ml, decimal MlLeft, string ImageObjectKey, string ImageUrl, bool Autumn, bool Spring, bool Summer, bool Winter, List<TagDto> Tags, bool IsDeleted);
	public record PerfumeUploadDto(string House, string PerfumeName, double Rating, string Notes, decimal Ml, decimal MlLeft, bool Autumn, bool Spring, bool Summer, bool Winter, List<TagDto> Tags);
}
