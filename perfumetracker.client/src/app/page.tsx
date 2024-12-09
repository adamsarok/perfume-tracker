
import React from "react";
import PerfumeSelector from "../components/perfume-selector";
import WornList from "@/components/worn-list";
import { R2_API_ADDRESS } from "@/services/conf";
import { getPerfumes } from "@/services/perfume-service";

export const dynamic = 'force-dynamic'

export default async function Home() {
  const r = await getPerfumes();
  const perfumes = r.filter((x) => x.perfume.ml > 0); //todo filter on server side
  return (
    <div>
      <div className="mt-2">
      <PerfumeSelector perfumes={perfumes}/>
      </div>
      <div className="mt-2">
      <WornList r2_api_address={R2_API_ADDRESS} />
      </div>
    </div>
  );
}
