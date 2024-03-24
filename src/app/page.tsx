
import React from "react";
import PerfumeSelector from "./perfume-selector";
import { db } from '@/db';
import * as actions from '@/app/seed';

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

export default async function Home() {
  let perfumes = await GetPerfumes();
  if (perfumes.length == 0) { 
    await actions.SeedCSV();
    perfumes = await GetPerfumes();
  }
  const worn = await GetWorn();
  return (
      <div className="flex">
        <div className="flex-initial w-1/3">
          <PerfumeSelector perfumes={perfumes} />
        </div>
        <div className="flex-initial w-2/3">
            {worn.map((wornon) => (
                <div key={wornon.id}>
                  <p>{wornon.wornOn.toDateString()} - {wornon.perfume.house} - {wornon.perfume.perfume}</p>
                </div>
              ))}
        </div>
      </div>
  );

//   <main className="flex min-h-screen flex-col items-center justify-between p-24">
//   <div className="z-10 max-w-5xl w-full items-center justify-between font-mono text-sm lg:flex">
//     <PerfumeSelector perfumes={perfumes} />
//   </div>
// </main>
   
}
