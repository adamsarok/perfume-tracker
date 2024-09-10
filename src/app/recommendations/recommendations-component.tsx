"use client";

import ReactMarkdown from 'react-markdown';
import { Button } from "@nextui-org/button";
import React, { useEffect } from "react";
import { useState } from "react";
import { Card, CardBody, CardHeader, Radio, RadioGroup } from '@nextui-org/react';
import ColorChip from '@/components/color-chip';
import { GetQuery, UserPreferences } from '@/services/recommendation-service';
import { toast } from 'react-toastify';

export const dynamic = 'force-dynamic'

interface RecommendationsPageProps {
  userPreferences: UserPreferences
}

export default function RecommendationsComponent({ userPreferences }: RecommendationsPageProps) {
  const [recommendations, setRecommendations] = useState<string | null>(null);
  //const tags = await tagRepo.getTags();
  //TODO: recommend perfumes already in collection, or already in coll 
  const getRecommendations = async () => {
    const query = GetQuery(userPreferences, wearOrBuy, perfumesOrNotes);
    console.log(query);
    if (query) {
      const res = await fetch(`/api/get-recommendations`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          query
        }),
      });
      console.log(res);
      const json = await res.json();
      if (res.ok) {
        setRecommendations(json.recommendations);
      } else {
        toast.error(`Failed to get download url: ${json.error}`);
      }
    } else {
      toast.error("Query generation failed, query is empty");
    }
  };

  const [wearOrBuy, setWearOrBuy] = useState<"wear" | "buy" | null>('wear');
  const [perfumesOrNotes, setPerfumesOrNotes] = useState<"perfumes" | "notes" | null>('perfumes');
  const [timeRange, setTimeRange] = useState<string | null>('last-3');

  useEffect(() => {
    
  }, [perfumesOrNotes]);

  return <div className="space-y-6">
    <RadioGroup 
      label="Wear or buy?"
      orientation="horizontal"
      onValueChange={(value) => setWearOrBuy(value === "wear" ? "wear" : "buy")}
      defaultValue='wear'
      >
      <Radio value="wear">What to wear (owned perfumes)</Radio>
      <Radio value="buy">What to buy (new perfumes)</Radio>
    </RadioGroup>
    <RadioGroup 
      label="Recommend based on notes or perfumes?"
      orientation="horizontal"
      onValueChange={(value) => setPerfumesOrNotes(value === "perfumes" ? "perfumes" : "notes")}
      defaultValue='perfumes'
      >
      <Radio value="perfumes">Recommend based on perfumes</Radio>
      <Radio value="notes">Recommend based on perfume notes</Radio>
    </RadioGroup>
    <Card>
      <CardBody>
        {perfumesOrNotes === 'perfumes' ? 
          'Based on the last 3 perfumes you wore:' : 'Based on the notes of the last 3 perfumes you wore:'}
          {perfumesOrNotes === 'perfumes' ?  
            userPreferences.last3perfumes.perfumes && userPreferences.last3perfumes.perfumes.map(p => 
              <div key={p.perfume.id}>
                {p.perfume.house} - {p.perfume.perfume}
              </div>) : 
            userPreferences.last3perfumes.tags && userPreferences.last3perfumes.tags.map(t => 
              <div key={t.id}>
                <ColorChip 
                    name={`${t.tag} - ${t.wornCount}`}
                    className='' 
                    key={t.tag}
                    color={t.color} 
                    onChipClick={null}  
                  />
              </div>)}
      </CardBody>
    </Card>
  
    <RadioGroup
      label="Time range"
      orientation='horizontal'
      onValueChange={setTimeRange}
      defaultValue='last-3'
    >
      <Radio value="last-3">Last 3 worn perfumes</Radio>
      <Radio value="all-time" disabled>All time (most worn perfumes or notes)</Radio>
    </RadioGroup>
    <Card>  
      <CardBody>
        <div >Recommendations generated with OpenAI's GPT-4</div>
      </CardBody>
    </Card>
    <Button onPress={async () => {
      await getRecommendations();
    }}>
      Show recommendations
    </Button>
    {recommendations && 
      <Card>
        <CardBody>
          <ReactMarkdown>{recommendations}</ReactMarkdown>
        </CardBody>
      </Card>
    }
  </div>
}