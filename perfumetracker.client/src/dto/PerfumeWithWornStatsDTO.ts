import { PerfumeDTO } from "./PerfumeDTO";

export interface PerfumeWithWornStatsDTO {
    perfume: PerfumeDTO,
    wornTimes: number | undefined,
    lastWorn: Date | undefined,
    burnRatePerYearMl: number,
    yearsLeft: number
}