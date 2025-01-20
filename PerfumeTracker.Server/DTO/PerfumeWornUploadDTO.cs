using System;

namespace PerfumeTracker.Server.DTO;

public record PerfumeWornUploadDTO(int PerfumeId, DateTime WornOn);