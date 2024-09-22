'use server';

import { Perfume, Tag } from "@prisma/client";
import { revalidatePath } from "next/cache";
import db from ".";
import * as perfumeRepo from "./perfume-repo"
import { ActionResult } from "./action-result";

export interface WornWithPerfume {
    id: number;
    perfumeId: number;
    wornOn: Date;
    perfume: Perfume;
    tags: Tag[];
}

//cursor based pagination - fast as we don't rely on skip/take
export async function getWornBeforeID(cursor: number | null, pageLength: number) : Promise<WornWithPerfume[]> {
    const worns = await db.perfumeWorn.findMany({
        where: cursor !== null ? {
            id: {
                lt: cursor
            }
        } : {},
        include: {
            perfume: {
                include: {
                    tags: {
                        include: {
                            tag: true
                        }
                    }
                }
            }
        },
        orderBy: [
            {
                id: 'desc',
            },
        ],
        take: pageLength
    });
    return toWornWithPerfume(worns);
}

interface PerfumeTagDTO {
    id: number,
    perfumeId: number,
    tagId: number,
    tag: Tag
}

interface WornDTO {
    perfume: Perfume & { tags: PerfumeTagDTO[] };
    id: number,
    perfumeId: number,
    wornOn: Date
}


//TODO: fix any
function toWornWithPerfume(data: WornDTO[]) : WornWithPerfume[] {
    const result: WornWithPerfume[] = [];
    data.map((w) => {
        result.push({
            id: w.id,
            perfumeId: w.perfumeId,
            wornOn: w.wornOn,
            perfume: w.perfume,
            tags: w.perfume.tags.map((t: PerfumeTagDTO) : Tag => {
                return {
                  id: t.id,
                  color: t.tag.color,
                  tag: t.tag.tag
                }
            })
        })
    })
    return result;
}

export async function getWornWithPerfume(wornFrom: Date | null) : Promise<WornWithPerfume[]> {
    const worns = await db.perfumeWorn.findMany({
        where: wornFrom !== null ? {
            wornOn: {
                gte: wornFrom
            }
        } : {},
        include: {
            perfume: {
                include: {
                    tags: {
                        include: {
                            tag: true
                        }
                    }
                }
            }
        },
        orderBy: [
            {
                wornOn: 'desc',
            },
        ]
    });
    return toWornWithPerfume(worns);
}

export async function deleteWear(id: number) {
    if (!id) return;
    await db.perfumeWorn.delete({
        where: {
            id
        },
    });
    revalidatePath('/');
}

//warning todo utc
export async function wearPerfume(id: number, date: Date) : Promise<ActionResult> {
    if (!id) {
        return {
            error: 'Perfume ID empty',
            ok: false
        }
    }
    const idNum: number = parseInt(id.toString());
    const startDay = new Date(date);
    startDay.setHours(0, 0, 0, 0);
    const startDayPlus1 = new Date();
    startDayPlus1.setDate(startDay.getDate() + 1);
    startDayPlus1.setHours(0, 0, 0, 0);
    const alreadyWornToday = await db.perfumeWorn.findFirst({
        where: {
            perfumeId: idNum,
            wornOn: {
                gte: startDay,
                lt: startDayPlus1
            }
        }
    });
    if (alreadyWornToday) {
        return {
            error: 'Already wearing this perfume today!',
            ok: false
        }
    }
    await db.perfumeWorn.create({
        data: {
            wornOn: date,
            perfume: {
                connect: {
                    id: idNum
                }
            }
        },
    });
    //revalidatePath('/'); not here!
    return {
        ok: true
    }
}


export interface PerfumeWornDTO {
    perfume: Perfume,
    wornTimes: number | undefined,
    lastWorn: Date | undefined,
    tags: Tag[]
}

export async function getAllPerfumesWithWearCount(): Promise<PerfumeWornDTO[]> {
    const worn = await db.perfumeWorn.groupBy({
        by: ['perfumeId'],
        _count: {
            id: true
        },
        _max: {
            wornOn: true
        }
    });
    const perfumes = await perfumeRepo.getPerfumesWithTags();
    const m = new Map();
    perfumes.forEach(function (x) {
        const dto: PerfumeWornDTO = {
            perfume: x.perfume,
            wornTimes: undefined,
            lastWorn: undefined,
            tags: x.tags
        }
        m.set(x.perfume.id, dto);
    });
    worn.forEach(function (x) {
        const dto = m.get(x.perfumeId);
        dto.wornTimes = x._count.id;
        dto.lastWorn = x._max.wornOn;
    })
    return Array.from(m.values());
}