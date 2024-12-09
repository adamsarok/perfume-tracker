"use server";

import { PERFUMETRACKER_API_ADDRESS as apiAddress } from "./conf";
import { TagStatDTO } from "@/dto/TagStatDTO";

export async function getTagStats(): Promise<TagStatDTO[]> {
  if (!apiAddress) throw new Error("PerfumeAPI address not set");
  const response = await fetch(`${apiAddress}/tags/stats`);
  if (!response.ok) {
    //TODO: fix unhandled errors
    throw new Error("Failed to fetch tag stats");
  }
  const tagStats: TagStatDTO[] = await response.json();
  return tagStats;
}

// export async function addPerfume(perfume: PerfumeUploadDTO) : Promise<ActionResult> {
//     if (!apiAddress) throw new Error("PerfumeAPI address not set");
//     const response = await fetch(`${apiAddress}/perfumes`, {
//         method: "POST",
//         headers: {
//           "Content-Type": "application/json",
//         },
//         body: JSON.stringify(perfume),
//       });
  
//       if (!response.ok) return { ok: false, error: `Error creating perfume: ${response.status}` };

//       const result: PerfumeUploadDTO = await response.json();
//       return { ok: true, id: result.id };
//   }
  
//   export async function updatePerfume(perfume: PerfumeUploadDTO) : Promise<ActionResult> {
//     if (!apiAddress) throw new Error("PerfumeAPI address not set");
//     const response = await fetch(`${apiAddress}/perfumes/${perfume.id}`, {
//         method: "PUT",
//         headers: {
//           "Content-Type": "application/json",
//         },
//         body: JSON.stringify(perfume),
//       });
  
//       if (!response.ok) return { ok: false, error: `Error updating perfume: ${response.status}` };
  
//       const result = await response.json();
//       console.log(result);
//       return { ok: true, id: perfume.id };
//   }
  
//   export async function deletePerfume(id: number) : Promise<ActionResult> {
//     if (!apiAddress) throw new Error("PerfumeAPI address not set");
//     const response = await fetch(`${apiAddress}/perfumes/${id}`, {
//         method: "DELETE",
//       });
  
//       if (!response.ok) return { ok: false, error: `Error deleting perfume: ${response.status}` };
  
//       const result = await response.json();
//       console.log(result);
//       return { ok: true, id: id };
//   }
  