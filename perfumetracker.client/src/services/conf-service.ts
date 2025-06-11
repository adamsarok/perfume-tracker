"use server";

import { env } from "process";

export async function getPerfumeTrackerApiAddress(): Promise<string | undefined> {
    return env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS;
}