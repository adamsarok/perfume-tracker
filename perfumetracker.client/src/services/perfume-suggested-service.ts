'use server';

import { PERFUMETRACKER_API_ADDRESS as apiAddress } from "./conf";

export async function getAlreadySuggestedPerfumeIds(dayFilter: number) : Promise<number[]> {
    if (!apiAddress) throw new Error("PerfumeAPI address not set");
    const qry = `${apiAddress}/perfumesuggesteds/${encodeURIComponent(dayFilter)}`;
    const response = await fetch(qry);
    if (!response.ok) {
      throw new Error("Failed to fetch already suggested perfumes");
    }
    const suggesteds: number[] = await response.json();
    return suggesteds;
}


export async function addSuggested(perfumeId: number) {
    console.log('addSuggested callsed');
    if (!apiAddress) throw new Error("PerfumeAPI address not set");
    const qry = `${apiAddress}/perfumesuggesteds/${encodeURIComponent(perfumeId)}`;
    const response = await fetch(qry, {
        method: "POST",
      });
    if (!response.ok) {
      throw new Error("Failed to add current perfume suggestion");
    }
    const suggesteds: number[] = await response.json();
    return suggesteds;
}