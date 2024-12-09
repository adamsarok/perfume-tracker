"use server";

import db from ".";
import { Perfume, Tag } from "@prisma/client";
import { ActionResult } from "./action-result";
import { getPerfumes } from "@/services/perfume-service";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";

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

export async function upsertPerfume(
  perfume: Perfume,
  tags: Tag[]
): Promise<ActionResult> {
  try {
    const selectedTags = tags.map((x) => x.id);
    if (perfume.id) {
      const currentTags = await db.perfumeTag.findMany({
        where: {
          perfumeId: perfume.id,
        },
      });
      const currentTagIds = currentTags.map((x) => x.tagId);
      const tagIdsToRemove = currentTags
        .filter((x) => !selectedTags.includes(x.tagId))
        .map((m) => m.id);
      const tagsToAdd = selectedTags.filter((x) => !currentTagIds.includes(x));
      const result = await db.perfume.update({
        where: {
          id: perfume.id,
        },
        data: {
          house: perfume.house,
          perfume: perfume.perfume,
          rating: perfume.rating,
          notes: perfume.notes,
          ml: perfume.ml,
          summer: perfume.summer,
          winter: perfume.winter,
          spring: perfume.spring,
          autumn: perfume.autumn,
          tags: {
            createMany: {
              data: tagsToAdd.map((x) => ({
                tagId: x,
              })),
            },
            deleteMany: {
              id: {
                in: tagIdsToRemove,
              },
            },
          },
        },
      });
      if (result)
        return {
          ok: true,
          error: "",
          id: result.id,
        };
    } else {
      const result = await db.perfume.create({
        data: {
          house: perfume.house,
          perfume: perfume.perfume,
          rating: perfume.rating,
          notes: perfume.notes,
          ml: perfume.ml,
          summer: perfume.summer,
          winter: perfume.winter,
          spring: perfume.spring,
          autumn: perfume.autumn,
          tags: {
            createMany: {
              data: tags.map((t) => ({
                tagId: t.id,
              })),
            },
          },
        },
      });
      if (result)
        return {
          ok: true,
          error: "",
          id: result.id,
        };
    }
  } catch (err: unknown) {
    if (err instanceof Error) {
      return {
        error: err.message,
        ok: false,
      };
    }
  }
  return {
    error: "Unknown error occured",
    ok: false,
  };
}

export async function getPerfumesForSelector(): Promise<PerfumeWithWornStatsDTO[]> {
  const perf = await getPerfumes();
  return perf.filter((x) => x.perfume.ml > 0); //todo filter on server side
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
