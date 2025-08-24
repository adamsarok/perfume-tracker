import { TagDTO } from "@/dto/TagDTO";
import { TagStatDTO } from "@/dto/TagStatDTO";
import { AxiosResult, del, get, post, put } from "./axios-service";

export async function getTagStats(): Promise<AxiosResult<TagStatDTO[]>> {
  return get<TagStatDTO[]>('/tags/stats');
}

export async function getTags(): Promise<AxiosResult<TagDTO[]>> {
  return get<TagDTO[]>(`/tags`);
}

export async function addTag(tag: TagDTO): Promise<AxiosResult<TagDTO>> {
  return post<TagDTO>('/tags', tag);
}

export async function updateTag(tag: TagDTO): Promise<AxiosResult<TagDTO>> {
  return put<TagDTO>(`/tags/${tag.id}`, tag);
}

export async function deleteTag(id: string): Promise<AxiosResult<TagDTO>> {
  return del<TagDTO>(`/tags/${id}`);
}
