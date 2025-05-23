"use server";

import { ActionResult } from "@/dto/ActionResult";
import { PERFUMETRACKER_API_ADDRESS } from "./conf";

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
  if (!PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const qry = `${PERFUMETRACKER_API_ADDRESS}/user-profiles`;
  const response = await fetch(qry);
  if (!response.ok) {
    throw new Error("Failed to fetch user profile");
  }
  const perfume: UserProfile = await response.json();
  return perfume;
}

export async function updateUserProfile(
  userProfile: UserProfile
): Promise<ActionResult> {
  if (!PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const response = await fetch(`${PERFUMETRACKER_API_ADDRESS}/user-profiles`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(userProfile),
  });

  if (!response.ok)
    return { ok: false, error: `Error updating user profile: ${response.status}` };

  const result = await response.json();
  console.log(result);
  return { ok: true };
}