import { PerfumeWithWornStatsDTO } from "./PerfumeWithWornStatsDTO";

export interface PerfumeRecommendationDTO {
    recommendationId: string,
    perfume: PerfumeWithWornStatsDTO,
    strategy: string
}