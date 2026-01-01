import { PerfumeRatingDownloadDTO } from "./PerfumeRatingDownloadDTO";
import { TagDTO } from "./TagDTO";

export interface PerfumeDTO {
    id: string;
    house: string;
    perfumeName: string;
    family: string;
    ml: number;
    mlLeft: number;
    imageObjectKey: string;
    imageUrl: string;
    autumn: boolean;
    spring: boolean;
    summer: boolean;
    winter: boolean;
    tags: TagDTO[];
    ratings: PerfumeRatingDownloadDTO[];
}