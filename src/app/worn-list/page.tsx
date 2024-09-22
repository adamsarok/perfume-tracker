import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import * as tagRepo from "@/db/tag-repo";
import { Spacer } from "@nextui-org/react";
import PerfumeWornTable from "./perfume-worn-table";

export const dynamic = 'force-dynamic'

export default async function StatsPage() {
    const perfumes = await perfumeWornRepo.getAllPerfumesWithWearCount();
    const past = new Date(0);
    perfumes.sort((a, b) => {
        const dateA = a.lastWorn ? a.lastWorn : past;
        const dateB = b.lastWorn ? b.lastWorn : past;
        return dateB.getTime() - dateA.getTime();
      });
    const tags = await tagRepo.getTags();
    return <div>
      <Spacer></Spacer>
      <PerfumeWornTable perfumes={perfumes} allTags={tags}></PerfumeWornTable>
    </div>
}