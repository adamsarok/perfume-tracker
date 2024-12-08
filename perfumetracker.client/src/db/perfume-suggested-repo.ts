'use server';

import db from ".";

export async function getAlreadySuggestedPerfumeIds(dayFilter: Date) {
    return await db.perfumeSuggested.findMany({
        where: {
            suggestedOn: {
                gte: dayFilter,
            },
        },
        select: {
            perfumeId: true,
        },
        distinct: ["perfumeId"],
    });
}


export async function addSuggested(perfumeId: number) {
    await db.perfumeSuggested.create({
        data: {
            suggestedOn: new Date(),
            perfume: {
                connect: {
                    id: perfumeId
                }
            }
        },
    });
}