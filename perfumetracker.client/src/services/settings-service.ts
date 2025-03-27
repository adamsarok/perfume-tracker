"use server";

import { ActionResult } from "@/dto/ActionResult";
import { PERFUMETRACKER_API_ADDRESS as apiAddress } from "./conf";

export interface PerfumeSettings {
  minimumRating: number;
  dayFilter: number;
  showMalePerfumes: boolean;
  showUnisexPerfumes: boolean;
  showFemalePerfumes: boolean;
  sprayAmount: number
}


export async function getSettings(): Promise<PerfumeSettings> { //TODO userID
  if (!apiAddress) throw new Error("PerfumeAPI address not set");
  const qry = `${apiAddress}/settings`;
  const response = await fetch(qry);
  if (!response.ok) {
    throw new Error("Failed to fetch settings");
  }
  const perfume: PerfumeSettings = await response.json();
  return perfume;
}

export async function updateSettings(
  perfume: PerfumeSettings
): Promise<ActionResult> {
  if (!apiAddress) throw new Error("PerfumeAPI address not set");
  const response = await fetch(`${apiAddress}/settings`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(perfume),
  });

  if (!response.ok)
    return { ok: false, error: `Error updating settings: ${response.status}` };

  const result = await response.json();
  console.log(result);
  return { ok: true };
}