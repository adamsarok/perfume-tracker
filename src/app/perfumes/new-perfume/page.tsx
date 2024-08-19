import PerfumeEditForm from "@/components/perfume-edit-form";
import db from "@/db";

export const dynamic = 'force-dynamic'

export default async function NewPerfumePage() {
    const tags = await db.tag.findMany();
    return <PerfumeEditForm perfume={null} perfumesTags={[]} allTags={tags}></PerfumeEditForm>
}