'use server';

import { db } from "@/db";
import { redirect } from "next/navigation";

//warning todo utc
export async function AddPerfume(id: number) {
    /*
    Error: 
    Invalid `prisma.perfumeWorn.create()` invocation:

    {
    data: {
        wornOn: new Date("2024-03-24T20:51:50.183Z"),
        perfume: {
        connect: {
            id: "109"
    */
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
        console.log('already added this perfume today');
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
}