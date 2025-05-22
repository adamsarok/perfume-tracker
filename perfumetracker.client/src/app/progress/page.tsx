import { generateMissions, getActiveMissions } from "@/services/mission-service";
import ProgressComponent from "./progress-component";

export const dynamic = 'force-dynamic'

export default async function ProgressPage() {
    await generateMissions();
    const missions = await getActiveMissions();
    return <div>
      <ProgressComponent Missions={missions}></ProgressComponent>
    </div>
}