'use server';

import { PERFUMETRACKER_API_ADDRESS as apiAddress } from "./conf";

export async function getPerfumeRandom() : Promise<number> {
    if (!apiAddress) throw new Error("PerfumeAPI address not set");
    const qry = `${apiAddress}/random-perfumes`;
    const response = await fetch(qry);
    if (!response.ok) {
      throw new Error("Failed to fetch random perfume");
    }
    const suggested: number = await response.json();
    return suggested;
}