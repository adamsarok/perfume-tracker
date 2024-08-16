import * as perfumeRepo from "@/db/perfume-repo";
import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import { Card, CardBody, CardFooter, CardHeader, chip, Chip, Divider, Radio, RadioGroup, select } from "@nextui-org/react";
import { getContrastColor } from "../colors";
import React from "react";
import { Perfume, Tag } from "@prisma/client";
import StatsComponent from "./stats-component";

export const dynamic = 'force-dynamic'

export default async function StatsPage() {
    const perfumes = await perfumeRepo.getPerfumesWithTags();
    const perfumesWorn = await perfumeWornRepo.getAllPerfumesWithWearCount();
    return (
        <div>
            <StatsComponent perfumes={perfumes} perfumesWorn={perfumesWorn}></StatsComponent>
        </div>
    )
}