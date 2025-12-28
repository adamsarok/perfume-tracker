"use client";

import React from "react";
import { Card, CardHeader } from "@/components/ui/card";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { showError, showSuccess } from "@/services/toasty-service";
import { useAuth } from "@/hooks/use-auth";
import { Star, Sparkles, Calendar, Dice5, ChartNoAxesCombined } from "lucide-react";
import ColorChip from "@/components/color-chip";
import SprayOnComponent from "@/components/spray-on";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { PerfumeRecommendationDTO } from "@/dto/PerfumeRecommendationDTO";

export interface PerfumeRecommendationsCardProps {
  readonly recommendation: PerfumeRecommendationDTO;
}

const getRecommendationIcon = (type?: string) => { 
  switch (type) {
    case "ForgottenFavorite":
      return <Star className="w-5 h-5 text-yellow-500" />;
    case "MoodBased":
      return <Sparkles className="w-5 h-5 text-pink-500" />;
    case "Seasonal":
      return <Calendar className="w-5 h-5 text-blue-500" />;
    case "Random":
      return <Dice5 className="w-5 h-5 text-purple-500" />;
    case "LeastUsed":
      return <ChartNoAxesCombined className="w-5 h-5 text-gray-500" />;
    default:
      return null;
  }
};

export default function PerfumeRecommendationsCard({
  recommendation,
}: PerfumeRecommendationsCardProps) {
  const auth = useAuth();
  if (!recommendation) return <div></div>;
  const perfume = recommendation.perfume.perfume;
  const avatar =
    perfume.perfumeName.split(" ").length > 1
      ? perfume.perfumeName
        .split(" ")
        .map((x) => x[0])
        .slice(0, 2)
        .join("")
      : perfume.perfumeName.slice(0, 2).toUpperCase();

  return (
    <form>
      <Card key={perfume.id} className="w-full perfume-card">
        <CardHeader>
          <a
            href={`/perfumes/${perfume.id}/`}
            className="flex items-center justify-between gap-4"
          >
            <div className="flex items-center space-x-4">
              <Avatar className="w-16 h-16 sm:w-20 sm:h-20 semi-bold">
                <AvatarImage
                  className="object-cover"
                  src={perfume.imageUrl}
                />
                <AvatarFallback>{avatar}</AvatarFallback>
              </Avatar>
              <div className="text-small leading-none text-default-600">
                {getRecommendationIcon(recommendation.strategy)}
                <p className="whitespace-normal text-small">{perfume.house} - {perfume.perfumeName}</p>
                <TooltipProvider>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <p className="mt-2 text-small tracking-tight text-default-400">
                        {`Worn on: ${recommendation.perfume.lastWorn?.toDateString()}`}
                      </p>
                    </TooltipTrigger>
                    <TooltipContent>
                      <div>
                        {perfume.tags.map((x) => (
                          <ColorChip
                            key={x.id}
                            className="mr-2"
                            onChipClick={null}
                            name={x.tagName}
                            color={x.color}
                          ></ColorChip>
                        ))}
                      </div>
                    </TooltipContent>
                  </Tooltip>
                </TooltipProvider>
              </div>
            </div>
             <SprayOnComponent
                    perfumeId={perfume.id}
                    onSuccess={null}
                    className="mb-2 mt-2"
                    randomsId={null}
                    showFullComponent={false}
                  ></SprayOnComponent>
          </a>
        </CardHeader>
      </Card>
    </form>
  );
}
