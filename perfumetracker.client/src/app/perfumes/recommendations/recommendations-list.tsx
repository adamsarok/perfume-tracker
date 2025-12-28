"use client";

import React, { useEffect, useState } from "react";
import PerfumeCard from "@/components/perfumecard";
import { useInView } from "react-intersection-observer";
import { PerfumeWornDTO } from "@/dto/PerfumeWornDTO";
import { getWornBeforeID } from "@/services/perfume-worn-service";
import { showError } from "@/services/toasty-service";
import { PerfumeRecommendationDTO } from "@/dto/PerfumeRecommendationDTO";
import { getPerfumeRecommendations } from "@/services/perfume-service";
import PerfumeRecommendationsCard from "./recommendations-card";

export default function RecommendationsList() {
  const [recommendations, setRecommendations] = useState<PerfumeRecommendationDTO[]>([]);
  const recommendationsCount = 5;

  const refreshList = async () => {
    setRecommendations([]);
    const result = await getPerfumeRecommendations(recommendationsCount);
    if (result.error || !result.data) {
      showError("Could not load recommendations", result.error ?? "unknown error");
      return;
    }
    //const newRecommendations = result.data;
    //newRecommendations.forEach(x => x.eventDate = new Date(x.eventDate));
    setRecommendations(result.data);
    console.log("Loaded recommendations:", result.data);
  };

  useEffect(() => {
    refreshList();
  }, []);

  return (
    <div className="w-full max-w-3xl mx-auto px-2">
      <div className="grid grid-cols-1 gap-4">
        {recommendations.map((recommendation) => (
          <PerfumeRecommendationsCard key={recommendation.perfume.perfume.id} 
            recommendation={recommendation}></PerfumeRecommendationsCard>
        ))}
      </div>
    </div>
  );
}
