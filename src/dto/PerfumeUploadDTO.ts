import { Perfume, Tag } from "@prisma/client";

export interface PerfumeUploadDTO {
    perfume: Perfume,
    wornTimes: number | undefined,
    lastWorn: Date | undefined,
    tags: Tag[]
}