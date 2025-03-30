'use server';

import { PerfumeWornDTO } from "@/dto/PerfumeWornDTO";
import { PERFUMETRACKER_API_ADDRESS, R2_API_ADDRESS } from "./conf";
import { PerfumeWornUploadDTO } from "@/dto/PerfumeWornUploadDTO";
import { ActionResult } from "@/dto/ActionResult";
import { getImageUrl } from "@/components/r2-image";

export async function getWornBeforeID(cursor: number | null, pageSize: number) : Promise<PerfumeWornDTO[]> {
    if (!PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
    const qry = `${PERFUMETRACKER_API_ADDRESS}/perfumeworns?cursor=${encodeURIComponent(cursor ?? 0)}&pageSize=${encodeURIComponent(pageSize)}`;
    const response = await fetch(qry);
    if (!response.ok) {
      throw new Error("Failed to fetch perfume worns");
    }
    const worns: PerfumeWornDTO[] = await response.json();
    worns.forEach(x => x.perfume.imagerUrl = getImageUrl(x.perfume.imageObjectKey, R2_API_ADDRESS));
    return worns;
}

export async function getWornPerfumeIDs(dayFilter: number) : Promise<number[]> {
  if (!PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const qry = `${PERFUMETRACKER_API_ADDRESS}/perfumeworns/perfumesbefore/${encodeURIComponent(dayFilter)}`;
  const response = await fetch(qry);
  if (!response.ok) {
    throw new Error("Failed to fetch already suggested perfumes");
  }
  const suggesteds: number[] = await response.json();
  return suggesteds;
}

export async function deleteWear(
  id: number
): Promise<ActionResult> {
  if (!PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const response = await fetch(`${PERFUMETRACKER_API_ADDRESS}/perfumeworns/${encodeURIComponent(id)}`, {
    method: "DELETE"
  });
  if (!response.ok) {
    return { ok: false, error: `Error deleting perfume wear record: ${response.status}` };
  }
  return { ok: true };
}

export async function wearPerfume(id: number, date: Date) : Promise<ActionResult> {
  if (!PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const dto: PerfumeWornUploadDTO = {
    perfumeId: id,
    wornOn: date
  };
  const response = await fetch(`${PERFUMETRACKER_API_ADDRESS}/perfumeworns`, {
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