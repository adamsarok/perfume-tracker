import { UserMissionDto } from "@/dto/MissionDto";
import { AxiosResult, get, post } from "./axios-service";

export async function getActiveMissions(): Promise<AxiosResult<UserMissionDto[]>> {
  const qry = `/missions/active`;
  return get<UserMissionDto[]>(qry);
}

export async function generateMissions(): Promise<void> {
  const qry = `/missions/generate`;
  await post(qry, null);
}
