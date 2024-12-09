import { PerfumeDTO } from "./PerfumeDTO";
import { TagDTO } from "./TagDTO";

export interface PerfumeWithWornStatsDTO {
    perfume: PerfumeDTO,
    wornTimes: number | undefined,
    lastWorn: Date | undefined,
    tags: TagDTO[]
}