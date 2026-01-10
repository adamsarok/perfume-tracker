import { PerfumeWithWornStatsDTO } from "./PerfumeWithWornStatsDTO";
import { RecommendationStrategy } from "./RecommendationStrategy";

export interface PerfumeRecommendationDTO {
    recommendationId: string,
    perfume: PerfumeWithWornStatsDTO,
    strategy: RecommendationStrategy
}