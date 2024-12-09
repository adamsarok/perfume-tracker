import PerfumeWornTable from "./perfume-worn-table";
import { Separator } from "@/components/ui/separator";
import { getTags } from "@/services/tag-service";

export const dynamic = 'force-dynamic'

export default async function StatsPage() {
    const tags = await getTags();
    return <div>
      <Separator></Separator>
      <PerfumeWornTable allTags={tags}></PerfumeWornTable>
    </div>
}