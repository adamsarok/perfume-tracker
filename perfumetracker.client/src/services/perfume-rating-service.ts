import { AxiosResult, del, get, post } from "./axios-service";
import { PerfumeRatingDownloadDTO } from "@/dto/PerfumeRatingDownloadDTO";
import { PerfumeRatingUploadDTO } from "@/dto/PerfumeRatingUploadDTO";


export async function deletePerfumeRating(
  perfumeId: string, ratingId: string
): Promise<AxiosResult<unknown>> {
  return del(`/perfumes/${encodeURIComponent(perfumeId)}/ratings/${encodeURIComponent(ratingId)}`);
}

export async function addPerfumeRating(perfumeId: string, rating: number, comment: string) : Promise<AxiosResult<PerfumeRatingDownloadDTO>> {
    const dto: PerfumeRatingUploadDTO = {
      perfumeId,
      rating,
      comment,
    };
    return post<PerfumeRatingDownloadDTO>(`/perfumes/${encodeURIComponent(perfumeId)}/ratings`, dto);
}

export async function getPerfumeRatings(perfumeId: string): Promise<AxiosResult<PerfumeRatingDownloadDTO[]>> {
  const qry = `/perfumes/${encodeURIComponent(perfumeId)}/ratings`;
  return get<PerfumeRatingDownloadDTO[]>(qry);
}