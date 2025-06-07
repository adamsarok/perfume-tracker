import { ActionResult } from "@/dto/ActionResult";
import { get, put } from "./axios-service";

export interface UserProfile {
  minimumRating: number;
  dayFilter: number;
  showMalePerfumes: boolean;
  showUnisexPerfumes: boolean;
  showFemalePerfumes: boolean;
  sprayAmountFullSizeMl: number;
  sprayAmountSamplesMl: number;
}

export async function getUserProfile(): Promise<UserProfile> {
  const qry = `/user-profiles`;
  const response = await get<UserProfile>(qry);
  return response.data;
}

export async function updateUserProfile(
  userProfile: UserProfile
): Promise<ActionResult> {
  await put(`/user-profiles`, userProfile);
  return { ok: true };
}