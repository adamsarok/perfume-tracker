'use server';

import db from ".";
import { Perfume, PerfumeTag, PerfumeWorn, Tag } from "@prisma/client";
import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { z } from "zod";

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

export async function upsertPerfume(id: number | null, tags: Tag[], formState: UpdatePerfumeFormState, formData: FormData)
    : Promise<UpdatePerfumeFormState> {
    try {
        const selectedTags = tags.map(x => x.id);
        const perf = perfumeSchema.safeParse({
            house: formData.get('house'),
            perfume: formData.get('perfume'),
            rating: formData.get('rating'),
            notes: formData.get('notes'),
            ml: formData.get('ml'),
        });
        if (!perf.success) {
            return {
                errors: perf.error.flatten().fieldErrors,
                result: null,
                state: 'failed'
            }
        }
        if (!parseFloat(perf.data.rating)) {
            return {
                errors: { rating: ['Not a valid rating'] },
                result: null,
                state: 'failed'
            }
        }
        const summmer = formData.get('summer') ? true : false;
        const winter = formData.get('winter') ? true : false;
        const spring = formData.get('spring') ? true : false;
        const autumn = formData.get('autumn') ? true : false;
        if (id) {
            const currentTags = await db.perfumeTag.findMany({
                where: {
                    perfumeId: id
                }
            });
            const currentTagIds = currentTags.map(x => x.tagId);
            const tagIdsToRemove = currentTags
                .filter(x => !selectedTags.includes(x.tagId))
                .map(m => m.id);
            const tagsToAdd = selectedTags.filter(x => !currentTagIds.includes(x));
            const result = await db.perfume.update({
                where: {
                    id: id
                },
                data: {
                    house: perf.data.house,
                    perfume: perf.data.perfume,
                    rating: parseFloat(perf.data.rating),
                    notes: perf.data.notes,
                    ml: parseInt(perf.data.ml),
                    summer: summmer,
                    winter: winter,
                    spring: spring,
                    autumn: autumn,
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
            if (result) return  {
                errors: {},
                result,
                state: 'success'
            }
        } else {
            const result = await db.perfume.create({
                data: {
                    house: perf.data.house,
                    perfume: perf.data.perfume,
                    rating: parseFloat(perf.data.rating),
                    notes: perf.data.notes,
                    ml: parseInt(perf.data.ml),
                    summer: summmer,
                    winter: winter,
                    spring: spring,
                    autumn: autumn,
                    tags: {
                        createMany: {
                            data: tags.map(t => ({
                                tagId: t.id
                            }))
                        }
                    }
                }
            });
            if (result) return  {
                errors: {},
                result,
                state: 'success'
            }
        }
    } catch (err: unknown) {
        if (err instanceof Error) {
            return {
                errors: { _form: [err.message] },
                result: null,
                state: 'failed'
            };
        }
    }
    return {
        errors: { _form: ['Unknown error occured'] },
        result: null,
        state: 'failed'
    };
}

export async function getPerfumesForSelector(): Promise<Perfume[]> {
    return await db.perfume.findMany({
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
