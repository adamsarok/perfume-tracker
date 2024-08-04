//'use client';

import { Button, Input, Link } from "@nextui-org/react";
import { useFormState } from "react-dom";
import *  as actions from "@/app/actions";
import PerfumeEditForm from "@/components/perfume-edit-form";
import { db } from "@/db";
import { notFound } from "next/navigation";
import { Tag } from "@prisma/client";

interface EditPerfumePageProps {
    params: {
        id: string
    }
}

export default async function EditPerfumePage({params}: EditPerfumePageProps) {
    const perfume = await db.perfume.findFirst({
        where: {
            id: parseInt(params.id)
        },
        include: {
            tags: {
                include: {
                    tag: true
                }
            }
        }
    });
    console.log(perfume);
    if (!perfume) return notFound();
    var allTags = await db.tag.findMany();
    var tags: Tag[] = [];
    perfume.tags.map(x => {
        tags.push({
            tag: x.tag.tag,
            color: x.tag.color
        })
    });
    //allTags = allTags.filter(allTag => !tags.some(tag => tag.tag === allTag.tag));
    return <PerfumeEditForm perfume={perfume} tags={tags} allTags={allTags}></PerfumeEditForm>
}