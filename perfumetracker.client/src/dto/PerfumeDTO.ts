import { TagDTO } from "./TagDTO";

export interface PerfumeDTO {
    id: string;
    house: string;
    perfumeName: string;
    rating: number;
    notes: string;
    ml: number;
    mlLeft: number;
    imageObjectKey: string;
    imagerUrl: string;
    autumn: boolean;
    spring: boolean;
    summer: boolean;
    winter: boolean;
    tags: TagDTO[];
}