using System;

namespace PerfumeTracker.Server.Models;

public class Mission : Entity {
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int XP { get; set; }
    public MissionType Type { get; set; }
    public int? RequiredCount { get; set; }
    public int? RequiredId { get; set; }
    public bool IsActive { get; set; }
}

public enum MissionType {
    WearPerfumes,
	WearSamePerfume,
	GetRandoms,
    AcceptRandoms,
	PerfumesTaggedPhotographed,
	UseUnusedPerfumes,
    WearDifferentPerfumes,
	WearNote,
}