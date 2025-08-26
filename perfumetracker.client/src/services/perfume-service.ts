import { PerfumeUploadDTO } from "@/dto/PerfumeUploadDTO";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import { ImageGuidDTO } from "@/dto/ImageGuidDTO";
import { AxiosResult, del, get, post, put } from "./axios-service";
import { PerfumeDTO } from "@/dto/PerfumeDTO";

export async function getPerfumesFulltext(
  fulltext: string
): Promise<AxiosResult<PerfumeWithWornStatsDTO[]>> {
  const qry = '/perfumes/' + (fulltext ? `fulltext/${encodeURIComponent(fulltext)}` : "");
  return get<PerfumeWithWornStatsDTO[]>(qry);
}

export async function getPerfume(id: string): Promise<AxiosResult<PerfumeWithWornStatsDTO>> {
  const qry = `/perfumes/${encodeURIComponent(id)}`;
  return get<PerfumeWithWornStatsDTO>(qry);
}

export async function getNextPerfumeId(id: string): Promise<AxiosResult<string>> {
  const qry = `/perfumes/${encodeURIComponent(id)}/next`;
  return get<string>(qry);
}

export async function getPreviousPerfumeId(id: string): Promise<AxiosResult<string>> {
  const qry = `/perfumes/${encodeURIComponent(id)}/previous`;
  return get<string>(qry);
}

export async function getPerfumes(): Promise<AxiosResult<PerfumeWithWornStatsDTO[]>> {
  const qry = `/perfumes/`;
  return get<PerfumeWithWornStatsDTO[]>(qry);
}

export async function addPerfume(
  perfume: PerfumeUploadDTO
): Promise<AxiosResult<PerfumeDTO>> {
  return post<PerfumeDTO>(`/perfumes`, perfume);
}

export async function updatePerfume(
  perfume: PerfumeUploadDTO
): Promise<AxiosResult<PerfumeDTO>> {
  return put<PerfumeDTO>(`/perfumes/${perfume.id}`, perfume);
}

export async function updateImageGuid(
  perfumeId: string,
  imageGuid: string
): Promise<AxiosResult<unknown>> {
  const dto: ImageGuidDTO = {
    parentObjectId: perfumeId,
    imageObjectKey: imageGuid,
  };
  return put(`/perfumes/imageguid`, dto);
}

export async function deletePerfume(id: string): Promise<AxiosResult<void>> {
  return del(`/perfumes/${encodeURIComponent(id)}`);
}
