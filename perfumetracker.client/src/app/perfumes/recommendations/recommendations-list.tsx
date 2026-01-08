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
import { getUserProfile, updateUserProfile } from "@/services/user-profiles-service";
import { getRecommendationIcon } from "./recommendation-icons";

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

  const loadUserProfile = async () => {
    const result = await getUserProfile();
    if (result.data?.preferredRecommendationStrategies && result.data.preferredRecommendationStrategies.length > 0) {
      setSelectedStrategies(result.data.preferredRecommendationStrategies);
    }
  };

  const refreshList = async (occasion?: string, savePreferences: boolean = false) => {
    // Save preferences if requested
    console.log("Save: " + savePreferences);
    if (savePreferences) {
      const profileResult = await getUserProfile();
      if (profileResult.data) {
        const updatedProfile = {
          ...profileResult.data,
          preferredRecommendationStrategies: selectedStrategies
        };
        const updateResult = await updateUserProfile(updatedProfile);
        if (updateResult.error) {
          showError("Could not save preferences", updateResult.error);
        }
      }
    }

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
    const loadData = async () => {
      const profileResult = await getUserProfile();
      console.log(profileResult);
      if (profileResult.data?.preferredRecommendationStrategies && profileResult.data.preferredRecommendationStrategies.length > 0) {
        setSelectedStrategies(profileResult.data.preferredRecommendationStrategies);
        // Use the loaded strategies for the initial recommendations
        const result = await getPerfumeRecommendations(
          recommendationsCount,
          undefined,
          profileResult.data.preferredRecommendationStrategies
        );
        if (result.error || !result.data) {
          showError(
            "Could not load recommendations",
            result.error ?? "unknown error"
          );
          return;
        }
        setRecommendations(result.data);
      } else {
        // Use default strategies if no preferences found
        await refreshList();
      }
    };
    loadData();
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
                {RecommendationStrategyLabels[strategy]} {getRecommendationIcon(strategy)}
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
        <Button onClick={() => refreshList(occasionOrMood, true)} className="mb-4">
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
