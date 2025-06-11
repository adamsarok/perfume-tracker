import { PerfumeWornDTO } from "@/dto/PerfumeWornDTO";
import { PerfumeWornUploadDTO } from "@/dto/PerfumeWornUploadDTO";
import { ActionResult } from "@/dto/ActionResult";
import { del, get, post } from "./axios-service";

export async function getWornBeforeID(cursor: number | null, pageSize: number) : Promise<PerfumeWornDTO[]> {
    const qry = `/perfume-events/worn-perfumes?cursor=${encodeURIComponent(cursor ?? 0)}&pageSize=${encodeURIComponent(pageSize)}`;
    const response = (await get<PerfumeWornDTO[]>(qry)).data;
    response.forEach(x => x.eventDate = new Date(x.eventDate));
    return response;
}

export async function getWornPerfumeIDs(dayFilter: number) : Promise<number[]> {
  const qry = `/perfume-events/worn-perfumes/${encodeURIComponent(dayFilter)}`;
  const response = await get<number[]>(qry);
  return response.data;
}

export async function deleteWear(
  id: string
): Promise<ActionResult> {
  await del<PerfumeWornDTO>(`/perfume-events/${encodeURIComponent(id)}`);
  return { ok: true };
}

export async function wearPerfume(id: string, date: Date, isRandomPerfume: boolean) : Promise<ActionResult> {
  const dto: PerfumeWornUploadDTO = {
    perfumeId: id,
    wornOn: date,
    type: 1,
    isRandomPerfume: isRandomPerfume,
  };
  const response = await post<PerfumeWornUploadDTO>(`/perfume-events`, dto);
  return { ok: true, id: response.data.perfumeId };
}