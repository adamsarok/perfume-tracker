import PerfumeEditForm from "@/components/perfume-edit-form";
import { notFound } from "next/navigation";
import { Tag } from "@prisma/client";
import * as perfumeRepo from "@/db/perfume-repo";
import db from "@/db";

interface EditPerfumePageProps {
    params: {
        id: string
    }
}

export default async function EditPerfumePage({params}: EditPerfumePageProps) {
    const id = parseInt(params.id);
    if (!id) return notFound();
    const perfume = await perfumeRepo.GetPerfumeWithTags(id);
    console.log(perfume);
    if (!perfume) return notFound();
    //TODO: clean DB from pages
    let allTags = await db.tag.findMany();
    let tags: Tag[] = [];
    perfume.tags.map(x => {
        tags.push({
            tag: x.tag.tag,
            color: x.tag.color
        })
    });
    return <PerfumeEditForm perfume={perfume} perfumesTags={tags} allTags={allTags}></PerfumeEditForm>
}