"use client";

import React, { useEffect, useState } from "react";
import PerfumeCard from "@/components/perfumecard";
import { useInView } from "react-intersection-observer";
import { PerfumeWornDTO } from "@/dto/PerfumeWornDTO";
import { getWornBeforeID } from "@/services/perfume-worn-service";
import { showError } from "@/services/toasty-service";
import { getPerfumeRecommendations } from "@/services/perfume-service";
import { PerfumeRecommendationDTO } from "@/dto/PerfumeRecommendationDTO";

export default function PerfumeRecommendationsList() {
  const [worns, setWorns] = useState<PerfumeRecommendationDTO[]>([]);
  const [cursor, setCursor] = useState<number | null>(null);
  const { ref, inView } = useInView();
  const cardsPerPage = 10;

  const load = async () => {
    const result = await getPerfumeRecommendations();
    if (result.error || !result.data) {
      showError("Could not load perfume recommendations", result.error ?? "unknown error");
      return;
    }
    setWorns(result.data);
  };

  useEffect(() => {
    load();
  }, []);

  return (
    <div className="w-full max-w-3xl mx-auto px-2">
      <div className="grid grid-cols-1 gap-4">
        {worns.map((worn) => (
          <PerfumeCard key={worn.id} worn={worn}></PerfumeCard>
        ))}
      </div>
      {cursor !== null && (
        <div ref={ref} className="text-center py-4">
          Loading...
        </div>
      )}
    </div>
  );
}
