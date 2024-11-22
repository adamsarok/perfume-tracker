import * as tagRepo from "@/db/tag-repo";
import PerfumeWornTable from "./perfume-worn-table";
import { Separator } from "@/components/ui/separator";
import { PERFUMETRACKER_API_ADDRESS } from "@/services/conf";

export const dynamic = 'force-dynamic'

export default async function StatsPage() {
    const tags = await tagRepo.getTags();
    return <div>
      <Separator></Separator>
      <PerfumeWornTable apiAddress={PERFUMETRACKER_API_ADDRESS} allTags={tags}></PerfumeWornTable>
    </div>
}