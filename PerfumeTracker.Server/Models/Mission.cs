using System;

namespace PerfumeTracker.Server.Models;

public class Mission : Entity {
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int XP { get; set; }
    public MissionType Type { get; set; }
    public int? RequiredCount { get; set; }
    public Guid? RequiredId { get; set; }
    public bool IsActive { get; set; }
	public virtual ICollection<UserMission> UserMissions { get; set; } = new List<UserMission>();
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