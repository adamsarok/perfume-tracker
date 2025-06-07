import { UserMissionDto } from "@/dto/MissionDto";
import { get, post } from "./axios-service";

export async function getActiveMissions(): Promise<UserMissionDto[]> {
  const qry = `/missions/active`;
  const response = await get<UserMissionDto[]>(qry);
  return response.data;
}

export async function generateMissions(): Promise<void> {
  const qry = `/missions/generate`;
  await post(qry, null);
}
