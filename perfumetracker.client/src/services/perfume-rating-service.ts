import { ActionResult } from "@/dto/ActionResult";
import { del, post } from "./axios-service";
import { PerfumeRatingDownloadDTO } from "@/dto/PerfumeRatingDownloadDTO";
import { PerfumeRatingUploadDTO } from "@/dto/PerfumeRatingUploadDTO";


export async function deleteRating(
  perfumeId: string, ratingId: string
): Promise<ActionResult> {
  await del<PerfumeRatingDownloadDTO>(`/perfumes/${encodeURIComponent(perfumeId)}/ratings/${encodeURIComponent(ratingId)}`);
  return { ok: true };
}

export async function ratePerfume(perfumeId: string, rating: number, comment: string) : Promise<ActionResult> {
  const dto: PerfumeRatingUploadDTO = {
    perfumeId,
    rating,
    comment,
  };
  const response = await post<PerfumeRatingDownloadDTO>(`/perfumes/${encodeURIComponent(perfumeId)}/ratings`, dto);
  return { ok: true, id: response.data.perfumeId };
}