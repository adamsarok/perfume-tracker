import PerfumeEditForm from "@/components/perfume-edit-form";
import { notFound } from "next/navigation";
import { R2_API_ADDRESS } from "@/services/conf";
import { getPerfume } from "@/services/perfume-service";
import { TagDTO } from "@/dto/TagDTO";
import { getTags } from "@/services/tag-service";

export const dynamic = 'force-dynamic'

interface EditPerfumePageProps {
    readonly params: Promise<{
        id: string
    }>
}

export default async function EditPerfumePage(props: EditPerfumePageProps) {
    const params = await props.params;
    const id = parseInt(params.id);
    if (!id) return notFound();
    const perfume = await getPerfume(id);
    if (!perfume) return notFound();
    const allTags = await getTags();
    const tags: TagDTO[] = [];
    perfume.perfume.tags.forEach(x => {
        tags.push({
            id: x.id,
            tagName: x.tagName,
            color: x.color
        })
    });
    console.log(R2_API_ADDRESS);
    return <PerfumeEditForm perfumeWithWornStats={perfume} perfumesTags={tags} allTags={allTags} r2_api_address={R2_API_ADDRESS}></PerfumeEditForm>
}