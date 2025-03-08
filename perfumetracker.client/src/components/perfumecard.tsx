"use client";

import React from "react";
import ColorChip from "./color-chip";
import { getImageUrl } from "./r2-image";

import { Card, CardHeader } from "@/components/ui/card";
import { Button } from "./ui/button";
import { Avatar, AvatarFallback, AvatarImage } from "./ui/avatar";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "./ui/tooltip";
import { PerfumeWornDTO } from "@/dto/PerfumeWornDTO";
import { deleteWear } from "@/services/perfume-worn-service";
import { showError, showSuccess } from "@/services/toasty-service";

export interface PerfumeCardProps {
  worn: PerfumeWornDTO;
  r2_api_address: string | undefined;
}

export default function PerfumeCard({
  worn,
  r2_api_address,
}: PerfumeCardProps) {
  if (!worn || !worn.perfume) return <div></div>;
  const perfume = worn.perfume;
  const avatar =
    perfume.perfumeName.split(" ").length > 1
      ? perfume.perfumeName
          .split(" ")
          .map((x) => x[0])
          .slice(0, 2)
          .join("")
      : perfume.perfumeName.slice(0, 2).toUpperCase();

  const handlePressStart = async (id: number) => {
    const result = await deleteWear(id);
    if (result.ok) showSuccess("Worn deleted");
    else showError(`Worn delete failed: ${result.error ?? "unknown error"}`);
    //TODO: revalidatepath
  };

  return (
    <form>
      <Card key={worn.id} className="min-w-96 perfume-card">
        <CardHeader>
          <a
            href={`/perfumes/${perfume.id}/`}
            className="flex items-center justify-between space-x-4"
          >
            <Avatar className="w-20 h-20 semi-bold">
              <AvatarImage
                className="object-cover"
                src={getImageUrl(perfume?.imageObjectKey, r2_api_address)}
              />
              <AvatarFallback>{avatar}</AvatarFallback>
            </Avatar>
            <div className="text-small leading-none text-default-600 ml-4">
              {perfume.house} - {perfume.perfumeName}
              <TooltipProvider>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <p className="mt-2 text-small tracking-tight text-default-400">
                      {`Worn on: ${worn.wornOn.toDateString()}`}
                    </p>
                  </TooltipTrigger>
                  <TooltipContent>
                    <div>
                      {worn.tags?.map((x) => (
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
            <Button
              color="danger"
              className="w-10"
              size="sm"
              onClick={() => {
                handlePressStart(worn.id);
              }}
            >
              X
            </Button>
          </a>
        </CardHeader>
      </Card>
    </form>
  );
}
