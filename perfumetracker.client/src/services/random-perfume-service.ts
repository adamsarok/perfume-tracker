'use server';

import { env } from "process";

export async function getPerfumeRandom() : Promise<string> {
    if (!env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
    const qry = `${env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS}/random-perfumes`;
    const response = await fetch(qry);
    if (!response.ok) {
      throw new Error("Failed to fetch random perfume");
    }
    const suggested: string = await response.json();
    return suggested;
}