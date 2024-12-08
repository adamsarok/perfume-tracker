using System;
using PerfumeTrackerAPI.DTO;

namespace PerfumeTracker.Server.DTO;

public record PerfumeWornDTO(
    int id, 
    DateTime wornOn, 
    PerfumeDTO perfume, 
    List<TagDTO> tags
);
