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

export async function UpsertPerfume(id: number | null, isNsfw: boolean, tags: Tag[], formState: UpdatePerfumeFormState, formData: FormData)
    : Promise<UpdatePerfumeFormState> {
    try {
        const selectedTags = tags.map(x => x.tag);
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
            console.log('Not a valid rating');
            return {
                errors: { rating: ['Not a valid rating'] }
            }
        }
        if (id) {
            const currentTags = await db.perfumeTag.findMany({
                where: {
                    perfumeId: id
                }
            });
            const currentTagNames = currentTags.map(x => x.tagName);
            const tagIdsToRemove = currentTags
                .filter(x => !selectedTags.includes(x.tagName))
                .map(m => m.id);
            const tagsToAdd = selectedTags.filter(x => !currentTagNames.includes(x));
            console.log("selectedTags:" + selectedTags)
            console.log("tagsToAdd:" + tagsToAdd);
            console.log("tagIdsToRemove:" + tagIdsToRemove);
            //TODO: tag remove does not work
            const result = await db.perfume.update({
                where: {
                    id: id
                },
                data: {
                    house: perf.data.house,
                    perfume: perf.data.perfume,
                    rating: parseFloat(perf.data.rating),
                    notes: perf.data.notes,
                    nsfw: isNsfw,
                    ml: parseInt(perf.data.ml),
                    tags: {
                        createMany: {
                            data: tagsToAdd.map(x => ({
                                tagName: x
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
                    nsfw: isNsfw,
                    ml: parseInt(perf.data.ml),
                    tags: {
                        createMany: {
                            data: tags.map(t => ({
                                tagName: t.tag
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

export async function GetPerfumesForSelector(): Promise<Perfume[]> {
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


export async function GetPerfumeWithTags(id: number) {
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

export async function GetPerfumesWithTags() {
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