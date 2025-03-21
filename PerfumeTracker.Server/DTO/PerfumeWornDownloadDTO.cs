namespace PerfumeTracker.Server.Dto;
public record PerfumeWornDownloadDto(
	int Id,
	DateTime WornOn,
	PerfumeDto Perfume,
	List<TagDto> Tags
);
