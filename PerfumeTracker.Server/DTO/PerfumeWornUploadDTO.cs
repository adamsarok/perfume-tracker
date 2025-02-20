using System;

namespace PerfumeTracker.Server.Dto;

public record PerfumeWornUploadDto(int PerfumeId, DateTime WornOn);