namespace PerfumeTracker.Server.Models;
public class UserProfile {
	public int Id { get; set; }
	public string UserName { get; set; } = null!;
	public string Email { get; set; } = null!;
	public int XP { get; set; } = 0;
	public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}
