import { GetRandomPerfumeResponse } from "@/dto/GetRandomPerfumeResponse";
import { AxiosResult, get } from "./axios-service";

export async function getPerfumeRandom() : Promise<AxiosResult<GetRandomPerfumeResponse>> {
    return get<GetRandomPerfumeResponse>('/random-perfumes');
}