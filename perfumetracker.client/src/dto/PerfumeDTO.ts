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
    tags: TagDTO[];
    ratings: PerfumeRatingDownloadDTO[];
    wearCount: number | undefined;
}