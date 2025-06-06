import { PerfumeUploadDTO } from "@/dto/PerfumeUploadDTO";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import { ImageGuidDTO } from "@/dto/ImageGuidDTO";
import { ActionResult } from "@/dto/ActionResult";
import { getImageUrl } from "@/components/r2-image";
import { getPerfumeTrackerApiAddress } from "./conf-service";
import { api } from "./auth-service";

export async function getPerfumesFulltext(
  fulltext: string
): Promise<PerfumeWithWornStatsDTO[]> {
  const apiUrl = await getPerfumeTrackerApiAddress();
  if (!apiUrl) throw new Error("PerfumeAPI address not set");
  
  const qry = `${apiUrl}/perfumes/${fulltext ? `fulltext/${fulltext}` : ""}`;
  const response = await api.get<PerfumeWithWornStatsDTO[]>(qry);
  return response.data;
}

export async function getPerfume(id: string): Promise<PerfumeWithWornStatsDTO> {
  const apiUrl = await getPerfumeTrackerApiAddress();
  if (!apiUrl) throw new Error("PerfumeAPI address not set");
  
  const qry = `${apiUrl}/perfumes/${encodeURIComponent(id)}`;
  const response = await api.get<PerfumeWithWornStatsDTO>(qry);
  response.data.perfume.imagerUrl = getImageUrl(response.data.perfume.imageObjectKey, process.env.NEXT_PUBLIC_R2_API_ADDRESS);
  return response.data;
}

export async function getPerfumes(): Promise<PerfumeWithWornStatsDTO[]> {
  const apiUrl = await getPerfumeTrackerApiAddress();
  if (!apiUrl) throw new Error("PerfumeAPI address not set");
  
  const qry = `${apiUrl}/perfumes/`;
  const response = await api.get<PerfumeWithWornStatsDTO[]>(qry);
  response.data.forEach(x => x.perfume.imagerUrl = getImageUrl(x.perfume.imageObjectKey, process.env.NEXT_PUBLIC_R2_API_ADDRESS));
  return response.data;
}

export async function addPerfume(
  perfume: PerfumeUploadDTO
): Promise<ActionResult> {
  const apiUrl = await getPerfumeTrackerApiAddress();
  if (!apiUrl) throw new Error("PerfumeAPI address not set");
  
  const response = await api.post<PerfumeUploadDTO>(`${apiUrl}/perfumes`, perfume);
  return { ok: true, id: response.data.id };
}

export async function updatePerfume(
  perfume: PerfumeUploadDTO
): Promise<ActionResult> {
  const apiUrl = await getPerfumeTrackerApiAddress();
  if (!apiUrl) throw new Error("PerfumeAPI address not set");
  
  await api.put(`${apiUrl}/perfumes/${perfume.id}`, perfume);
  return { ok: true, id: perfume.id };
}

export async function updateImageGuid(
  perfumeId: string, imageGuid: string
): Promise<ActionResult> {
  const apiUrl = await getPerfumeTrackerApiAddress();
  if (!apiUrl) throw new Error("PerfumeAPI address not set");
  
  const dto: ImageGuidDTO = { parentObjectId: perfumeId, imageObjectKey: imageGuid };
  await api.put(`${apiUrl}/perfumes/imageguid`, dto);
  return { ok: true, id: perfumeId };
}

export async function deletePerfume(id: string): Promise<ActionResult> {
  const apiUrl = await getPerfumeTrackerApiAddress();
  if (!apiUrl) throw new Error("PerfumeAPI address not set");
  
  await api.delete(`${apiUrl}/perfumes/${id}`);
  return { ok: true, id: id };
}
