import { Perfume, Tag } from "@prisma/client";

export interface PerfumeWithWornStatsDTO {
    perfume: Perfume,
    wornTimes: number | undefined,
    lastWorn: Date | undefined,
    tags: Tag[]
}