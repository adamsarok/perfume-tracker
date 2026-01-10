import { RecommendationStrategy } from "@/dto/RecommendationStrategy";
import { Calendar, ChartNoAxesCombined, Dice5, PartyPopper, Sparkles, Star } from "lucide-react";

export const getRecommendationIcon = (type?: RecommendationStrategy) => { 
  switch (type) {
    case RecommendationStrategy.ForgottenFavorite:
      return <Star className="w-5 h-5 text-yellow-500" />;
    case RecommendationStrategy.SimilarToLastUsed:
      return <Sparkles className="w-5 h-5 text-pink-500" />;
    case RecommendationStrategy.Seasonal:
      return <Calendar className="w-5 h-5 text-blue-500" />;
    case RecommendationStrategy.Random:
      return <Dice5 className="w-5 h-5 text-purple-500" />;
    case RecommendationStrategy.LeastUsed:
      return <ChartNoAxesCombined className="w-5 h-5 text-gray-500" />;
    case RecommendationStrategy.MoodOrOccasion:
      return <PartyPopper className="w-5 h-5 text-gray-500" />;
    default:
      return null;
  }
};