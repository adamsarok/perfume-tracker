namespace PerfumeTracker.Server.DTO;

public record GlobalPerfumeDto(
	Guid Id,
	string House,
	string PerfumeName,
	string Family,
	List<TagDto> Tags
);