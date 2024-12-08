import * as tagRepo from "@/db/tag-repo";
import PerfumeWornTable from "./perfume-worn-table";
import { Separator } from "@/components/ui/separator";

export const dynamic = 'force-dynamic'

export default async function StatsPage() {
    const tags = await tagRepo.getTags();
    return <div>
      <Separator></Separator>
      <PerfumeWornTable allTags={tags}></PerfumeWornTable>
    </div>
}