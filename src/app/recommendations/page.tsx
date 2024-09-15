import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import RecommendationsComponent from "./recommendations-component";
import GetUserPreferences from "@/services/recommendation-service";

export const dynamic = 'force-dynamic'

export default async function RecommendationsPage() {
    const userPreferences = await GetUserPreferences();
    return <div>
      <RecommendationsComponent userPreferences={userPreferences}></RecommendationsComponent>
    </div>
}