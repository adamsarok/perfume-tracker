namespace PerfumeTracker.Server.DTO {
	public record PerfumeDTO(int Id, string House, string PerfumeName, double Rating, string Notes, int Ml, string ImageObjectKey, bool Autumn, bool Spring, bool Summer, bool Winter, List<TagDTO> Tags);
}
