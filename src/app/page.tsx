
import React from "react";
import PerfumeSelector from "./perfume-selector";
import { db } from '@/db';
import * as actions from '@/app/seed';
import { Avatar, Card, CardBody, CardHeader, Divider } from "@nextui-org/react";

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
  // if (perfumes.length == 0) { 
  //   await actions.SeedCSV();
  //   perfumes = await GetPerfumes();
  // }
  const worn = await GetWorn();
  return (
      <div >
          <PerfumeSelector perfumes={perfumes} />
            {worn.map((wornon) => (
                <Card key={wornon.id}>
                  <CardHeader>
                    <Avatar className="semi-bold" 
                      name={wornon.perfume.perfume.split(" ").length > 1 ? 
                          wornon.perfume.perfume.split(" ").map((x) => x[0]).slice(0,2).join("") 
                          : wornon.perfume.perfume.slice(0,2).toUpperCase()} />
                    <p className="text-small leading-none text-default-600 ml-4">{wornon.perfume.house} - {wornon.perfume.perfume}</p>
                  </CardHeader>
                  <CardBody>
                    <p className="text-small tracking-tight text-default-400">{wornon.wornOn.toDateString()}</p>
                  </CardBody>
                </Card>
              ))}
      </div>
  );

//   <main className="flex min-h-screen flex-col items-center justify-between p-24">
//   <div className="z-10 max-w-5xl w-full items-center justify-between font-mono text-sm lg:flex">
//     <PerfumeSelector perfumes={perfumes} />
//   </div>
// </main>
   
}
