
import React from "react";
import PerfumeSelector from "../components/perfume-selector";
import PerfumeCard from "@/components/perfumecard";
import { Link } from "@nextui-org/react";
import * as actions from '@/app/actions';

export const dynamic = 'force-dynamic'

export default async function Home() {
  const perfumes = await actions.GetPerfumesForSelector();
  const worn = await actions.GetWorn();
  return (
    <div >
      <Link isBlock showAnchorIcon color="foreground" href="/perfumes/new-perfume">New Perfume</Link>
      <Link isBlock showAnchorIcon color="foreground" href="/worn-list">Worn List</Link>
      <Link isBlock showAnchorIcon color="foreground" href="/stats">Stats</Link>
      <PerfumeSelector perfumes={perfumes}/>
        {worn.map((wornon) => (
          <PerfumeCard perfume={wornon.perfume} wornId={wornon.id} wornCount={undefined} wornOn={wornon.wornOn}></PerfumeCard>
        ))}
    </div>
  );   
}
