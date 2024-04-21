
import React from "react";
import PerfumeSelector from "../components/perfume-selector";
import { db } from '@/db';
import { Perfume, PerfumeWorn } from "@prisma/client";
import PerfumeCard from "@/components/perfumecard";
import { Button, Link } from "@nextui-org/react";

export const dynamic = 'force-dynamic'

async function GetPerfumes() {
  return await db.perfume.findMany({
    orderBy: [
      {
        house: 'asc',
      },
      {
        perfume: 'asc',
      },
    ]
  });
}

async function GetWorn() {
  return await db.perfumeWorn.findMany({
    include: {
      perfume: true
    },
    orderBy: [
      {
        wornOn: 'desc',
      },
    ]
  });
}

function compareDates( a: PerfumeWorn, b:PerfumeWorn ) {
  if ( a.wornOn < b.wornOn ){
    return -1;
  }
  if ( a.wornOn > b.wornOn ){
    return 1;
  }
  return 0;
}

function getSuggestion(perfumes: Perfume[], worn: PerfumeWorn[]) {
  const wornPerfumes = new Set<number>;
  worn.map((x: any) => wornPerfumes.add(x.perfumeId));
  var list = perfumes.filter((x) => !wornPerfumes.has(x.id));
  if (!list) {
    let earliestWornIDs = worn.slice(-10).map(a => a.perfumeId);
    list = perfumes.filter((x) => earliestWornIDs.includes(x.id)); 
  }
  const ind: number = Math.floor(Math.random() * list.length);
  return list[ind];
}

export default async function Home() {
  const perfumes = (await GetPerfumes()).filter((x) => x.rating >= 8);
  const worn = await GetWorn();
  const suggestion = getSuggestion(perfumes, worn);
  return (
    <div >
      <Link isBlock showAnchorIcon color="foreground" href="/new-perfume">New Perfume</Link>
      <Link isBlock showAnchorIcon color="foreground" href="/stats">Stats</Link>
      <PerfumeSelector perfumes={perfumes} defaultInput={suggestion?.house + ' - ' + suggestion?.perfume} defaultSelectedKey={suggestion?.id}/> {/*defaultSelectedKey={suggestion?.id} */}
        {worn.map((wornon) => (
          <PerfumeCard perfume={wornon.perfume} wornOn={wornon.wornOn} id={wornon.id} avatar={null}></PerfumeCard>
        ))}
    </div>
  );   
}
