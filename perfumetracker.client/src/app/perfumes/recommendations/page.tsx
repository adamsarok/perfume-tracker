"use client";

import React, { useEffect, useState } from "react";
import WornList from "@/components/worn-list";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import { getPerfumes } from "@/services/perfume-service";
import { showError } from "@/services/toasty-service";

export const dynamic = 'force-dynamic'

export default function RecommendationsPage() {
  const [perfumes, setPerfumes] = useState<PerfumeWithWornStatsDTO[]>([]);
  const [refreshTrigger, setRefreshTrigger] = useState<number>(0);

  useEffect(() => {
    async function fetchPerfumes() {
      const r = await getPerfumes();
      if (r.error || !r.data) {
        setPerfumes([]);
        showError("Could not load perfumes", r.error ?? "unknown error");
        return;
      }
      const perfumes = r.data.filter((x) => x.perfume.ml > 0); //todo filter on server side
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
        <WornList refreshTrigger={refreshTrigger} />
      </div>
    </div>
  );
}
