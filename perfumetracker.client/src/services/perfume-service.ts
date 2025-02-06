"use server";

import { PERFUMETRACKER_API_ADDRESS as apiAddress } from "./conf";
import { PerfumeUploadDTO } from "@/dto/PerfumeUploadDTO";
import { PerfumeStatsDTO } from "@/dto/PerfumeStatsDTO";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import { ImageGuidDTO } from "@/dto/ImageGuidDTO";
import { ActionResultID } from "@/dto/ActionResultID";

export async function getPerfumesFulltext(
  fulltext: string
): Promise<PerfumeWithWornStatsDTO[]> {
  if (!apiAddress) throw new Error("PerfumeAPI address not set");
  const qry = `${apiAddress}/perfumes/${
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
  if (!apiAddress) throw new Error("PerfumeAPI address not set");
  const qry = `${apiAddress}/perfumes/${encodeURIComponent(id)}`;
  const response = await fetch(qry);
  if (!response.ok) {
    throw new Error("Failed to fetch perfume");
  }
  const perfume: PerfumeWithWornStatsDTO = await response.json();
  return perfume;
}

export async function getPerfumes(): Promise<PerfumeWithWornStatsDTO[]> {
  if (!apiAddress) throw new Error("PerfumeAPI address not set");
  const qry = `${apiAddress}/perfumes/`;
  const response = await fetch(qry);
  if (!response.ok) {
    throw new Error("Failed to fetch perfumes");
  }
  const perfumes: PerfumeWithWornStatsDTO[] = await response.json();
  return perfumes;
}

export async function getPerfumeStats(): Promise<PerfumeStatsDTO> {
  if (!apiAddress) throw new Error("PerfumeAPI address not set");
  const response = await fetch(`${apiAddress}/perfumes/stats`);
  if (!response.ok) {
    throw new Error("Failed to fetch perfume stats");
  }
  const stats: PerfumeStatsDTO = await response.json();
  return stats;
}

export async function addPerfume(
  perfume: PerfumeUploadDTO
): Promise<ActionResultID> {
  if (!apiAddress) throw new Error("PerfumeAPI address not set");
  const response = await fetch(`${apiAddress}/perfumes`, {
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
): Promise<ActionResultID> {
  if (!apiAddress) throw new Error("PerfumeAPI address not set");
  const response = await fetch(`${apiAddress}/perfumes/${perfume.id}`, {
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
): Promise<ActionResultID> {
  if (!apiAddress) throw new Error("PerfumeAPI address not set");
  const dto: ImageGuidDTO = { parentObjectId: perfumeId, imageObjectKey: imageGuid };
  const response = await fetch(`${apiAddress}/perfumes/imageguid`, {
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

export async function deletePerfume(id: number): Promise<ActionResultID> {
  if (!apiAddress) throw new Error("PerfumeAPI address not set");
  const response = await fetch(`${apiAddress}/perfumes/${id}`, {
    method: "DELETE"
  });

  if (!response.ok)
    return { ok: false, error: `Error deleting perfume: ${response.status}` };

  const result = await response.json();
  console.log(result);
  return { ok: true, id: id };
}
