import { AxiosResult, get, put } from "./axios-service";

export interface UserProfile {
  minimumRating: number;
  dayFilter: number;
  showMalePerfumes: boolean;
  showUnisexPerfumes: boolean;
  showFemalePerfumes: boolean;
  sprayAmountFullSizeMl: number;
  sprayAmountSamplesMl: number;
}

export async function getUserProfile(): Promise<AxiosResult<UserProfile>> {
  const qry = `/user-profiles`;
  return get<UserProfile>(qry);
}

export async function updateUserProfile(
  userProfile: UserProfile
): Promise<AxiosResult<unknown>> {
  return put(`/user-profiles`, userProfile);
}