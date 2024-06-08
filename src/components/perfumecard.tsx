'use client'

import { Card, CardHeader, CardBody, Avatar, divider, Button, Link } from "@nextui-org/react";
import { Perfume, PerfumeWorn } from "@prisma/client";
import * as actions from "@/app/actions";
import React, { ReactNode } from "react";

// export const dynamic = 'force-dynamic'

export interface PerfumeCardProps {
    worn: PerfumeWorn | null,
    perfume: Perfume,
    showDelete: boolean
}

export default function PerfumeCard({worn, perfume, showDelete}: PerfumeCardProps) { 
    if (!perfume || !perfume.perfume) return (<div></div>);
    const avatar = perfume.perfume.split(" ").length > 1 
        ? perfume.perfume.split(" ").map((x) => x[0]).slice(0,2).join("") 
        : perfume.perfume.slice(0,2).toUpperCase();

    const handlePressStart = (id: number) => {
      actions.DeleteWear(id);
    };

    return (
      <form>
        {/* <Link href={`/perfumes/${perfume.id}/`}>  */}
        <Card key={worn ? worn.id : perfume.id} className="min-w-96">
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
                    {showDelete ? 
                    <Button color="danger" 
                          size="sm" className="absolute right-5  max-w-4"
                          onPress={() => {
                            if (worn) handlePressStart(worn.id);
                          }}
                        >X</Button> : ""} 
                  </CardHeader>
                  <CardBody>
                  
                      <p className="text-small tracking-tight text-default-400">
                        {"Last worn: " + (worn?.wornOn ? worn.wornOn.toDateString() : "Never")}
                      </p>

                  </CardBody>
          </Card>
        {/* </Link> */}
      </form>
    );
  }