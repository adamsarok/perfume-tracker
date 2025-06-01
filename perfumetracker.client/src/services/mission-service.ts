"use server";

import { UserMissionDto } from "@/dto/MissionDto";
import { env } from "process";

export async function getActiveMissions(): Promise<UserMissionDto[]> {
  if (!env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const qry = `${env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS}/missions/active`;
  const response = await fetch(qry);
  if (!response.ok) {
    throw new Error("Failed to fetch active missions");
  }
  const missions: UserMissionDto[] = await response.json();
  return missions;
}

export async function generateMissions(): Promise<void> {
  if (!env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const qry = `${env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS}/missions/generate`;
  const response = await fetch(qry, {
    method: "POST",
  });
  if (!response.ok) {
    throw new Error("Failed to generate missions");
  }
}
