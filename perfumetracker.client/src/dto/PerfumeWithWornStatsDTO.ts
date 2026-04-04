import { PerfumeDTO } from "./PerfumeDTO";

export interface PerfumeWithWornStatsDTO {
    perfume: PerfumeDTO,
    burnRatePerYearMl: number,
    yearsLeft: number,
    averageRating: number,
    lastComment: string,
}