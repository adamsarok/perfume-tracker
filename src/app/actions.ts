'use server';

import { db } from "@/db";
import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";

export async function AddPerfume(formState: {}, formData: FormData) {
    try {

        const house = formData.get('house');
        const perfume = formData.get('perfume');
        const rating = formData.get('rating');

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
        console.log('itt vagyug');
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
                rating: parseFloat(rating)
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
    console.log("Perfume add fire with ID: " + id);
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