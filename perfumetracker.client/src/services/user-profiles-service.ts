import { AxiosResult, get, put } from "./axios-service";
import { RecommendationStrategy } from "@/dto/RecommendationStrategy";

export interface UserProfile {
  minimumRating: number;
  dayFilter: number;
  showMalePerfumes: boolean;
  showUnisexPerfumes: boolean;
  showFemalePerfumes: boolean;
  sprayAmountFullSizeMl: number;
  sprayAmountSamplesMl: number;
  preferredRecommendationStrategies: RecommendationStrategy[];
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