using System;

namespace PerfumeTracker.Server.Models;

public class UserMission : UserEntity {
    public Guid MissionId { get; set; }
    public int Progress { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public virtual UserProfile User { get; set; } = null!;
    public virtual Mission Mission { get; set; } = null!;
	public int XP_Awarded { get; set; } = 0;
} 