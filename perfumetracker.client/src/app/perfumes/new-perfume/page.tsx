import PerfumeEditForm from "@/components/perfume-edit-form";
import { getTags } from "@/services/tag-service";
import { env } from "process";

export const dynamic = 'force-dynamic'

export default async function NewPerfumePage() {
    const tags = await getTags();
    return <PerfumeEditForm perfumeWithWornStats={null} perfumesTags={[]} allTags={tags} r2_api_address={env.NEXT_PUBLIC_R2_API_ADDRESS} isRandomPerfume={false}></PerfumeEditForm>
}