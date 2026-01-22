import { PerfumeDTO } from "./PerfumeDTO";

export interface PerfumeWithWornStatsDTO {
    perfume: PerfumeDTO,
    lastWorn: Date | undefined,
    burnRatePerYearMl: number,
    yearsLeft: number,
    averageRating: number,
    lastComment: string,
}