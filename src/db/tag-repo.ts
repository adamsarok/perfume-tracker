'use server';

import { Tag } from "@prisma/client";
import db from ".";
import { z } from "zod";

interface InsertTagFormState {
    result: {
        id: number,
        tag: string,
        color: string
    } | null
    errors: {
        name?: string[];
        color?: string[];
        _form?: string[];
    }
}

const tagSchema = z.object({
    tag: z.string().min(1),
    color: z.string().min(1) //TODO color validate
})


export async function insertTag(formState: InsertTagFormState, formData: FormData) : Promise<InsertTagFormState> {
    try {
        console.log(formData);
        const perf = tagSchema.safeParse({
            tag: formData.get('tag'),
            color: formData.get('color')
        });
        if (!perf.success) {
            console.log(perf.error.flatten().fieldErrors);
            return {
                result: null,
                errors: perf.error.flatten().fieldErrors,
            }
        }
        const result = await db.tag.create({
            data: {
                tag: perf.data.tag,
                color: perf.data.color,
            }
        });
        console.log(result);
        return {
            result: {
                id: result.id,
                tag: result.tag,
                color: result.color
            },
            errors: { _form: [] }
        };
    } catch (err: unknown) {
        if (err instanceof Error) {
            return {
                result: null,
                errors: { _form: [err.message] }
            };
        } else {
            return {
                result: null,
                errors: { _form: ['Unknown error occured'] }
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
    } catch (error: any) {
        return { success: false, error: error.message || 'Failed to delete tag' };
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
    } catch (error: any) {
        return { success: false, error: error.message || 'Failed to delete tag' };
    }
}


