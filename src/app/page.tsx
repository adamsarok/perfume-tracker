
import React from "react";
import PerfumeSelector from "./perfume-selector";
import { db } from '@/db';
import { Perfume, PerfumeWorn } from "@prisma/client";
import PerfumeCard from "@/components/perfumecard";
import { Button, Link } from "@nextui-org/react";

//export const dynamic = 'force-dynamic'

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
      perfume: true,
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
  const notWorn = perfumes.find((x) => !wornPerfumes.has(x.id));
  if (notWorn) return notWorn;
  return perfumes.find((x) => x.id === worn.slice(-1)[0].perfumeId);
}

export default async function Home() {
  const perfumes = await GetPerfumes();
  const worn = await GetWorn();
  const suggestion = getSuggestion(perfumes, worn);
  return (
      <div >
        <Link isBlock showAnchorIcon color="foreground" href="/new-perfume">New Perfume</Link>
        <Link isBlock showAnchorIcon color="foreground" href="/stats">Stats</Link>
       {suggestion && <PerfumeCard perfume={suggestion} wornOn={null} id={suggestion.id} avatar="🎁"></PerfumeCard>}
 
          <PerfumeSelector perfumes={perfumes} defaultSelectedKey={suggestion?.id}/>
            {worn.map((wornon) => (
                <PerfumeCard perfume={wornon.perfume} wornOn={wornon.wornOn} id={wornon.id} avatar={null}></PerfumeCard>
              ))}
      </div>
  );

//   <main className="flex min-h-screen flex-col items-center justify-between p-24">
//   <div className="z-10 max-w-5xl w-full items-center justify-between font-mono text-sm lg:flex">
//     <PerfumeSelector perfumes={perfumes} />
//   </div>
// </main>
   
}
