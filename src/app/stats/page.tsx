//'use client';

import *  as actions from "@/app/actions";
import { PerfumeWornDTO } from "@/app/actions";
import PerfumeWornTable from "@/components/perfume-worn-table";
import { Link, Spacer } from "@nextui-org/react";

export default async function NewPerfumePage() {
    const perfumes = await actions.GetWornPerfumes();
    const past = new Date(0);
    //console.log(past);
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