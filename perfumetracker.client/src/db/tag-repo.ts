'use server';

import { Tag } from "@prisma/client";
import db from ".";
import { ActionResult } from "./action-result";

export async function insertTag(tag: Tag) : Promise<ActionResult> {
    try {
        const result = await db.tag.create({
            data: {
                tag: tag.tag,
                color: tag.color,
            }
        });
        return {
            ok: true,
            id: result.id
        };
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

export async function getTags() : Promise<Tag[]> {
    return await db.tag.findMany();
}

interface TagResult {
    success: boolean;
    error: string;
}

export async function deleteTag(id: number) : Promise<TagResult> {
    try {    
        await db.tag.delete({
                where: {
                    id
                }
            });
        return { success: true, error: "" }
    } catch (err: unknown) {
        if (err instanceof Error) {
            return { success: false, error: err.message || 'Failed to delete tag' };
         } else {
            return { success: false, error: 'Failed to delete tag: Unknown error occured' };
        }
    } 
}

export async function updateTag(id: number, tag: string, color: string) : Promise<TagResult> {
    try {    
        await db.tag.update({
                where: {
                    id
                },
                data: {
                    tag,
                    color
                }
            });
        return { success: true, error: "" }
    } catch (err: unknown) {
        if (err instanceof Error) {
            return { success: false, error: err.message || 'Failed to update tag' };
         } else {
            return { success: false, error: 'Failed to update tag: Unknown error occured' };
        }
    }
}


