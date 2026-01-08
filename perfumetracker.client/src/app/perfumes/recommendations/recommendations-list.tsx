"use client";

import React, { useEffect, useState } from "react";
import { showError } from "@/services/toasty-service";
import { PerfumeRecommendationDTO } from "@/dto/PerfumeRecommendationDTO";
import { getPerfumeRecommendations } from "@/services/perfume-service";
import PerfumeRecommendationsCard from "./recommendations-card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { RecommendationStrategy, RecommendationStrategyLabels } from "@/dto/RecommendationStrategy";

export default function RecommendationsList() {
  const [recommendations, setRecommendations] = useState<
    PerfumeRecommendationDTO[]
  >([]);
  const [occasionOrMood, setOccasionOrMood] = useState("");
  const [selectedStrategies, setSelectedStrategies] = useState<RecommendationStrategy[]>([
    RecommendationStrategy.ForgottenFavorite,
    RecommendationStrategy.SimilarToLastUsed,
    RecommendationStrategy.Seasonal,
    RecommendationStrategy.Random,
    RecommendationStrategy.LeastUsed
  ]);
  const recommendationsCount = 5;

  const availableStrategies = [
    RecommendationStrategy.ForgottenFavorite,
    RecommendationStrategy.SimilarToLastUsed,
    RecommendationStrategy.Seasonal,
    RecommendationStrategy.Random,
    RecommendationStrategy.LeastUsed
  ];

  const refreshList = async (occasion?: string) => {
    const result = await getPerfumeRecommendations(
      recommendationsCount,
      occasion,
      selectedStrategies.length > 0 ? selectedStrategies : undefined
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

  const toggleStrategy = (strategy: RecommendationStrategy) => {
    setSelectedStrategies(prev => {
      if (prev.includes(strategy)) {
        return prev.filter(s => s !== strategy);
      } else {
        return [...prev, strategy];
      }
    });
  };

  useEffect(() => {
    const loadRecommendations = async () => {
      await refreshList();
    };
    loadRecommendations();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div className="w-full max-w-3xl mx-auto px-2">
      <div className="mb-4">
        <Label className="mb-2 block">Recommendation Strategies</Label>
        <div className="grid grid-cols-2 gap-3 mb-4 p-3 border rounded-md">
          {availableStrategies.map((strategy) => (
            <div key={strategy} className="flex items-center space-x-2">
              <Checkbox
                id={`strategy-${strategy}`}
                checked={selectedStrategies.includes(strategy)}
                onCheckedChange={() => toggleStrategy(strategy)}
              />
              <label
                htmlFor={`strategy-${strategy}`}
                className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 cursor-pointer"
              >
                {RecommendationStrategyLabels[strategy]}
              </label>
            </div>
          ))}
        </div>
      </div>
      
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
