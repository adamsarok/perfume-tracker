import { PerfumeUploadDTO } from "@/dto/PerfumeUploadDTO";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import { ImageGuidDTO } from "@/dto/ImageGuidDTO";
import { ActionResult } from "@/dto/ActionResult";
import { del, get, post, put } from "./axios-service";

export async function getPerfumesFulltext(
  fulltext: string
): Promise<PerfumeWithWornStatsDTO[]> {
 
  const qry = `/perfumes/${fulltext ? `fulltext/${fulltext}` : ""}`;
  const response = await get<PerfumeWithWornStatsDTO[]>(qry);
  return response.data;
}

export async function getPerfume(id: string): Promise<PerfumeWithWornStatsDTO> {
  const qry = `/perfumes/${encodeURIComponent(id)}`;
  const response = await get<PerfumeWithWornStatsDTO>(qry);
  return response.data;
}

export async function getNextPerfumeId(id: string): Promise<string> {
  const qry = `/perfumes/${encodeURIComponent(id)}/next`;
  const response = await get<string>(qry);
  return response.data;
}

export async function getPreviousPerfumeId(id: string): Promise<string> {
  const qry = `/perfumes/${encodeURIComponent(id)}/previous`;
  const response = await get<string>(qry);
  return response.data;
}

export async function getPerfumes(): Promise<PerfumeWithWornStatsDTO[]> {
  const qry = `/perfumes/`;
  const response = await get<PerfumeWithWornStatsDTO[]>(qry);
  return response.data;
}

export async function addPerfume(
  perfume: PerfumeUploadDTO
): Promise<ActionResult> {
  const response = await post<PerfumeUploadDTO>(`/perfumes`, perfume);
  return { ok: true, id: response.data.id };
}

export async function updatePerfume(
  perfume: PerfumeUploadDTO
): Promise<ActionResult> {
  await put(`/perfumes/${perfume.id}`, perfume);
  return { ok: true, id: perfume.id };
}

export async function updateImageGuid(
  perfumeId: string, imageGuid: string
): Promise<ActionResult> {
  const dto: ImageGuidDTO = { parentObjectId: perfumeId, imageObjectKey: imageGuid };
  await put(`/perfumes/imageguid`, dto);
  return { ok: true, id: perfumeId };
}

export async function deletePerfume(id: string): Promise<ActionResult> {
  await del(`/perfumes/${id}`);
  return { ok: true, id: id };
}
