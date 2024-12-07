import { TagUploadDTO } from "./TagUploadDTO";

export interface PerfumeUploadDTO {
    id: number;
    house: string;
    perfumeName: string;
    rating: number;
    notes: string;
    ml: number;
    imageObjectKey: string;
    autumn: boolean;
    spring: boolean;
    summer: boolean;
    winter: boolean;
    tags: TagUploadDTO[]
}