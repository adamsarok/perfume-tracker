import PerfumeWornTable from "./perfume-worn-table";
import { Separator } from "@/components/ui/separator";
import { getSettings } from "@/services/settings-service";
import { getTags } from "@/services/tag-service";

export const dynamic = 'force-dynamic'

export default async function StatsPage() {
    const tags = await getTags();
    const settings = await getSettings();
    return <div>
      <Separator></Separator>
      <PerfumeWornTable allTags={tags} settings={settings}></PerfumeWornTable>
    </div>
}