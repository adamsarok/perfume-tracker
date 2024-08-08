
import React from "react";
import PerfumeSelector from "../components/perfume-selector";
import PerfumeCard from "@/components/perfumecard";
import { Link } from "@nextui-org/react";
import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import * as perfumeRepo from "@/db/perfume-repo";

export const dynamic = 'force-dynamic'

export default async function Home() {
  const perfumes = await perfumeRepo.GetPerfumesForSelector();
  const worn = await perfumeWornRepo.GetWorn();
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
