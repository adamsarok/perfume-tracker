import { PerfumeDTO } from "./PerfumeDTO";

export interface PerfumeWithWornStatsDTO {
    perfume: PerfumeDTO,
    burnRatePerYearMl: number,
    yearsLeft: number,
    lastComment: string,
}
