import * as perfumeRepo from "@/db/perfume-repo";
import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import { Card, CardBody, CardFooter, CardHeader, chip, Chip, Divider, Radio, RadioGroup, select } from "@nextui-org/react";
import { getContrastColor } from "../colors";
import React from "react";
import { Perfume, Tag } from "@prisma/client";
import StatsComponent from "./calendar-component";

export const dynamic = 'force-dynamic'

export default async function CalendarPage() {

    const perfumes = await perfumeRepo.getPerfumesWithTags();
    const perfumesWorn = await perfumeWornRepo.getWornWithPerfume();
    return (
        <div>
            <StatsComponent perfumes={perfumes} perfumesWorn={perfumesWorn}></StatsComponent>
        </div>
    )
}