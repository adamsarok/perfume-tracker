'use server';

import { db } from "@/db";
import { PerfumeWorn } from "@prisma/client";
import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { z } from "zod";

const perfumeSchema = z.object({
    house: z.string().min(1),
    perfume: z.string().min(1),
    rating: z.string().min(1),
    notes: z.string().min(3)
})

interface UpdatePerfumeFormState {
    errors: {
        house?: string[];
        perfume?: string[];
        rating?: string[];
        notes?: string[];
        _form?: string[];
    }
}

export async function UpdatePerfume(id: number, formState: UpdatePerfumeFormState, formData: FormData)
    : Promise<UpdatePerfumeFormState> {
    try {
        const perf = perfumeSchema.safeParse({   
            house: formData.get('house'),
            perfume: formData.get('perfume'),
            rating: formData.get('rating'),
            notes: formData.get('notes')
        });
        if (!perf.success) {
            return {
                errors: perf.error.flatten().fieldErrors,
            }
        }
        if (!parseFloat(perf.data.rating)) {
            console.log('Not a valid rating');
            return {
                errors: { rating: [ 'Not a valid rating' ]}
            }
        }
  
        const result = await db.perfume.update({
            where: {
                id: id
            },
            data: {
                house: perf.data.house,
                perfume: perf.data.perfume,
                rating: parseFloat(perf.data.rating),
                notes: perf.data.notes
            }
        });
        console.log(result);
    } catch (err: unknown) {
        if (err instanceof Error) {
            return {
                errors: { _form: [ err.message ] }
            };
        } else {
            return {
                errors: { _form: [ 'Unknown error occured' ] }
            };
        }
    }
    revalidatePath('/');
    redirect('/');
}

export async function GetPerfumes() {
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
  
  export  async function GetWorn() {
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

export async function getSuggestion() {
    const perfumes = (await GetPerfumes());
    const worn = await GetWorn();

    const wornPerfumes = new Set<number>;
    worn.map((x: any) => wornPerfumes.add(x.perfumeId));
    var list = perfumes.filter((x) => !wornPerfumes.has(x.id));
    if (!list) {
      let earliestWornIDs = worn.slice(-10).map(a => a.perfumeId);
      list = perfumes.filter((x) => earliestWornIDs.includes(x.id)); 
    }
    const ind: number = Math.floor(Math.random() * list.length);
    return list[ind].id;
}
  
export async function compareDates( a: PerfumeWorn, b:PerfumeWorn ) {
    if ( a.wornOn < b.wornOn ){
      return -1;
    }
    if ( a.wornOn > b.wornOn ){
      return 1;
    }
    return 0;
  }

export async function AddPerfume(formState: {}, formData: FormData) : Promise<UpdatePerfumeFormState> {
    try {
        const perf = perfumeSchema.safeParse({
            house: formData.get('house'),
            perfume: formData.get('perfume'),
            rating: formData.get('rating'),
            notes: formData.get('notes') 
        });
        if (!perf.success) {
            return {
                errors: perf.error.flatten().fieldErrors,
            }
        }
        if (!parseFloat(perf.data.rating)) {
            console.log('Not a valid rating');
            return {
                errors: { rating: [ 'Not a valid rating' ]}
            }
        }
       
        var exists = await db.perfume.findFirst({
            where: {
                house: perf.data.house,
                perfume: perf.data.perfume
            }
        });
        if (exists) {
            return {
                errors: { rating: [ 'Perfume already added' ]}
            }
        };
        await db.perfume.create({
            data: {
                house: perf.data.house,
                perfume: perf.data.perfume,
                rating: parseInt(perf.data.rating),
                notes: perf.data.notes
            }
        });
    } catch (err: unknown) {
        if (err instanceof Error) {
            return {
                errors: { _form: [ err.message ] }
            };
        } else {
            return {
                errors: { _form: [ 'Unknown error occured' ] }
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