"use client";

import React, { useEffect, useState } from "react";
import { showError } from "@/services/toasty-service";
import { PerfumeRecommendationDTO } from "@/dto/PerfumeRecommendationDTO";
import { getPerfumeRecommendations } from "@/services/perfume-service";
import PerfumeRecommendationsCard from "./recommendations-card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export default function RecommendationsList() {
  const [recommendations, setRecommendations] = useState<
    PerfumeRecommendationDTO[]
  >([]);
  const [occasionOrMood, setOccasionOrMood] = useState("");
  const recommendationsCount = 5;

  const refreshList = async (occasion?: string) => {
    const result = await getPerfumeRecommendations(
      recommendationsCount,
      occasion
    );
    if (result.error || !result.data) {
      showError(
        "Could not load recommendations",
        result.error ?? "unknown error"
      );
      return;
    }
    setRecommendations(result.data);
  };

  useEffect(() => {
    refreshList();
  }, []);

  return (
    <div className="w-full max-w-3xl mx-auto px-2">
      <Label htmlFor="occasionOrMood">Occasion or Mood</Label>
      <div className="flex space-x-2" >
        <Input
          id="occasionOrMood"
          type="text"
          aria-label="Occasion or Mood"
          placeholder="Summer night..."
          className="mb-4"
          value={occasionOrMood}
          onChange={(e) => setOccasionOrMood(e.target.value)}
        />
        <Button onClick={() => refreshList(occasionOrMood)} className="mb-4">
          Recommend
        </Button>
      </div>
      <div className="grid grid-cols-1 gap-4">
        {recommendations.map((recommendation) => (
          <PerfumeRecommendationsCard
            key={recommendation.perfume.perfume.id}
            recommendation={recommendation}
          ></PerfumeRecommendationsCard>
        ))}
      </div>
    </div>
  );
}
