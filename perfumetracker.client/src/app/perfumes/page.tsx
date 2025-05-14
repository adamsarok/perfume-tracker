import PerfumeWornTable from "./perfume-worn-table";
import { Separator } from "@/components/ui/separator";
import { getUserProfile } from "@/services/user-profiles-service";
import { getTags } from "@/services/tag-service";

export const dynamic = 'force-dynamic'

export default async function StatsPage() {
    const tags = await getTags();
    const userProfile = await getUserProfile();
    return <div>
      <Separator></Separator>
      <PerfumeWornTable allTags={tags} userProfile={userProfile}></PerfumeWornTable>
    </div>
}