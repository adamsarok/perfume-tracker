import { RecommendationDownloadDTO } from "@/dto/RecommendationDownloadDTO";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import { AxiosResult, get, post } from "./axios-service";

export interface TagWithCount {
  id: string;
  tag: string;
  color: string;
  wornCount: number;
}

export interface UserPreference {
  perfumes: PerfumeWithWornStatsDTO[] | null; //not exactly correct as the worncount is total, not 3 days...
  tags: TagWithCount[] | null;
}

export interface UserPreferences {
  last3perfumes: UserPreference;
  allTime: UserPreference;
}

//TODO: paging
export async function getAlreadyRecommended(): Promise<AxiosResult<RecommendationDownloadDTO[]>> {
  const qry = `/recommendations/`;
  return get<RecommendationDownloadDTO[]>(qry);
}

function RemoveDiacritics(input: string) {
  return input.normalize("NFD").replace(/[\u0300-\u036f]/g, "");
}

export default async function getAiRecommendations(
  query: string
): Promise<AxiosResult<string>> {
  query = RemoveDiacritics(query);
  return post<string>('/ai', query);
}