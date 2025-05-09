using PerfumeTracker.Server.Models;

namespace PerfumeTracker.Server.Dto;
public record PerfumeWornDownloadDto(
	int Id,
	DateTime WornOn,
	int PerfumeId,
	string perfumeImageObjectKey,
	string perfumeImageUrl,
	string perfumeHouse,
    string perfumeName,
	List<TagDto> PerfumeTags
);
