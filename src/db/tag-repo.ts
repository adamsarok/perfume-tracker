'use server';

import db from ".";
import { z } from "zod";

interface InsertTagFormState {
    result: {
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


export async function InsertTag(formState: InsertTagFormState, formData: FormData) : Promise<InsertTagFormState> {
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
        return {
            result: {
                tag: perf.data.tag,
                color: perf.data.color
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


