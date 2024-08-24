"use client";

import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import { getPerfumeSimilar } from "@/services/recommendations";
import { Button } from "@nextui-org/button";
import { useState } from "react";

export const dynamic = 'force-dynamic'

interface RecommendationsPageProps {
  perfumes: perfumeWornRepo.PerfumeWornDTO[];
}

export default function RecommendationsComponent({perfumes} : RecommendationsPageProps) {
    //const perfumes = await perfumeWornRepo.getAllPerfumesWithWearCount(); //TODO should be limited
    const [recommendations, setRecommendations] = useState<string | null>(null);
    //const tags = await tagRepo.getTags();
    //TODO: recommend perfumes already in collection, or already in coll 
    return <div>
      <div>
        Based on the last 3 perfumes you wore:
        {perfumes.slice(0, 3).map(p => `${p.perfume.house} - ${p.perfume.perfume}`).join(', ')}
      </div>
      <Button onPress={async () => {
        const lastThreePerfumes = perfumes.slice(0, 3).map(p => `${p.perfume.house} - ${p.perfume.perfume}`);
        const recommendations = await getPerfumeSimilar(lastThreePerfumes);
        //setRecommendations(recommendations) TODO
      }}>
        Show recommendations
      </Button>
      {recommendations && <div>{recommendations}</div>}
    </div>
}