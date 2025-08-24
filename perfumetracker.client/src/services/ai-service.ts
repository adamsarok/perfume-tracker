import { RecommendationDownloadDTO } from "@/dto/RecommendationDownloadDTO";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import { AxiosResult, get, post } from "./axios-service";
import { Axios } from "axios";

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

// export async function addRecommendation(
//   recommendation: RecommendationUploadDTO
// ): Promise<RecommendationDownloadDTO> {
//   if (!apiAddress) throw new Error("PerfumeAPI address not set");
//   const qry = `${apiAddress}/recommendations/`;
//   const response = await fetch(qry, {
//     method: "POST",
//     headers: {
//       "Content-Type": "application/json",
//     },
//     body: JSON.stringify(recommendation),
//   });
//   if (!response.ok) {
//     throw new Error("Failed to add recommendation");
//   }
//   const r: RecommendationDownloadDTO = await response.json();
//   return r;
// }

function RemoveDiacritics(input: string) {
  return input.normalize("NFD").replace(/[\u0300-\u036f]/g, "");
}

export default async function getAiRecommendations(
  query: string
): Promise<AxiosResult<string>> {
  query = RemoveDiacritics(query);
  return post<string>('/ai', query);
}