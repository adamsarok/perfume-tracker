import { TagDTO } from "@/dto/TagDTO";
import { TagStatDTO } from "@/dto/TagStatDTO";
import { ActionResult } from "@/dto/ActionResult";
import { getPerfumeTrackerApiAddress } from "./conf-service";
import { api } from "./auth-service";

export async function getTagStats(): Promise<TagStatDTO[]> {
  const apiUrl = await getPerfumeTrackerApiAddress();
  const response = await api.get<TagStatDTO[]>(`${apiUrl}/tags/stats`);
  return response.data;
}

export async function getTags(): Promise<TagDTO[]> {
  const apiUrl = await getPerfumeTrackerApiAddress();
  const response = await api.get<TagDTO[]>(`${apiUrl}/tags`);
  return response.data;
}

export async function addTag(tag: TagDTO): Promise<ActionResult> {
  const apiUrl = await getPerfumeTrackerApiAddress();
  const response = await api.post<TagDTO>(`${apiUrl}/tags`, tag);
  return { ok: true, id: response.data.id };
}

export async function updateTag(tag: TagDTO): Promise<ActionResult> {
  const apiUrl = await getPerfumeTrackerApiAddress();
  const response = await api.put<TagDTO>(`${apiUrl}/tags/${tag.id}`, tag);
  return { ok: true, id: response.data.id };
}

export async function deleteTag(id: string): Promise<ActionResult> {
  const apiUrl = await getPerfumeTrackerApiAddress();
  const response = await api.delete<TagDTO>(`${apiUrl}/tags/${id}`);
  return { ok: true, id: response.data.id };
}
