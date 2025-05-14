namespace PerfumeTracker.Server.Models;
public class UserProfile : Entity {
	public int Id { get; set; }
	public string UserName { get; set; } = null!;
	public string Email { get; set; } = null!;
	public int XP { get; set; } = 0;
	public int UserId { get; set; }
	public double MinimumRating { get; set; }
	public int DayFilter { get; set; }
	public bool ShowMalePerfumes { get; set; }
	public bool ShowUnisexPerfumes { get; set; }
	public bool ShowFemalePerfumes { get; set; }
	public decimal SprayAmountFullSizeMl { get; set; }
	public decimal SprayAmountSamplesMl { get; set; }
	public decimal SprayAmountForBottleSize(decimal bottleSizeMl) => bottleSizeMl >= 20 ? SprayAmountFullSizeMl : SprayAmountSamplesMl;
	public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}
