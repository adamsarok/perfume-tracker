"use server";

import { PerfumeWornDTO } from "@/db/perfume-worn-repo";
import { PERFUMETRACKER_API_ADDRESS as apiAddress } from "./conf";

export async function getPerfumes(fulltext: string): Promise<PerfumeWornDTO[]> {
  if (!apiAddress) throw new Error("PerfumeAPI address not set");
  const qry = `${apiAddress}/perfumes/${
    fulltext ? `fulltext/${fulltext}` : ""
  }`;
  const response = await fetch(qry);
  if (!response.ok) {
    throw new Error("Failed to fetch perfumes");
  }
  const perfumes: PerfumeWornDTO[] = await response.json();
  return perfumes;
}
