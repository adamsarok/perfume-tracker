using System;

namespace PerfumeTracker.Server.Models;

public class UserMission : Entity {
    public int Id { get; set; }
    public int UserId { get; set; }
    public int MissionId { get; set; }
    public int Progress { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public virtual UserProfile User { get; set; } = null!;
    public virtual Mission Mission { get; set; } = null!;
} 