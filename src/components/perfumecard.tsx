'use client'

import { Avatar, Link } from "@nextui-org/react";
import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import React from "react";
import { WornWithPerfume } from "@/db/perfume-worn-repo";
import ColorChip from "./color-chip";
import {Tooltip} from "@nextui-org/tooltip";
import { getImageUrl } from "./r2-image";

import {
  Card,
  // CardContent,
  // CardDescription,
  // CardFooter,
  CardHeader,
  //CardTitle,
} from "@/components/ui/card"
import { Button } from "./ui/button";

export interface PerfumeCardProps {
  worn: WornWithPerfume,
  r2_api_address: string | undefined
}

export default function PerfumeCard({ worn, r2_api_address }: PerfumeCardProps) {
  if (!worn || !worn.perfume) return (<div></div>);
  const perfume = worn.perfume;
  const avatar = perfume.perfume.split(" ").length > 1
    ? perfume.perfume.split(" ").map((x) => x[0]).slice(0, 2).join("")
    : perfume.perfume.slice(0, 2).toUpperCase();

  const handlePressStart = (id: number) => {
    perfumeWornRepo.deleteWear(id);
    window.location.reload();
  };

  return (
    <form>
      <Card key={worn.id} className="min-w-96 perfume-card">
        <CardHeader>
          <div className="flex items-center justify-between space-x-4 rounded-md border p-4">
          <Link href={`/perfumes/${perfume.id}/`}>
            <Avatar className="w-20 h-20 semi-bold" size="sm" src={getImageUrl(perfume?.imageObjectKey, r2_api_address)}
              name={avatar} />
            <div className="text-small leading-none text-default-600 ml-4">
              {perfume.house} - {perfume.perfume}
              <Tooltip content={
                <div>
                  {worn.tags?.map(x => (
                    <ColorChip key={x.id} className="mr-2" onChipClick={null} name={x.tag} color={x.color}></ColorChip>
                  ))}
                </div>
              }>
              <p className="mt-2 text-small tracking-tight text-default-400">
                {`Worn on: ${worn.wornOn.toDateString()}`}
              </p>
            </Tooltip>
            </div>
          </Link>
          <Button color="danger"
            className="w-10"
            size="sm"
            onClick={() => {
              handlePressStart(worn.id);
            }}
          >X</Button>
          </div>
        </CardHeader>
      </Card>
    </form>
  );
}