import { TagDTO } from "./TagDTO";

export interface PerfumeUploadDTO {
    id: string;
    house: string;
    perfumeName: string;
    family: string;
    ml: number;
    mlLeft: number;
    tags: TagDTO[]
}