'use server';

import { db } from "@/db";
import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";

export async function AddPerfume(formState: {}, formData: FormData) {
    try {
        const house = formData.get('house');
        const perfume = formData.get('perfume');
        const rating = formData.get('rating');
        const notes = formData.get('notes');

        //todo this is bad, zod validate!!!
        if (typeof house !== 'string' || house.length < 1) {
            return {
                message: 'House must be longer'
            };
        }
        if (typeof perfume !== 'string' || perfume.length < 1) {
            return {
                message: 'Perfume must be longer'
            };
        }
        if (typeof rating !== 'string') {
            return {
                message: 'Rating is empty?'
            };
        }
        if (typeof notes !== 'string' || notes.length < 1) {
            return {
                message: 'Notes must be longer'
            };
        }
        var exists = await db.perfume.findFirst({
            where: {
                house: house,
                perfume: perfume
            }
        });
        if (exists) return {
            message: 'Perfume already added'
        };
        await db.perfume.create({
            data: {
                house: house,
                perfume: perfume,
                rating: parseFloat(rating),
                notes: notes
            }
        });
    } catch (err: unknown) {
        if (err instanceof Error) {
            return {
                message: err.message
            };
        } else {
            return {
                message: "Unknown error type"
            };
        }
    }
    revalidatePath('/');
    redirect('/');
}

//warning todo utc
export async function WearPerfume(id: number) {
    console.log("Perfume add fire with ID: " + id); //replace with msg
    if (!id) return;
    const idNum: number = parseInt(id.toString()); //I dont get why I have to parse a number to a number...
    const today = new Date();
    today.setHours(0,0,0);
    const tomorrow = new Date();
    tomorrow.setDate(today.getDate() + 1); //maybe????
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
    perfumeId: number,
    house: string,
    perfume: string,
    rating: number,
    wornTimes: number | undefined,
    lastWorn: Date | undefined,
}

export async function GetWornPerfumes(): Promise<PerfumeWornDTO[]> {
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
    perfumes.forEach(function(x) {
        let dto: PerfumeWornDTO = { 
            perfumeId: x.id,
            house: x.house,
            perfume: x.perfume,
            rating: x.rating,
            wornTimes: undefined,
            lastWorn: undefined
        }
        m.set(x.id, dto);
    });
    worn.forEach(function(x) {
        let dto = m.get(x.perfumeId);
        dto.wornTimes = x._count.id;
        dto.lastWorn = x._max.wornOn;
    })
    return Array.from(m.values());
  }