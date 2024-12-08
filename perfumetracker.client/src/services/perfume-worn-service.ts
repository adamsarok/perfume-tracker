'use server';

import { PerfumeWornDTO } from "@/dto/PerfumeWornDTO";
import { PERFUMETRACKER_API_ADDRESS as apiAddress } from "./conf";

export async function getWornBeforeID(cursor: number | null, pageSize: number) : Promise<PerfumeWornDTO[]> {
    if (!apiAddress) throw new Error("PerfumeAPI address not set");
    const qry = `${apiAddress}/perfumeworns?cursor=${encodeURIComponent(cursor ?? 0)}&pageSize=${encodeURIComponent(pageSize)}`;
    const response = await fetch(qry);
    if (!response.ok) {
      throw new Error("Failed to fetch perfume worns");
    }
    const worns: PerfumeWornDTO[] = await response.json();
    return worns;
}

export async function getWornPerfumeIDs(dayFilter: number) : Promise<number[]> {
  if (!apiAddress) throw new Error("PerfumeAPI address not set");
  const qry = `${apiAddress}/perfumeworns/perfumesbefore/${encodeURIComponent(dayFilter)}`;
  const response = await fetch(qry);
  if (!response.ok) {
    throw new Error("Failed to fetch already suggested perfumes");
  }
  const suggesteds: number[] = await response.json();
  return suggesteds;
}
