import PerfumeEditForm from "@/components/perfume-edit-form";
import { R2_API_ADDRESS } from "@/services/conf";
import { getTags } from "@/services/tag-service";

export const dynamic = 'force-dynamic'

export default async function NewPerfumePage() {
    const tags = await getTags();
    return <PerfumeEditForm perfume={null} perfumesTags={[]} allTags={tags} r2_api_address={R2_API_ADDRESS}></PerfumeEditForm>
}