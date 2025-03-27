'use server';

import { PERFUMETRACKER_API_ADDRESS as apiAddress } from "./conf";

export async function getPerfumeSuggestion() : Promise<number> {
    if (!apiAddress) throw new Error("PerfumeAPI address not set");
    const qry = `${apiAddress}/perfumesuggesteds`;
    const response = await fetch(qry);
    if (!response.ok) {
      throw new Error("Failed to fetch perfume suggestion");
    }
    const suggested: number = await response.json();
    return suggested;
}