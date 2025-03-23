import { TagDTO } from "./TagDTO";

export interface PerfumeDTO {
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
    tags: TagDTO[];
    percentLeft: number;
}