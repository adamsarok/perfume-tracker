import { TagDTO } from "./TagDTO";

export interface GlobalPerfumeDTO {
    id: string;
    house: string;
    perfumeName: string;
    family: string;
    tags: TagDTO[];
}