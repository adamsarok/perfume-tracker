'use server';

import { PERFUMETRACKER_API_ADDRESS as apiAddress } from "./conf";
import { PerfumeSettings } from "./settings-service";

export async function getPerfumeSuggestion(settings: PerfumeSettings) : Promise<number> {
    if (!apiAddress) throw new Error("PerfumeAPI address not set");
    const params = new URLSearchParams({
      dayFilter: encodeURIComponent(settings.dayFilter.toString()),
      minimumRating: encodeURIComponent(settings.minimumRating.toString()),
    });
    const qry = `${apiAddress}/perfumesuggesteds/?${params.toString()}`;
    const response = await fetch(qry);
    if (!response.ok) {
      throw new Error("Failed to fetch perfume suggestion");
    }
    const suggested: number = await response.json();
    return suggested;
}