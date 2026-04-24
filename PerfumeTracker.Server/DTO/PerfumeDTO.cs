using PerfumeTracker.Server.Features.PerfumeRatings;

namespace PerfumeTracker.Server.Dto;

public record PerfumeDto(Guid Id,
	string House,
	string PerfumeName,
	string Family,
	string Parfumeur,
	decimal Ml,
	decimal MlLeft,
	Guid? ImageObjectKey,
	string ImageUrl,
	List<TagDto> Tags,
	bool IsDeleted,
	IReadOnlyList<PerfumeRatingDownloadDto> Ratings,
	int WearCount,
	decimal AverageRating,
	DateTime? LastWorn
	);
public record PerfumeUploadDto(string House, string PerfumeName, string Family, string Parfumeur, decimal Ml, decimal MlLeft, List<TagDto> Tags);

