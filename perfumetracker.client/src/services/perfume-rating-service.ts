import { ActionResult } from "@/dto/ActionResult";
import { del, get, post } from "./axios-service";
import { PerfumeRatingDownloadDTO } from "@/dto/PerfumeRatingDownloadDTO";
import { PerfumeRatingUploadDTO } from "@/dto/PerfumeRatingUploadDTO";
import { AxiosError } from "axios";


export async function deletePerfumeRating(
  perfumeId: string, ratingId: string
): Promise<ActionResult> {
  await del<PerfumeRatingDownloadDTO>(`/perfumes/${encodeURIComponent(perfumeId)}/ratings/${encodeURIComponent(ratingId)}`);
  return { ok: true };
}

export async function addPerfumeRating(perfumeId: string, rating: number, comment: string) : Promise<ActionResult> {
  try {
    const dto: PerfumeRatingUploadDTO = {
      perfumeId,
      rating,
      comment,
    };
    const response = await post<PerfumeRatingDownloadDTO>(`/perfumes/${encodeURIComponent(perfumeId)}/ratings`, dto);
    return { ok: true, id: response.data.perfumeId };
  } catch (error) {
    console.error('Error adding perfume rating:', error);
    if (error instanceof AxiosError) {
      return { 
        ok: false, 
        error: error.response?.data?.message || error.response?.statusText || 'Failed to add rating' 
      };
    }
    return { ok: false, error: 'Failed to add rating' };
  }
}

export async function getPerfumeRatings(perfumeId: string): Promise<PerfumeRatingDownloadDTO[]> {
  const qry = `/perfumes/${encodeURIComponent(perfumeId)}/ratings`;
  const response = await get<PerfumeRatingDownloadDTO[]>(qry);
  return response.data;
}