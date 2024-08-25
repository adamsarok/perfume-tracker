import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import RecommendationsComponent from "./recommendations-component";
import GetUserPreferences from "@/services/recommendation-service";

export const dynamic = 'force-dynamic'

export default async function RecommendationsPage() {
    // const perfumes = await perfumeWornRepo.getAllPerfumesWithWearCount(); //TODO should be limited
    const userPreferences = await GetUserPreferences();
    console.log(userPreferences);
    return <div>
      <RecommendationsComponent userPreferences={userPreferences}></RecommendationsComponent>
    </div>
}