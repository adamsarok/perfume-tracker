"use server";

import { RecommendationDownloadDTO } from "@/dto/RecommendationDownloadDTO";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import { env } from "process";

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
  if (!env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const qry = `${env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS}/recommendations/`;
  const response = await fetch(qry);
  if (!response.ok) {
    throw new Error("Failed to fetch previous recommendations");
  }
  const r: RecommendationDownloadDTO[] = await response.json();
  return r;
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
  if (!env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const qry = `${env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS}/ai`;
  console.log(qry);
  const response = await fetch(qry, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ query }),
  });
  console.log(response);
  if (!response.ok) {
    throw new Error("Failed to fetch ai recommendations");
  }
  const text = await response.text();
  return text.replace(/\\n/g, '\n').substring(1, text.length - 2);
}