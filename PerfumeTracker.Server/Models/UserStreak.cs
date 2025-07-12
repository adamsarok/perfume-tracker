namespace PerfumeTracker.Server.Models;

public class UserStreak : UserEntity {
	public enum StreakGoal {
		WearPerfume,
		TryNewPerfume, //TODO: add more
	}
	public enum StreakType {
		Daily,
		Weekly,
	}
	public StreakGoal Goal { get; set; } = StreakGoal.WearPerfume;
	public StreakType Type { get; set; } = StreakType.Daily;
	public DateTime? StreakStart { get; set; }
	public DateTime? StreakEnd { get; set; }
}

