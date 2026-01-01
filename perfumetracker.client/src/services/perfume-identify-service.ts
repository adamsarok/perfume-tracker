import { AxiosResult, get } from "./axios-service";

export interface IdentifiedPerfumeDTO {
    house: string;
    perfumeName: string;
    family: string;
    notes: string[];
    confidenceScore: number;
}

export async function getIdentifiedPerfume(house: string, perfumeName: string): Promise<AxiosResult<IdentifiedPerfumeDTO>> {
  const qry = `/perfumes/identify/${encodeURIComponent(house)}/${encodeURIComponent(perfumeName)}`;
  return get<IdentifiedPerfumeDTO>(qry);
}