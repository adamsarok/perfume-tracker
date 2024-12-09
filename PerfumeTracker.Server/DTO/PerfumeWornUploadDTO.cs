using System;

namespace PerfumeTracker.Server.DTO;

public record PerfumeWornUploadDTO(int perfumeId, DateTime wornOn);