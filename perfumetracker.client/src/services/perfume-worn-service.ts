import { PerfumeWornDTO } from "@/dto/PerfumeWornDTO";
import { PerfumeWornUploadDTO } from "@/dto/PerfumeWornUploadDTO";
import { ActionResult } from "@/dto/ActionResult";
import { getImageUrl } from "@/components/r2-image";
import { env } from "process";
import { getPerfumeTrackerApiAddress } from "./conf-service";
import { api } from "./auth-service";

export async function getWornBeforeID(cursor: Date | null, pageSize: number) : Promise<PerfumeWornDTO[]> {
    const apiUrl = await getPerfumeTrackerApiAddress();
    const qry = `${apiUrl}/perfume-events/worn-perfumes?cursor=${encodeURIComponent(cursor ? cursor.toISOString() : "")}&pageSize=${encodeURIComponent(pageSize)}`;
   
    const response = (await api.get<PerfumeWornDTO[]>(qry)).data;
  
    //const worns: PerfumeWornDTO[] = await response.json();
    response.forEach(x => x.perfumeImageUrl = getImageUrl(x.perfumeImageObjectKey, env.NEXT_PUBLIC_R2_API_ADDRESS));
    return response;
}

export async function getWornPerfumeIDs(dayFilter: number) : Promise<number[]> {
  const apiUrl = await getPerfumeTrackerApiAddress();
  const qry = `${apiUrl}/perfume-events/worn-perfumes/${encodeURIComponent(dayFilter)}`;
  const response = await fetch(qry);
  if (!response.ok) {
    throw new Error("Failed to fetch already suggested perfumes");
  }
  const suggesteds: number[] = await response.json();
  return suggesteds;
}

export async function deleteWear(
  id: string
): Promise<ActionResult> {
  const apiUrl = await getPerfumeTrackerApiAddress();
  const response = await fetch(`${apiUrl}/perfume-events/${encodeURIComponent(id)}`, {
    method: "DELETE"
  });
  if (!response.ok) {
    return { ok: false, error: `Error deleting perfume wear record: ${response.status}` };
  }
  return { ok: true };
}

export async function wearPerfume(id: string, date: Date, isRandomPerfume: boolean) : Promise<ActionResult> {
  const apiUrl = await getPerfumeTrackerApiAddress();
  const dto: PerfumeWornUploadDTO = {
    perfumeId: id,
    wornOn: date,
    type: 1,
    isRandomPerfume: isRandomPerfume,
  };
  const response = await fetch(`${apiUrl}/perfume-events`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(dto),
  });
  if (!response.ok) {
    return { ok: false, error: `Error wearing perfume: ${response.status}` };
  }
  return { ok: true };
}