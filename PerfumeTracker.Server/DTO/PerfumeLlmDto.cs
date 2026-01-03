namespace PerfumeTracker.Server.Dto;

public record PerfumeLlmDto(
	Guid Id,
	string House,
	string PerfumeName,
	string Family,
	decimal Rating,
	int TimesWorn,
	string[] Tags,
	string? LastComment
);
