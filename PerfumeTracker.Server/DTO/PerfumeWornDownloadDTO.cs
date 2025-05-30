using PerfumeTracker.Server.Models;

namespace PerfumeTracker.Server.Dto;
public record PerfumeEventDownloadDto(
	Guid Id,
	DateTime WornOn,
	Guid PerfumeId,
	string perfumeImageObjectKey,
	string perfumeImageUrl,
	string perfumeHouse,
    string perfumeName,
	List<TagDto> PerfumeTags
);
