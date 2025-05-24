"use client";

import React from "react";
import ColorChip from "./color-chip";
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
  readonly worn: PerfumeWornDTO;
}

export default function PerfumeCard({
  worn
}: PerfumeCardProps) {
  if (!worn) return <div></div>;
  const avatar =
    worn.perfumeName.split(" ").length > 1
      ? worn.perfumeName
          .split(" ")
          .map((x) => x[0])
          .slice(0, 2)
          .join("")
      : worn.perfumeName.slice(0, 2).toUpperCase();

  const handlePressStart = async (id: string) => {
    const result = await deleteWear(id);
    if (result.ok) showSuccess("Worn deleted");
    else showError(`Worn delete failed: ${result.error ?? "unknown error"}`);
    //TODO: revalidatepath
  };

  return (
    <form>
      <Card key={worn.id} className="w-full perfume-card">
        <CardHeader>
          <a
            href={`/perfumes/${worn.perfumeId}/`}
            className="flex items-center justify-between gap-4"
          >
            <div className="flex items-center space-x-4">
              <Avatar className="w-16 h-16 sm:w-20 sm:h-20 semi-bold">
                <AvatarImage
                  className="object-cover"
                  src={worn.perfumeImageUrl}
                />
                <AvatarFallback>{avatar}</AvatarFallback>
              </Avatar>
              <div className="text-small leading-none text-default-600">
                <p className="whitespace-normal text-small">{worn.perfumeHouse} - {worn.perfumeName}</p>
                <TooltipProvider>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <p className="mt-2 text-small tracking-tight text-default-400">
                        {`Worn on: ${worn.wornOn.toDateString()}`}
                      </p>
                    </TooltipTrigger>
                    <TooltipContent>
                      <div>
                        {worn.perfumeTags?.map((x) => (
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
            <Button
              color="danger"
              className="w-9 h-8 p-0 flex-shrink-0"
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
