namespace PerfumeTracker.Server.Features.PerfumeEvents;

public record PerfumeEventDownloadDto(
	Guid Id,
	DateTime EventDate,
	Guid PerfumeId,
	Guid? PerfumeImageObjectKey,
	string PerfumeImageUrl,
	string PerfumeHouse,
	string PerfumeName,
	List<TagDto> PerfumeTags,
	int SequenceNumber,
	bool IsDeleted
);