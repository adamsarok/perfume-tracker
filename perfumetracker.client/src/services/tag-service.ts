import { TagDTO } from "@/dto/TagDTO";
import { TagStatDTO } from "@/dto/TagStatDTO";
import { ActionResult } from "@/dto/ActionResult";
import { del, get, post, put } from "./axios-service";

export async function getTagStats(): Promise<TagStatDTO[]> {
  const response = await get<TagStatDTO[]>('/tags/stats');
  return response.data;
}

export async function getTags(): Promise<TagDTO[]> {
  const response = await get<TagDTO[]>(`/tags`);
  return response.data;
}

export async function addTag(tag: TagDTO): Promise<ActionResult> {
  const response = await post<TagDTO>('/tags', tag);
  return { ok: true, id: response.data.id };
}

export async function updateTag(tag: TagDTO): Promise<ActionResult> {
  const response = await put<TagDTO>(`/tags/${tag.id}`, tag);
  return { ok: true, id: response.data.id };
}

export async function deleteTag(id: string): Promise<ActionResult> {
  const response = await del<TagDTO>(`/tags/${id}`);
  return { ok: true, id: response.data.id };
}
