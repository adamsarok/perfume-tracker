using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace PerfumeTracker.Server.Models;
public class UserProfile : Entity {
	public UserProfile() { }
	public UserProfile(Guid userId, string userName, string email) {
		Id = userId;
		UserName = userName;
		Email = email;
		MinimumRating = 8;
		DayFilter = 30;
		ShowFemalePerfumes = true;
		ShowMalePerfumes = true;
		ShowUnisexPerfumes = true;
		SprayAmountFullSizeMl = 0.2M; //0.1 ml * 2 sprays
		SprayAmountSamplesMl = 0.1M;  //0.05 ml * 2 sprays
	}
	public string UserName { get; set; } = null!;
	public string Email { get; set; } = null!;
	public int XP { get; set; } = 0;
	public double MinimumRating { get; set; }
	public int DayFilter { get; set; }
	public bool ShowMalePerfumes { get; set; }
	public bool ShowUnisexPerfumes { get; set; }
	public bool ShowFemalePerfumes { get; set; }
	public decimal SprayAmountFullSizeMl { get; set; }
	public decimal SprayAmountSamplesMl { get; set; }
	public decimal SprayAmountForBottleSize(decimal bottleSizeMl) => bottleSizeMl >= 20 ? SprayAmountFullSizeMl : SprayAmountSamplesMl;
	public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
	public virtual ICollection<UserMission> UserMissions { get; set; } = new List<UserMission>();
}
