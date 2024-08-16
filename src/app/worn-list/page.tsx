import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import * as tagRepo from "@/db/tag-repo";
import { Link, Spacer } from "@nextui-org/react";
import PerfumeWornTable from "./perfume-worn-table";

export const dynamic = 'force-dynamic'

export default async function StatsPage() {
    const perfumes = await perfumeWornRepo.getAllPerfumesWithWearCount();
    const past = new Date(0);
    perfumes.sort((a, b) => {
        let dateA = a.lastWorn ? a.lastWorn : past;
        let dateB = b.lastWorn ? b.lastWorn : past;
        return dateB.getTime() - dateA.getTime();
      });
    const tags = await tagRepo.getTags();
    return <div>
      <Link isBlock showAnchorIcon href='/' color="foreground">Back</Link>
      <Spacer></Spacer>
      <PerfumeWornTable perfumes={perfumes} allTags={tags}></PerfumeWornTable>
    </div>
}