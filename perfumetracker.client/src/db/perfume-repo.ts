"use server";

import db from ".";
import { Perfume, Tag } from "@prisma/client";
import { ActionResult } from "./action-result";

export async function setImageObjectKey(
  id: number,
  objectKey: string
): Promise<ActionResult> {
  try {
    await db.perfume.update({
      where: {
        id,
      },
      data: {
        imageObjectKey: objectKey,
      },
    });
    return { ok: true };
  } catch (err: unknown) {
    if (err instanceof Error) {
      return {
        ok: false,
        error: err.message,
      };
    } else {
      return { ok: false };
    }
  }
}


// export async function getSurpriseId(pastDaysSkipped: number) {
//   //TODO filter in API
//   const p = await getPerfumes();
//   const ids = p.filter ({
//     where: {
//       ml: {
//         gt: 0,
//       },
//       rating: {
//         gte: 8,
//       },
//     },
//     select: {
//       id: true,
//     },
//   });
//   const worn = await getWornPerfumeIDs(pastDaysSkipped);
//   const suggested = await getAlreadySuggestedPerfumeIds(pastDaysSkipped);
//   const filtered = ids.filter(
//     (x) => !worn.flatMap((m) => m).includes(x.id) && !suggested.includes(x.id)
//   );
//   const id = filtered[Math.floor(Math.random() * filtered.length)];
//   await addSuggested(id.id);
//   return id.id;
// }


export async function getPerfumeWithTags(id: number) {
  return await db.perfume.findFirst({
    where: {
      id: id,
    },
    include: {
      tags: {
        include: {
          tag: true,
        },
      },
    },
  });
}

export interface PerfumeWithTagDTO {
  perfume: Perfume;
  tags: Tag[];
}

export async function getPerfumesWithTags(): Promise<PerfumeWithTagDTO[]> {
  const perfumes = await db.perfume.findMany({
    include: {
      tags: {
        include: {
          tag: true,
        },
      },
    },
  });
  const result: PerfumeWithTagDTO[] = [];
  perfumes.map((p) => {
    result.push({
      perfume: p,
      tags: p.tags.map((t) => {
        return {
          id: t.tag.id,
          color: t.tag.color,
          tag: t.tag.tag,
        };
      }),
    });
  });
  return result;
}

export async function deletePerfume(
  id: number
): Promise<{ error: string | null }> {
  try {
    await db.perfume.delete({
      where: {
        id,
      },
    });
  } catch (err: unknown) {
    if (err instanceof Error) {
      return {
        error: err.message,
      };
    } else {
      return {
        error: "Unknown error occured",
      };
    }
  }
  return {
    error: null,
  };
}
