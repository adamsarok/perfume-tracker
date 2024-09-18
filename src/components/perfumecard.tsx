'use client'

import { Card, CardHeader, CardBody, Avatar, divider, Button, Link } from "@nextui-org/react";
import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import React, { ReactNode } from "react";
import { WornWithPerfume } from "@/db/perfume-worn-repo";
import ColorChip from "./color-chip";
import {Tooltip} from "@nextui-org/tooltip";
import { getImageUrl } from "./r2-image";

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
      <Card key={worn.id} className="min-w-96">
        <CardHeader>
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
            size="sm" className="absolute right-5  max-w-4"
            onPress={() => {
              handlePressStart(worn.id);
            }}
          >X</Button>
        </CardHeader>
      </Card>
    </form>
  );
}