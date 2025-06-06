
import React from "react";
import PerfumeSelector from "../components/perfume-selector";
import WornList from "@/components/worn-list";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
//import { getPerfumes } from "@/services/perfume-service";

export const dynamic = 'force-dynamic'

export default async function Home() {
  //const r = await getPerfumes();
  //const perfumes = r.filter((x) => x.perfume.ml > 0); //todo filter on server side
  const perfumes: PerfumeWithWornStatsDTO[] = [];
  return (
    <div>
      <div className="mt-2">
      <PerfumeSelector perfumes={perfumes}/>
      </div>
      <div className="mt-2">
      <WornList />
      </div>
    </div>
  );
}
