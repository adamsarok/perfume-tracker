"use client";

import ReactMarkdown from 'react-markdown';
import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import { getPerfumeSimilar } from "@/services/recommendations";
import { Button } from "@nextui-org/button";
import React from "react";
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
    const getRecommendations = async (pastPerfumes: string) => {
      if (pastPerfumes && pastPerfumes.length > 0) {
          const res = await fetch(`/api/get-recommendations`, {
              method: 'PUT',
              headers: {
                'Content-Type': 'application/json',
              },
              body: JSON.stringify({
                pastPerfumes
            }),
          });
          console.log(res);
          const json = await res.json();
          if (res.ok) {
              //setRecommendations(json.recommendations.split('\n'));
              setRecommendations(json.recommendations);
          } else {
              console.error(`Failed to get download url: ${json.error}`);
          }
      }
  };

  const past = new Date(0); //todo refactor
  const lastThreePerfumes =  perfumes.sort((a, b) => {
    let dateA = a.lastWorn ? a.lastWorn : past;
    let dateB = b.lastWorn ? b.lastWorn : past;
    return dateB.getTime() - dateA.getTime();
  }).slice(0,3).map(p => `${p.perfume.house} - ${p.perfume.perfume}`);

    return <div>
      <div>
        Based on the last 3 perfumes you wore:
        {lastThreePerfumes.map(p => <div>{p}</div>)}
      </div>
      <Button onPress={async () => {
        await getRecommendations(lastThreePerfumes.join(', '));
      }}>
        Show recommendations
      </Button>
      {recommendations && <ReactMarkdown>{recommendations}</ReactMarkdown>}
    </div>
}