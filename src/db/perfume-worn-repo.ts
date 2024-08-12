'use server';

import { Perfume, PerfumeWorn, Tag } from "@prisma/client";
import { revalidatePath } from "next/cache";
import db from ".";
import * as perfumeRepo from "./perfume-repo"

export interface WornWithPerfume {
    id: number;
    perfumeId: number;
    wornOn: Date;
    perfume: Perfume;
    tags: Tag[];
}


export async function getWorn() : Promise<WornWithPerfume[]> {
    const worns = await db.perfumeWorn.findMany({
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
    let result: WornWithPerfume[] = [];
    worns.map((w) => {
        result.push({
            id: w.id,
            perfumeId: w.perfumeId,
            wornOn: w.wornOn,
            perfume: w.perfume,
            tags: w.perfume.tags.map((t) : Tag => {
                return {
                  id: t.tag.id,
                  color: t.tag.color,
                  tag: t.tag.tag
                }
            })
        })
    })
    return result;
}

export async function deleteWear(id: number) {
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
export async function wearPerfume(id: number) {
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
        console.log("Already wearing this perfume today"); //TODO
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
    let m = new Map();
    perfumes.forEach(function (x) {
        let dto: PerfumeWornDTO = {
            perfume: x,
            wornTimes: undefined,
            lastWorn: undefined,
            tags: x.tags.map(x => x.tag)
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