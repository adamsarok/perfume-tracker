"use client";

import { getActiveMissions } from "@/services/mission-service";
import ProgressComponent from "./progress-component";
import { UserMissionDto } from "@/dto/MissionDto";
import { useEffect, useState } from "react";

export const dynamic = 'force-dynamic'

export default function ProgressPage() {
    const [missions, setMissions] = useState<UserMissionDto[]>([]);
    useEffect(() => {
        const fetchData = async () => {
            const activeMissions = await getActiveMissions();
            setMissions(activeMissions);
        }
        fetchData();
    }, []);
    return <div>
      <ProgressComponent Missions={missions}></ProgressComponent>
    </div>
}