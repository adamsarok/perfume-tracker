"use server";

import { RecommendationDownloadDTO } from "@/dto/RecommendationDownloadDTO";
import { PERFUMETRACKER_API_ADDRESS as apiAddress } from "./conf";
import { getPerfumes } from "./perfume-service";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import { RecommendationUploadDTO } from "@/dto/RecommendationUploadDTO copy";
import { getOpenAIResponse } from "./openai-service";

export interface TagWithCount {
  id: number;
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

export async function getQuery(
  userPreferences: UserPreferences,
  buyOrWear: "buy" | "wear" | null,
  basedOnNotes: "notes" | "perfumes" | null
): Promise<string> {
  if (!userPreferences || !buyOrWear || !basedOnNotes) return "";
  let query;
  if (basedOnNotes === "perfumes") {
    if (!userPreferences.last3perfumes) return "";
    const last3perfumes =
      userPreferences.last3perfumes.perfumes
        ?.map((p) => `${p.perfume.house} - ${p.perfume.perfumeName}`)
        .join(", ") ?? "";
    query = `Based on these past choices: ${last3perfumes}, suggest 3 perfumes.`;
  } else {
    if (!userPreferences.last3perfumes.tags) return "";
    const last3perfumesTags =
      userPreferences.last3perfumes.tags?.map((t) => t.tag).join(", ") ?? "";
    query = `Based on these perfume notes: ${last3perfumesTags}, suggest 3 perfumes.`;
  }

  //we use less tokens if we group perfumes by house eg
  const ownedPerfumes = userPreferences.allTime.perfumes?.reduce((acc, p) => {
    const house = p.perfume.house;
    if (!acc[house]) {
      acc[house] = [];
    }
    acc[house].push(p.perfume.perfumeName);
    return acc;
  }, {} as Record<string, string[]>);
  const formattedOwnedPerfumes = ownedPerfumes
    ? Object.entries(ownedPerfumes)
        .map(([house, perfumes]) => `${house} { ${perfumes.join(", ")} }`)
        .join(", ")
    : "";
  if (buyOrWear === "buy")
    query += ` Suggest perfumes not in this list: ${formattedOwnedPerfumes}`;
  else
    query += ` Suggest perfumes only from this list: ${formattedOwnedPerfumes}`;
  return query;
}

function aggregatePerfumesTags(
  perfumes: PerfumeWithWornStatsDTO[]
): UserPreference {
  const result: UserPreference = {
    perfumes: [],
    tags: [],
  };

  //TODO: fix logical issue - worncount is calculated in repo and thus not filtered for 3 days

  const acc = perfumes.reduce((acc, perfume) => {
    perfume.tags.forEach((t) => {
      if (!acc[t.id]) {
        acc[t.id] = {
          id: t.id,
          tag: t.tagName,
          color: t.color,
          wornCount: 0,
        };
      }
      acc[t.id].wornCount += perfume.wornTimes ?? 0;
    });
    return acc;
  }, {} as Record<string, TagWithCount>);

  result.tags = Object.values(acc);
  result.perfumes = perfumes;

  return result;
}

export async function getUserPreferences(): Promise<UserPreferences> {
  //TODO: filter on server side!
  const perfumes = (await getPerfumes()).filter(
    (x) => x.perfume.ml > 0 && x.perfume.rating >= 8
  );

  const past = new Date(0); //todo refactor
  const lastThreePerfumes = perfumes
    .sort((a, b) => {
      const dateA = a.lastWorn ? new Date(a.lastWorn) : past;
      const dateB = b.lastWorn ? new Date(b.lastWorn) : past;
      console.log(dateA, dateB);
      return dateB.getTime() - dateA.getTime();
    })
    .slice(0, 3);

  return {
    last3perfumes: aggregatePerfumesTags(lastThreePerfumes),
    allTime: aggregatePerfumesTags(perfumes),
  };
}

//TODO: paging
export async function getAlreadyRecommended(): Promise<
  RecommendationDownloadDTO[]
> {
  if (!apiAddress) throw new Error("PerfumeAPI address not set");
  const qry = `${apiAddress}/recommendations/`;
  const response = await fetch(qry);
  if (!response.ok) {
    throw new Error("Failed to fetch previous recommendations");
  }
  const r: RecommendationDownloadDTO[] = await response.json();
  return r;
}

export async function addRecommendation(
  recommendation: RecommendationUploadDTO
) : Promise<RecommendationDownloadDTO> {
  if (!apiAddress) throw new Error("PerfumeAPI address not set");
  const qry = `${apiAddress}/recommendations/`;
  const response = await fetch(qry, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(recommendation),
  });
  if (!response.ok) {
    throw new Error("Failed to add recommendation");
  }
  const r: RecommendationDownloadDTO = await response.json();
  return r;
}



function RemoveDiacritics(input: string) {
  return input.normalize("NFD").replace(/[\u0300-\u036f]/g, "");
}

export default async function getRecommendations(query: string) : Promise<RecommendationDownloadDTO> {
  try {
    query = RemoveDiacritics(query);
    const recommendations = await getOpenAIResponse(query);
    if (recommendations) {
      const dto: RecommendationUploadDTO = {
        query,
        recommendations,
      };
      return await addRecommendation(dto);
    } else {
      return Promise.reject(new Error('Open AI response is empty'));
    }
  } catch (err: unknown) {
    if (err instanceof Error) {
      console.error(err.message);
      return Promise.reject(err);
    } else {
      console.error("Unknown error occured");
      return Promise.reject("Unknown error occured");
    }
  }
}

