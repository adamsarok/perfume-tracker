import { PerfumeWithWornStatsDTO } from "./PerfumeWithWornStatsDTO";

export interface PerfumeRecommendationDTO {
    perfume: PerfumeWithWornStatsDTO,
    recommendationStrategy: string
}