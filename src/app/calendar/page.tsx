import * as perfumeRepo from "@/db/perfume-repo";
import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import React from "react";
import StatsComponent from "./calendar-component";
import { getTags } from "@/db/tag-repo";

export const dynamic = 'force-dynamic'

export default async function CalendarPage() {

    const d = new Date();
    d.setDate(d.getDate() - 7);
    const perfumes = await perfumeRepo.getPerfumesWithTags();
    const perfumesWorn = await perfumeWornRepo.getWornWithPerfume(d);
    const tags = await getTags();
    return (
        <div>
            <StatsComponent perfumes={perfumes} perfumesWorn={perfumesWorn} allTags={tags}></StatsComponent>
        </div>
    )
}