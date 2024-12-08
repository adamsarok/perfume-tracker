import React from "react";
import StatsComponent from "./stats-component";

export const dynamic = 'force-dynamic'

export default async function StatsPage() {
    return (
        <div>
            <StatsComponent></StatsComponent>
        </div>
    )
}