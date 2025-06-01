"use server";

import { TagDTO } from "@/dto/TagDTO";
import { env } from "process";
import { TagStatDTO } from "@/dto/TagStatDTO";
import { ActionResult } from "@/dto/ActionResult";

export async function getTagStats(): Promise<TagStatDTO[]> {
  if (!env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const response = await fetch(`${env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS}/tags/stats`);
  if (!response.ok) {
    //TODO: fix unhandled errors
    throw new Error("Failed to fetch tag stats");
  }
  const tagStats: TagStatDTO[] = await response.json();
  return tagStats;
}

export async function getTags(): Promise<TagDTO[]> {
  if (!env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const response = await fetch(`${env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS}/tags`);
  if (!response.ok) {
    //TODO: fix unhandled errors
    throw new Error("Failed to fetch tag stats");
  }
  const tagStats: TagDTO[] = await response.json();
  return tagStats;
}

export async function addTag(tag: TagDTO) : Promise<ActionResult> {
    if (!env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
    const response = await fetch(`${env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS}/tags`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(tag),
      });
  
      if (!response.ok) return { ok: false, error: `Error creating tag: ${response.status}` };

      const result: TagDTO = await response.json();
      return { ok: true, id: result.id };
  }
  
  export async function updateTag(tag: TagDTO) : Promise<ActionResult> {
    if (!env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
    const response = await fetch(`${env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS}/tags/${tag.id}`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(tag),
      });
  
      if (!response.ok) return { ok: false, error: `Error updating tag: ${response.status}` };
  
      const result = await response.json();
      console.log(result);
      return { ok: true, id: tag.id };
  }
  
  export async function deleteTag(id: string) : Promise<ActionResult> {
    if (!env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
    const response = await fetch(`${env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS}/tags/${id}`, {
        method: "DELETE",
      });
  
      if (!response.ok) return { ok: false, error: `Error deleting tag: ${response.status}` };
  
      const result = await response.json();
      console.log(result);
      return { ok: true, id: id };
  }
  