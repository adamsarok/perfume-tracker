"use server";

import { DashbouardUrlDTO } from "@/dto/DashboardUrlDTO";
import { PERFUMETRACKER_API_ADDRESS } from "./conf";

export async function getDashboardUrl(): Promise<string> {
  if (!PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const response = await fetch(`${PERFUMETRACKER_API_ADDRESS}/metabase`);
  if (!response.ok) {
    //TODO: fix unhandled errors
    throw new Error("Failed to fetch Metabase dashboard Url");
  }
  const data: DashbouardUrlDTO = await response.json();
  console.log(data);
  return data.iframeUrl;
}