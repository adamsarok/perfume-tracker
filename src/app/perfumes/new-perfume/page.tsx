import PerfumeEditForm from "@/components/perfume-edit-form";
import { db } from "@/db";

export default async function NewPerfumePage() {
    const tags = await db.tag.findMany();
    return <PerfumeEditForm perfume={null} tags={[]} allTags={tags}></PerfumeEditForm>
}