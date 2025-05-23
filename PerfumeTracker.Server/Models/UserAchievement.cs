namespace PerfumeTracker.Server.Models;

public class UserAchievement : Entity {
	public int Id { get; set; }
	public int AchievementId { get; set; }
	public virtual Achievement Achievement { get; set; } = null!;
	public virtual UserProfileNew UserProfile { get; set; } = null!;
	public bool IsRead { get; set; } = false;
}
