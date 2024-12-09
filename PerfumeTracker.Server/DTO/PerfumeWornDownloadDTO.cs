using System;
using PerfumeTrackerAPI.DTO;

namespace PerfumeTracker.Server.DTO;

public record PerfumeWornDownloadDTO(
    int id, 
    DateTime wornOn, 
    PerfumeDTO perfume, 
    List<TagDTO> tags
);
