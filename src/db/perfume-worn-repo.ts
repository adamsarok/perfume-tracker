'use server';

import { Perfume, PerfumeWorn, Tag } from "@prisma/client";
import { revalidatePath } from "next/cache";
import db from ".";
import * as perfumeRepo from "./perfume-repo"

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
    tags: Tag[]
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
    const perfumes = await perfumeRepo.GetPerfumesWithTags();
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