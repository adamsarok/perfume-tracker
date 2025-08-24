import { PerfumeWornDTO } from "@/dto/PerfumeWornDTO";
import { PerfumeWornUploadDTO } from "@/dto/PerfumeWornUploadDTO";
import { AxiosResult, del, get, post } from "./axios-service";

export async function getWornBeforeID(cursor: number | null, pageSize: number) : Promise<AxiosResult<PerfumeWornDTO[]>> {
    const qry = `/perfume-events/worn-perfumes?cursor=${encodeURIComponent(cursor ?? 0)}&pageSize=${encodeURIComponent(pageSize)}`;
    return get<PerfumeWornDTO[]>(qry);
}

export async function getWornPerfumeIDs(dayFilter: number) : Promise<AxiosResult<number[]>> {
  const qry = `/perfume-events/worn-perfumes/${encodeURIComponent(dayFilter)}`;
  return get<number[]>(qry);
}

export async function deleteWear(
  id: string
): Promise<AxiosResult<PerfumeWornDTO>> {
  return del<PerfumeWornDTO>(`/perfume-events/${encodeURIComponent(id)}`);
}

export async function wearPerfume(id: string, date: Date, isRandomPerfume: boolean) : Promise<AxiosResult<PerfumeWornUploadDTO>> {
  const dto: PerfumeWornUploadDTO = {
    perfumeId: id,
    wornOn: date,
    type: 1,
    isRandomPerfume: isRandomPerfume,
  };
  return post<PerfumeWornUploadDTO>(`/perfume-events`, dto);
}