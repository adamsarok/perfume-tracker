namespace PerfumeTracker.Server.Features.PerfumeEvents;

public record PerfumeEventDownloadDto(
    Guid Id,
    DateTime EventDate,
    Guid PerfumeId,
    string PerfumeImageObjectKey,
    string PerfumeImageUrl,
    string PerfumeHouse,
    string PerfumeName,
    List<TagDto> PerfumeTags,
    int SequenceNumber,
	bool IsDeleted
); 