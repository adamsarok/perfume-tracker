import { TagDTO } from "./TagDTO";

export interface PerfumeUploadDTO {
    id: string;
    house: string;
    perfumeName: string;
    family: string;
    parfumeur: string;
    ml: number;
    mlLeft: number;
    tags: TagDTO[]
}
