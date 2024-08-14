'use server';

import db from ".";
import { Perfume, PerfumeWorn, Tag } from "@prisma/client";
import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { z } from "zod";

const perfumeSchema = z.object({
    house: z.string().min(1),
    perfume: z.string().min(1),
    rating: z.string().min(1),
    notes: z.string().min(3),
    ml: z.string().min(1),
})

interface UpdatePerfumeFormState {
    errors: {
        house?: string[];
        perfume?: string[];
        rating?: string[];
        notes?: string[];
        ml?: string[];
        _form?: string[];
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
            }
        }
        if (!parseFloat(perf.data.rating)) {
            return {
                errors: { rating: ['Not a valid rating'] }
            }
        }
        if (id) {
            const currentTags = await db.perfumeTag.findMany({
                // include: {
                //     tag: true
                // },
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
        } else {
            const result = await db.perfume.create({
                data: {
                    house: perf.data.house,
                    perfume: perf.data.perfume,
                    rating: parseFloat(perf.data.rating),
                    notes: perf.data.notes,
                    ml: parseInt(perf.data.ml),
                    tags: {
                        createMany: {
                            data: tags.map(t => ({
                                tagId: t.id
                            }))
                        }
                    }
                }
            });
        }
    } catch (err: unknown) {
        if (err instanceof Error) {
            return {
                errors: { _form: [err.message] }
            };
        } else {
            return {
                errors: { _form: ['Unknown error occured'] }
            };
        }
    }
    //TODO: success msg or reload?
    return {
        errors: {  }
    };
    //revalidatePath('/');
    //redirect('/');
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

export async function getPerfumesWithTags() {
    return await db.perfume.findMany({
        include: {
            tags: {
                include: {
                    tag: true
                }
            }
        }
    });
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
