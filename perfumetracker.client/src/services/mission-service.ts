"use server";

import { UserMissionDto } from "@/dto/MissionDto";
import { PERFUMETRACKER_API_ADDRESS as apiAddress } from "./conf";

export async function getActiveMissions(): Promise<UserMissionDto[]> {
  if (!apiAddress) throw new Error("PerfumeAPI address not set");
  const qry = `${apiAddress}/missions/active`;
  const response = await fetch(qry);
  if (!response.ok) {
    throw new Error("Failed to fetch active missions");
  }
  const missions: UserMissionDto[] = await response.json();
  return missions;
}

export async function generateMissions(): Promise<void> {
  if (!apiAddress) throw new Error("PerfumeAPI address not set");
  const qry = `${apiAddress}/missions/generate`;
  const response = await fetch(qry, {
    method: "POST",
  });
  if (!response.ok) {
    throw new Error("Failed to generate missions");
  }
}
