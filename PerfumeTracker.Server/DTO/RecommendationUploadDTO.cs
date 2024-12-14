using System;

namespace PerfumeTracker.Server.DTO;

public class RecommendationUploadDTO
{
    public string Query { get; set; } = null!;

    public string Recommendations { get; set; } = null!;
}
