'use client'

import { Card, CardHeader, CardBody, Avatar, divider, Button, Link } from "@nextui-org/react";
import { Perfume } from "@prisma/client";
import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import React, { ReactNode } from "react";

export interface PerfumeCardProps {
  wornId: number | undefined,
  wornOn: Date | undefined,
  wornCount: number | undefined
  perfume: Perfume,
}

export default function PerfumeCard({ wornId, wornOn, wornCount, perfume }: PerfumeCardProps) {
  if (!perfume || !perfume.perfume) return (<div></div>);
  const avatar = perfume.perfume.split(" ").length > 1
    ? perfume.perfume.split(" ").map((x) => x[0]).slice(0, 2).join("")
    : perfume.perfume.slice(0, 2).toUpperCase();

  const handlePressStart = (id: number) => {
    perfumeWornRepo.deleteWear(id);
  };

  return (
    <form>
      <Card key={wornId ? wornId : perfume.id} className="min-w-96">
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
          {wornId ?
            <Button color="danger"
              size="sm" className="absolute right-5  max-w-4"
              onPress={() => {
                handlePressStart(wornId);
              }}
            >X</Button> : ""}
        </CardHeader>
        <CardBody>
          <p className="text-small tracking-tight text-default-400">
            {getWornStr(wornOn, wornCount)}
          </p>
        </CardBody>
      </Card>
    </form>
  );
}

function getWornStr(wornOn: Date | undefined, wornCount: number | undefined): string {
  if (wornOn && wornCount) return `Worn ${wornCount} times, last: ${wornOn.toDateString()}`;
  if (wornOn) return `Worn on: ${wornOn.toDateString()}`;
  return 'Never worn';
}