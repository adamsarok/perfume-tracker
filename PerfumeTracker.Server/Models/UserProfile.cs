using PerfumeTracker.Server.Features.Perfumes.Services;

namespace PerfumeTracker.Server.Models;

public class UserProfile : Entity {
	public UserProfile() { }
	public UserProfile(Guid userId, string userName, string email) {
		Id = userId;
		MinimumRating = 8;
		DayFilter = 30;
		ShowFemalePerfumes = true;
		ShowMalePerfumes = true;
		ShowUnisexPerfumes = true;
		SprayAmountFullSizeMl = 0.2M; //0.1 ml * 2 sprays
		SprayAmountSamplesMl = 0.1M;  //0.05 ml * 2 sprays
		PreferredRecommendationStrategies = new List<RecommendationStrategy> {
			RecommendationStrategy.ForgottenFavorite,
			RecommendationStrategy.SimilarToLastUsed,
			RecommendationStrategy.Seasonal,
			RecommendationStrategy.Random,
			RecommendationStrategy.LeastUsed
		};
	}
	public decimal MinimumRating { get; set; }
	public int DayFilter { get; set; }
	public bool ShowMalePerfumes { get; set; }
	public bool ShowUnisexPerfumes { get; set; }
	public bool ShowFemalePerfumes { get; set; }
	public decimal SprayAmountFullSizeMl { get; set; }
	public decimal SprayAmountSamplesMl { get; set; }
	public List<RecommendationStrategy> PreferredRecommendationStrategies { get; set; } = new();
	public decimal SprayAmountForBottleSize(decimal bottleSizeMl) => bottleSizeMl >= 20 ? SprayAmountFullSizeMl : SprayAmountSamplesMl;
	public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
	public virtual ICollection<UserMission> UserMissions { get; set; } = new List<UserMission>();
	public string Timezone { get; set; } = "UTC"; //TODO: set per tenant timezone for nicer streak handling
}
