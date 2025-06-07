import { RecommendationDownloadDTO } from "@/dto/RecommendationDownloadDTO";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import { get, post } from "./axios-service";

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
export async function getAlreadyRecommended(): Promise<
  RecommendationDownloadDTO[]
> {
  const qry = `/recommendations/`;
  const response = await get<RecommendationDownloadDTO[]>(qry);
  return response.data;
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
): Promise<string> {
  query = RemoveDiacritics(query);
  console.log(query);
  return await getOpenAIResponse(query);
  // if (recommendations) {
  //   const dto: RecommendationUploadDTO = {
  //     query,
  //     recommendations,
  //   };
  //   return await addRecommendation(dto); //TODO: move this to server side
  // } else {
  //   return Promise.reject(new Error("Open AI response is empty"));
  // }
}

async function getOpenAIResponse(query: string) {
  const qry = `/ai`;
  const response = (await post<string>(qry, query)).data;
  return response.replace(/\\n/g, '\n').substring(1, response.length - 2);
}