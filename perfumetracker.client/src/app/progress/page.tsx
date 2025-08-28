"use client";

import { getActiveMissions } from "@/services/mission-service";
import ProgressComponent from "./progress-component";
import { UserMissionDto } from "@/dto/MissionDto";
import { useEffect, useState } from "react";
import { showError } from "@/services/toasty-service";

export const dynamic = 'force-dynamic'

export default function ProgressPage() {
    const [missions, setMissions] = useState<UserMissionDto[]>([]);
    useEffect(() => {
        const fetchData = async () => {
            const result = await getActiveMissions();
            if (result.error || !result.data) {
                setMissions([]);
                showError("Could not load missions", result.error ?? "unknown error");
                return;
            }
            setMissions(result.data);
        }
        fetchData();
    }, []);
    return <div>
      <ProgressComponent Missions={missions}></ProgressComponent>
    </div>
}