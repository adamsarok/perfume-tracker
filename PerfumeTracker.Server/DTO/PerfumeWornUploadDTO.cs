using static PerfumeTracker.Server.Models.PerfumeWorn;

namespace PerfumeTracker.Server.Dto;
public record PerfumeEventUploadDto(int PerfumeId, DateTime WornOn, PerfumeEventType Type, decimal? Amount, bool IsRandomPerfume);