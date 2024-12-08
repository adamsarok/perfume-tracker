import { PerfumeDTO } from "./PerfumeDTO";
import { TagDTO } from "./TagDTO";

export interface PerfumeWornDTO {
    id: number,
    perfume: PerfumeDTO,
    wornOn: Date,
    tags: TagDTO[]
}