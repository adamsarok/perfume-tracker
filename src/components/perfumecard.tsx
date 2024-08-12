'use client'

import { Card, CardHeader, CardBody, Avatar, divider, Button, Link } from "@nextui-org/react";
import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import React, { ReactNode } from "react";
import { WornWithPerfume } from "@/db/perfume-worn-repo";
import ColorChip from "./color-chip";
import {Tooltip} from "@nextui-org/tooltip";

export interface PerfumeCardProps {
  worn: WornWithPerfume
}

export default function PerfumeCard({ worn }: PerfumeCardProps) {
  if (!worn || !worn.perfume) return (<div></div>);
  const perfume = worn.perfume;
  const avatar = perfume.perfume.split(" ").length > 1
    ? perfume.perfume.split(" ").map((x) => x[0]).slice(0, 2).join("")
    : perfume.perfume.slice(0, 2).toUpperCase();

  const handlePressStart = (id: number) => {
    perfumeWornRepo.deleteWear(id);
  };

  return (
    <form>
      <Card key={worn.id} className="min-w-96">
        <CardHeader>
          <Link href={`/perfumes/${perfume.id}/`}>
            <Avatar className="semi-bold"
              name={avatar} />
            <div className="text-small leading-none text-default-600 ml-4">
              {perfume.house} - {perfume.perfume}
            </div>
            <Avatar className="semi-bold ml-4"
              name={perfume.rating.toString()}
              color="secondary" />
          </Link>
          <Button color="danger"
            size="sm" className="absolute right-5  max-w-4"
            onPress={() => {
              handlePressStart(worn.id);
            }}
          >X</Button>
        </CardHeader>
        <CardBody>
          <Tooltip content={
           <div>
            {worn.tags.map(x => (
              <ColorChip key={x.id} className="mr-2" onChipClick={null} name={x.tag} color={x.color}></ColorChip>
            ))}
            </div>
          }>
            <p className="text-small tracking-tight text-default-400">
              {`Worn on: ${worn.wornOn.toDateString()}`}
            </p>
          </Tooltip>
        </CardBody>
      </Card>
    </form>
  );
}