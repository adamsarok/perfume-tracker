using System;
using PerfumeTracker.Server.DTO;

namespace PerfumeTracker.Server.DTO;

public record PerfumeWornDownloadDTO(
	int Id,
	DateTime WornOn,
	PerfumeDTO Perfume,
	List<TagDTO> Tags
);
