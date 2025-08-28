import { AxiosResult, get } from "./axios-service";

export async function getPerfumeRandom() : Promise<AxiosResult<string>> {
    return get<string>('/random-perfumes');
}