using PerfumeTracker.Server.Features.PerfumeRatings;

namespace PerfumeTracker.Server.Dto;
public record PerfumeDto(Guid Id, string House, string PerfumeName, decimal Ml, decimal MlLeft, Guid? ImageObjectKey, string ImageUrl, bool Autumn, bool Spring, bool Summer, bool Winter, List<TagDto> Tags, bool IsDeleted, List<PerfumeRatingDownloadDto> Ratings);
public record PerfumeUploadDto(string House, string PerfumeName, decimal Ml, decimal MlLeft, bool Autumn, bool Spring, bool Summer, bool Winter, List<TagDto> Tags);

