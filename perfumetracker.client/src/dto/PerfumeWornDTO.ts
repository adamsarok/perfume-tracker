import { TagDTO } from "./TagDTO";

export interface PerfumeWornDTO {
    id: number,
    perfumeId: number,
    perfumeImageObjectKey: string,
    perfumeImageUrl: string,
    perfumeHouse: string,
    perfumeName: string,
    perfumeTags: TagDTO[],
    wornOn: Date,
}