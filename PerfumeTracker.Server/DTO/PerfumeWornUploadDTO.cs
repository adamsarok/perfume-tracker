using static PerfumeTracker.Server.Models.PerfumeEvent;

namespace PerfumeTracker.Server.Dto;
public record PerfumeEventUploadDto(Guid PerfumeId, DateTime WornOn, PerfumeEventType Type, decimal? Amount, bool IsRandomPerfume);