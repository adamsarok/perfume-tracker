
import React from "react";
import PerfumeSelector from "../components/perfume-selector";
import { db } from '@/db';
import { Perfume, PerfumeWorn } from "@prisma/client";
import PerfumeCard from "@/components/perfumecard";
import { Button, Link } from "@nextui-org/react";
import * as actions from '@/app/actions';

export const dynamic = 'force-dynamic'

export default async function Home() {
  const perfumes = await actions.GetPerfumes();
  const worn = await actions.GetWorn();
  return (
    <div >
      <Link isBlock showAnchorIcon color="foreground" href="/perfumes/new-perfume">New Perfume</Link>
      <Link isBlock showAnchorIcon color="foreground" href="/stats">Stats</Link>
      <PerfumeSelector perfumes={perfumes}/>
        {worn.map((wornon) => (
          <PerfumeCard perfume={wornon.perfume} wornOn={wornon.wornOn} id={wornon.id} avatar={null}></PerfumeCard>
        ))}
    </div>
  );   
}
