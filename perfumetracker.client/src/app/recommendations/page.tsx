import { getUserPreferences } from "@/services/recommendation-service";
import RecommendationsComponent from "./recommendations-component";

export const dynamic = 'force-dynamic'

export default async function RecommendationsPage() {
    const userPreferences = await getUserPreferences();
    return <div>
      <RecommendationsComponent userPreferences={userPreferences}></RecommendationsComponent>
    </div>
}