"use server";

import { PERFUMETRACKER_API_ADDRESS, R2_API_ADDRESS } from "./conf";
import { PerfumeUploadDTO } from "@/dto/PerfumeUploadDTO";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import { ImageGuidDTO } from "@/dto/ImageGuidDTO";
import { ActionResult } from "@/dto/ActionResult";
import { getImageUrl } from "@/components/r2-image";

export async function getPerfumesFulltext(
  fulltext: string
): Promise<PerfumeWithWornStatsDTO[]> {
  if (!PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const qry = `${PERFUMETRACKER_API_ADDRESS}/perfumes/${
    fulltext ? `fulltext/${fulltext}` : ""
  }`;
  const response = await fetch(qry);
  if (!response.ok) {
    throw new Error("Failed to fetch perfumes");
  }
  const perfumes: PerfumeWithWornStatsDTO[] = await response.json();
  return perfumes;
}

export async function getPerfume(id: number): Promise<PerfumeWithWornStatsDTO> {
  if (!PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const qry = `${PERFUMETRACKER_API_ADDRESS}/perfumes/${encodeURIComponent(id)}`;
  const response = await fetch(qry);
  if (!response.ok) {
    throw new Error("Failed to fetch perfume");
  }
  const perfume: PerfumeWithWornStatsDTO = await response.json();
  perfume.perfume.imagerUrl = getImageUrl(perfume.perfume.imageObjectKey, R2_API_ADDRESS);
  return perfume;
}

export async function getPerfumes(): Promise<PerfumeWithWornStatsDTO[]> {
  if (!PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const qry = `${PERFUMETRACKER_API_ADDRESS}/perfumes/`;
  const response = await fetch(qry);
  if (!response.ok) {
    throw new Error("Failed to fetch perfumes");
  }
  const perfumes: PerfumeWithWornStatsDTO[] = await response.json();
  perfumes.forEach(x => x.perfume.imagerUrl = getImageUrl(x.perfume.imageObjectKey, R2_API_ADDRESS));
  return perfumes;
}

export async function addPerfume(
  perfume: PerfumeUploadDTO
): Promise<ActionResult> {
  if (!PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const response = await fetch(`${PERFUMETRACKER_API_ADDRESS}/perfumes`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(perfume),
  });

  if (!response.ok)
    return { ok: false, error: `Error creating perfume: ${response.status}` };

  const result: PerfumeUploadDTO = await response.json();
  return { ok: true, id: result.id };
}

export async function updatePerfume(
  perfume: PerfumeUploadDTO
): Promise<ActionResult> {
  if (!PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const response = await fetch(`${PERFUMETRACKER_API_ADDRESS}/perfumes/${perfume.id}`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(perfume),
  });

  if (!response.ok)
    return { ok: false, error: `Error updating perfume: ${response.status}` };

  const result = await response.json();
  console.log(result);
  return { ok: true, id: perfume.id };
}

export async function updateImageGuid(
  perfumeId: number, imageGuid: string
): Promise<ActionResult> {
  if (!PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const dto: ImageGuidDTO = { parentObjectId: perfumeId, imageObjectKey: imageGuid };
  const response = await fetch(`${PERFUMETRACKER_API_ADDRESS}/perfumes/imageguid`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(dto),
  });

  if (!response.ok)
    return { ok: false, error: `Error updating perfume: ${response.status}` };

  const result = await response.json();
  console.log(result);
  return { ok: true, id: perfumeId };
}

export async function deletePerfume(id: number): Promise<ActionResult> {
  if (!PERFUMETRACKER_API_ADDRESS) throw new Error("PerfumeAPI address not set");
  const response = await fetch(`${PERFUMETRACKER_API_ADDRESS}/perfumes/${id}`, {
    method: "DELETE"
  });

  if (!response.ok)
    return { ok: false, error: `Error deleting perfume: ${response.status}` };

  const result = await response.json();
  console.log(result);
  return { ok: true, id: id };
}
