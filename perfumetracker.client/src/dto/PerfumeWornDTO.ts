import { TagDTO } from "./TagDTO";

export interface PerfumeWornDTO {
    id: string,
    perfumeId: string,
    perfumeImageObjectKey: string,
    perfumeImageUrl: string,
    perfumeHouse: string,
    perfumeName: string,
    perfumeTags: TagDTO[],
    eventDate: Date,
    sequenceNumber: number
}