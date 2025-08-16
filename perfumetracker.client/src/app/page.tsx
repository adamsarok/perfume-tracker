"use client";

import React, { useEffect, useState } from "react";
import PerfumeSelector from "../components/perfume-selector";
import WornList from "@/components/worn-list";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import { getPerfumes } from "@/services/perfume-service";

export const dynamic = 'force-dynamic'

export default function Home() {
  const [perfumes, setPerfumes] = useState<PerfumeWithWornStatsDTO[]>([]);
  const [refreshTrigger, setRefreshTrigger] = useState<number>(0);

  useEffect(() => {
    async function fetchPerfumes() {
      const r = await getPerfumes();
      const perfumes = r.filter((x) => x.perfume.ml > 0); //todo filter on server side
      setPerfumes(perfumes);
    }
    fetchPerfumes();
  }, []);

  const handleSprayOnSuccess = () => {
    setRefreshTrigger(prev => prev + 1);
  };

  return (
    <div>
      <div className="mt-2">
        <PerfumeSelector perfumes={perfumes} onSprayOnSuccess={handleSprayOnSuccess} />
      </div>
      <div className="mt-2">
        <WornList refreshTrigger={refreshTrigger} />
      </div>
    </div>
  );
}
