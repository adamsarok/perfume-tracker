'use server';

import { db } from "@/db";
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

export async function GetTotalMls(): Promise<number> {
    const res = await db.perfume.aggregate({
        _sum: {
            ml: true
        }
    });
    return res._sum.ml ?? 0;
}

interface InsertTagFormState {
    result: {
        tag: string,
        color: string
    } | null
    errors: {
        name?: string[];
        color?: string[];
        _form?: string[];
    }
}

const tagSchema = z.object({
    tag: z.string().min(1),
    color: z.string().min(1) //TODO color validate
})


export async function InsertTag(formState: InsertTagFormState, formData: FormData) : Promise<InsertTagFormState> {
    try {
        console.log(formData);
        const perf = tagSchema.safeParse({
            tag: formData.get('tag'),
            color: formData.get('color')
        });
        if (!perf.success) {
            console.log(perf.error.flatten().fieldErrors);
            return {
                result: null,
                errors: perf.error.flatten().fieldErrors,
            }
        }
        const result = await db.tag.create({
            data: {
                tag: perf.data.tag,
                color: perf.data.color,
            }
        });
        return {
            result: {
                tag: perf.data.tag,
                color: perf.data.color
            },
            errors: { _form: [] }
        };
    } catch (err: unknown) {
        if (err instanceof Error) {
            return {
                result: null,
                errors: { _form: [err.message] }
            };
        } else {
            return {
                result: null,
                errors: { _form: ['Unknown error occured'] }
            };
        }
    }
}

export async function UsertPerfume(id: number | null, isNsfw: boolean, tags: Tag[], formState: UpdatePerfumeFormState, formData: FormData)
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
    revalidatePath('/');
    redirect('/');
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

export async function GetPerfumesForSuggestion(): Promise<PerfumeWornDTO[]> {
    const worn = await GetAllPerfumesWithWearCount();
    const sugg = worn.filter(x => x.perfume.rating >= 8 && x.perfume.ml > 0)
        .sort((a, b) => (a.wornTimes ?? 0) - (b.wornTimes ?? 0))
        .slice(0, 3)
    return sugg;
}

export async function GetWorn() {
    return await db.perfumeWorn.findMany({
        include: {
            perfume: true
        },
        orderBy: [
            {
                wornOn: 'desc',
            },
        ]
    });
}

export async function compareDates(a: PerfumeWorn, b: PerfumeWorn) {
    if (a.wornOn < b.wornOn) {
        return -1;
    }
    if (a.wornOn > b.wornOn) {
        return 1;
    }
    return 0;
}


export async function DeleteWear(id: number) {
    if (!id) return;
    const idNum: number = parseInt(id.toString());
    await db.perfumeWorn.delete({
        where: {
            id
        },
    });
    revalidatePath('/');
}

//warning todo utc
export async function WearPerfume(id: number) {
    if (!id) return;
    const idNum: number = parseInt(id.toString());
    const today = new Date();
    today.setHours(0, 0, 0);
    const tomorrow = new Date();
    tomorrow.setDate(today.getDate() + 1);
    const alreadyWornToday = await db.perfumeWorn.findFirst({
        where: {
            perfumeId: idNum,
            wornOn: {
                gte: today,
                lt: tomorrow
            }
        }
    });
    if (alreadyWornToday) {
        console.log("Already wearing this perfume today");
        return;
    }
    await db.perfumeWorn.create({
        data: {
            wornOn: new Date(),
            perfume: {
                connect: {
                    id: idNum
                }
            }
        },
    });
    revalidatePath('/');
}

export interface PerfumeWornDTO {
    perfume: Perfume,
    wornTimes: number | undefined,
    lastWorn: Date | undefined,
}

export async function GetAllPerfumesWithWearCount(): Promise<PerfumeWornDTO[]> {
    const worn = await db.perfumeWorn.groupBy({
        by: ['perfumeId'],
        _count: {
            id: true
        },
        _max: {
            wornOn: true
        }
    });
    const perfumes = await db.perfume.findMany();
    var m = new Map();
    perfumes.forEach(function (x) {
        let dto: PerfumeWornDTO = {
            perfume: x,
            wornTimes: undefined,
            lastWorn: undefined
        }
        m.set(x.id, dto);
    });
    worn.forEach(function (x) {
        let dto = m.get(x.perfumeId);
        dto.wornTimes = x._count.id;
        dto.lastWorn = x._max.wornOn;
    })
    return Array.from(m.values());
}