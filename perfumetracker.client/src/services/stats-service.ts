import { AxiosResult, get } from "./axios-service";

export async function getCurrentUserStats(): Promise<AxiosResult<UserStatsDTO>> {
  const qry = `/users/stats`;
  return get<UserStatsDTO>(qry);
}

export interface FavoritePerfumeDTO {
  id: string;
  house: string;
  perfumeName: string;
  averageRating: number;
  wearCount: number;
}

export interface FavoriteTagDTO {
  id: string;
  tagName: string;
  color: string;
  wearCount: number;
  totalMl: number;
}

export interface PerfumeRecommendationStatsDTO {
  strategy: string;
  totalRecommendations: number;
  acceptedRecommendations: number;
}

export interface UserStatsDTO {
  startDate: string | null;
  lastWear: string | null;
  wearCount: number;
  perfumesCount: number;
  totalPerfumesMl: number;
  totalPerfumesMlLeft: number;
  monthlyUsageMl: number;
  yearlyUsageMl: number;
  favoritePerfumes: FavoritePerfumeDTO[];
  favoriteTags: FavoriteTagDTO[];
  currentStreak: number | null;
  bestStreak: number | null;
  recommendationStats: PerfumeRecommendationStatsDTO[];
}