import { AxiosResult, get, post } from "./axios-service";
import { GlobalPerfumeDTO } from "@/dto/GlobalPerfumeDTO";
import { PerfumeDTO } from "@/dto/PerfumeDTO";

export async function searchGlobalPerfumes(
  searchText: string
): Promise<AxiosResult<GlobalPerfumeDTO[]>> {
  if (!searchText || searchText.trim().length < 2) {
    return { data: [], error: undefined, ok: true };
  }
  const qry = `/global-perfumes/search/${encodeURIComponent(searchText)}`;
  return get<GlobalPerfumeDTO[]>(qry);
}

export interface AddPerfumeFromGlobalRequest {
  ml: number;
  mlLeft: number;
}

export async function addPerfumeFromGlobal(
  globalPerfumeId: string,
  ml: number,
  mlLeft: number
): Promise<AxiosResult<PerfumeDTO>> {
  const request: AddPerfumeFromGlobalRequest = { ml, mlLeft };
  return post<PerfumeDTO>(
    `/perfumes/from-global/${encodeURIComponent(globalPerfumeId)}`,
    request
  );
}