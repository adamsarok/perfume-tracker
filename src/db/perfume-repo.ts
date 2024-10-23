'use server';

import db from ".";
import { Perfume, Tag } from "@prisma/client";
import { z } from "zod";
import { ActionResult } from "./action-result";

const perfumeSchema = z.object({
    house: z.string().min(1),
    perfume: z.string().min(1),
    rating: z.string().min(1),
    notes: z.string().min(3),
    ml: z.string().min(1),
    // winter: z.string().nullable(),
    // summer: z.string().nullable(),
    // autumn: z.string().nullable(),
    // spring: z.string().nullable(),
})

interface UpdatePerfumeFormState {
    errors: {
        house?: string[];
        perfume?: string[];
        rating?: string[];
        notes?: string[];
        ml?: string[];
        winter?: string[];
        summer?: string[];
        autumn?: string[];
        spring?: string[];
        _form?: string[];
    },
    result: Perfume | null,
    state: 'init' | 'failed' | 'success'
}

export async function setImageObjectKey(id: number, objectKey: string) : Promise<ActionResult> {
    try {
        await db.perfume.update({
            where: {
                id
            }, 
            data: {
                imageObjectKey: objectKey
            }
        });
        return { ok: true };
    } catch (err: unknown) {
        if (err instanceof Error) {
            return {
                ok: false,
                error: err.message
            };
        } else {
            return { ok: false };
        }
    }
}

export async function upsertPerfume(perfume: Perfume, tags: Tag[])
    : Promise<ActionResult> {
    try {
        const selectedTags = tags.map(x => x.id);
        if (perfume.id) {
            const currentTags = await db.perfumeTag.findMany({
                where: {
                    perfumeId: perfume.id
                }
            });
            const currentTagIds = currentTags.map(x => x.tagId);
            const tagIdsToRemove = currentTags
                .filter(x => !selectedTags.includes(x.tagId))
                .map(m => m.id);
            const tagsToAdd = selectedTags.filter(x => !currentTagIds.includes(x));
            const result = await db.perfume.update({
                where: {
                    id: perfume.id
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
                            data: tagsToAdd.map(x => ({
                                tagId: x
                            }))
                        },
                        deleteMany: {
                            id: {
                                in: tagIdsToRemove
                            }
                        }
                    }
                }
            });
            if (result) return {
                ok: true,
                error: "",
                id: result.id
            }
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
                            data: tags.map(t => ({
                                tagId: t.id
                            }))
                        }
                    }
                }
            });
            if (result) return {
                ok: true,
                error: "",
                id: result.id
            }
        }
    } catch (err: unknown) {
        if (err instanceof Error) {
            
            return {
                error: err.message,
                ok: false
            };
        }
    }
    return {
        error:'Unknown error occured',
        ok: false
    };
}

export async function getPerfumesForSelector(): Promise<Perfume[]> {
    return await db.perfume.findMany({
        where: {
            ml: {
                gt: 0
            }
        },
        orderBy: [
            {
                house: 'asc',
            },
            {
                perfume: 'asc',
            },
        ]
    });
}


export async function getPerfumeWithTags(id: number) {
    return await db.perfume.findFirst({
        where: {
            id: id
        },
        include: {
            tags: {
                include: {
                    tag: true
                }
            }
        }
    });
}

export interface PerfumeWithTagDTO {
    perfume: Perfume,
    tags: Tag[]
}

export async function getPerfumesWithTags() : Promise<PerfumeWithTagDTO[]> {
    const perfumes = await db.perfume.findMany({
        include: {
            tags: {
                include: {
                    tag: true
                }
            }
        }
    });
    const result: PerfumeWithTagDTO[] = [];
    perfumes.map((p) => {
        result.push({
            perfume: p,
            tags: p.tags.map((t) => {
                return {
                    id: t.tag.id,
                    color: t.tag.color,
                    tag: t.tag.tag
                }
            })
        });
    });
    return result;
}

export async function deletePerfume(id: number) : Promise<{ error: string | null }> {
    try {
        await db.perfume.delete({
            where: {
                id
            }
        });
    } catch (err: unknown) {
        if (err instanceof Error) {
            return {
                error: err.message
            };
        } else {
            return {
                error: 'Unknown error occured'
            };
        }
    }
    return {
        error: null
    };
}
