'use server';

import { Recommendation } from "@prisma/client";
import db from ".";

export async function insertRecommendation(query: string, recommendations: string | null) : Promise<ActionResult> {
    try {
        if (!query) return { ok: false, error: "Query empty" };
        await db.recommendation.create({
            data: {
                query,
                recommendations: recommendations ?? ''
            }
        });
        return { ok: true };
    } catch (err: unknown) {
        if (err instanceof Error) {
            return {
                ok: false,
                error: err.message
            };
        } else {
            return {
                ok: false,
                error: 'Unknown error occured'
            };
        }
    }
}

export async function getRecommendations() : Promise<Recommendation[]> {
    return await db.recommendation.findMany();
}


