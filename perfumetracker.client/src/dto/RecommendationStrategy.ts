export enum RecommendationStrategy {
  ForgottenFavorite = 0,
  SimilarToLastUsed = 1,
  Seasonal = 2,
  Random = 3,
  LeastUsed = 4,
  MoodOrOccasion = 5
}

export const RecommendationStrategyLabels: Record<RecommendationStrategy, string> = {
  [RecommendationStrategy.ForgottenFavorite]: "Forgotten Favorite",
  [RecommendationStrategy.SimilarToLastUsed]: "Similar to Last Used",
  [RecommendationStrategy.Seasonal]: "Seasonal",
  [RecommendationStrategy.Random]: "Random",
  [RecommendationStrategy.LeastUsed]: "Least Used",
  [RecommendationStrategy.MoodOrOccasion]: "Mood or Occasion"
};
