import *  as actions from "@/app/actions";
import PerfumeWornTable from "@/components/perfume-worn-table";
import { Link, Spacer } from "@nextui-org/react";

export const dynamic = 'force-dynamic'

export default async function StatsPage() {
    const perfumes = await actions.GetAllPerfumesWithWearCount();
    const past = new Date(0);
    perfumes.sort((a, b) => {
        let dateA = a.lastWorn ? a.lastWorn : past;
        let dateB = b.lastWorn ? b.lastWorn : past;
        return dateB.getTime() - dateA.getTime();
      });
    return <div>
      <Link isBlock showAnchorIcon href='/' color="foreground">Back</Link>
      <Spacer></Spacer>
      <PerfumeWornTable perfumes={perfumes}></PerfumeWornTable>
    </div>
}