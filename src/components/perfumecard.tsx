'use client'

import { Card, CardHeader, CardBody, Avatar, divider, Button, Link } from "@nextui-org/react";
import { Perfume } from "@prisma/client";
import * as actions from "@/app/actions";
import React from "react";

export interface PerfumeCardProps {
    id: number,
    perfume: Perfume,
    wornOn: Date | null,
    avatar: string | null
}

export default function PerfumeCard({id, perfume, wornOn, avatar}: PerfumeCardProps) { 
    if (!perfume || !perfume.perfume) return (<div></div>);
    if (!avatar) avatar = perfume.perfume.split(" ").length > 1 
        ? perfume.perfume.split(" ").map((x) => x[0]).slice(0,2).join("") 
        : perfume.perfume.slice(0,2).toUpperCase();

    //why doesn't the onPress work here?
    return (
      <form>
        <Link href={`/perfumes/${perfume.id}/`}> 
        <Card key={id} className="min-w-96">
                  <CardHeader>
                    <Avatar className="semi-bold"
                      name={avatar} />
                
                    <div className="text-small leading-none text-default-600 ml-4">
                      {perfume.house} - {perfume.perfume}
                    </div>
                  </CardHeader>
                  <CardBody>
                    <p className="text-small tracking-tight text-default-400">
                      {"Last worn: " + (wornOn ? wornOn.toDateString() : "Never")}
                    </p>
                  </CardBody>
          </Card>
        </Link>
      </form>
    );
  }