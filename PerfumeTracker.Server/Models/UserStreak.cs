namespace PerfumeTracker.Server.Models;

public class UserStreak : UserEntity {
	public DateTime StreakStartAt { get; set; }
	public DateTime LastProgressedAt { get; set; }
	public DateTime? StreakEndAt { get; set; }
	public int Progress { get; set; } = 0;
}

