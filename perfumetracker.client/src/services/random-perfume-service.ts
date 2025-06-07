import { get } from "./axios-service";

export async function getPerfumeRandom() : Promise<string> {
    const response = await get<string>('/random-perfumes');
    return response.data;
}