export enum RecommendationStrategy {
  ForgottenFavorite = "ForgottenFavorite",
  SimilarToLastUsed = "SimilarToLastUsed",
  Seasonal = "Seasonal",
  Random = "Random",
  LeastUsed = "LeastUsed",
  MoodOrOccasion = "MoodOrOccasion"
}

export const RecommendationStrategyLabels: Record<RecommendationStrategy, string> = {
  [RecommendationStrategy.ForgottenFavorite]: "Forgotten Favorite",
  [RecommendationStrategy.SimilarToLastUsed]: "Similar to Last Used",
  [RecommendationStrategy.Seasonal]: "Seasonal",
  [RecommendationStrategy.Random]: "Random",
  [RecommendationStrategy.LeastUsed]: "Least Used",
  [RecommendationStrategy.MoodOrOccasion]: "Mood or Occasion"
};
