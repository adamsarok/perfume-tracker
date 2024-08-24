import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import RecommendationsComponent from "./recommendations-component";

export const dynamic = 'force-dynamic'

export default async function RecommendationsPage() {
    const perfumes = await perfumeWornRepo.getAllPerfumesWithWearCount(); //TODO should be limited
    return <div>
      <RecommendationsComponent perfumes={perfumes}></RecommendationsComponent>
    </div>
}