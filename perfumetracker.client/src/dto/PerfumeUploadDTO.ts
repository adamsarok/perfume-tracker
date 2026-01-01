import { TagDTO } from "./TagDTO";

export interface PerfumeUploadDTO {
    id: string;
    house: string;
    perfumeName: string;
    family: string;
    ml: number;
    mlLeft: number;
    autumn: boolean;
    spring: boolean;
    summer: boolean;
    winter: boolean;
    tags: TagDTO[]
}