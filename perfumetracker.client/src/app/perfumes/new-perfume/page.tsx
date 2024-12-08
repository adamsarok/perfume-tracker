import PerfumeEditForm from "@/components/perfume-edit-form";
import db from "@/db";
import { R2_API_ADDRESS } from "@/services/conf";

export const dynamic = 'force-dynamic'

export default async function NewPerfumePage() {
    const tags = await db.tag.findMany();
    return <PerfumeEditForm perfume={null} perfumesTags={[]} allTags={tags} r2_api_address={R2_API_ADDRESS}></PerfumeEditForm>
}