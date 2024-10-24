"use client";

import ReactMarkdown from "react-markdown";
import React from "react";
import { useState } from "react";
import ColorChip from "@/components/color-chip";
import { GetQuery, UserPreferences } from "@/services/recommendation-service";
import { toast } from "react-toastify";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Card, CardContent } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";

export const dynamic = "force-dynamic";

interface RecommendationsPageProps {
  userPreferences: UserPreferences;
}

export default function RecommendationsComponent({
  userPreferences,
}: RecommendationsPageProps) {
  const [recommendations, setRecommendations] = useState<string | null>(null);
  //const tags = await tagRepo.getTags();
  //TODO: recommend perfumes already in collection, or already in coll
  const getRecommendations = async () => {
    const query = GetQuery(userPreferences, wearOrBuy, perfumesOrNotes);
    console.log(query);
    if (query) {
      const res = await fetch(`/api/get-recommendations`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          query,
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

  const [wearOrBuy, setWearOrBuy] = useState<"wear" | "buy" | null>("wear");
  const [perfumesOrNotes, setPerfumesOrNotes] = useState<
    "perfumes" | "notes" | null
  >("perfumes");
  const [timeRange, setTimeRange] = useState<string | null>("last-3");
  //TODO: timerange filtering
  console.log(timeRange);

  return (
    <div className="space-y-6">
      <Label>Wear or buy?</Label>
      <RadioGroup
        orientation="horizontal"
        onValueChange={(value) =>
          setWearOrBuy(value === "wear" ? "wear" : "buy")
        }
        defaultValue="wear"
      >
        <div className="flex items-center space-x-2">
          <RadioGroupItem value="wear" id="wear" />
          <Label htmlFor="wear">What to wear (owned perfumes)</Label>
        </div>
        <div className="flex items-center space-x-2">
          <RadioGroupItem value="buy" id="buy" />
          <Label htmlFor="buy">What to buy (new perfumes)</Label>
        </div>
      </RadioGroup>
      <Label>Recommend based on notes or perfumes?</Label>
      <RadioGroup
        orientation="horizontal"
        onValueChange={(value) =>
          setPerfumesOrNotes(value === "perfumes" ? "perfumes" : "notes")
        }
        defaultValue="perfumes"
      >
        <div className="flex items-center space-x-2">
          <RadioGroupItem value="perfumes" id="perfumes" />
          <Label htmlFor="perfumes">Recommend based on perfumes</Label>
        </div>
        <div className="flex items-center space-x-2">
          <RadioGroupItem value="notes" id="notes" />
          <Label htmlFor="notes">Recommend based on perfume notes</Label>
        </div>
      </RadioGroup>
      <Card>
        <CardContent>
          {perfumesOrNotes === "perfumes"
            ? "Based on the last 3 perfumes you wore:"
            : "Based on the notes of the last 3 perfumes you wore:"}
          {perfumesOrNotes === "perfumes"
            ? userPreferences.last3perfumes.perfumes &&
              userPreferences.last3perfumes.perfumes.map((p) => (
                <div key={p.perfume.id}>
                  {p.perfume.house} - {p.perfume.perfume}
                </div>
              ))
            : userPreferences.last3perfumes.tags &&
              userPreferences.last3perfumes.tags.map((t) => (
                <div key={t.id}>
                  <ColorChip
                    name={`${t.tag} - ${t.wornCount}`}
                    className=""
                    key={t.tag}
                    color={t.color}
                    onChipClick={null}
                  />
                </div>
              ))}
        </CardContent>
      </Card>

      <Label>Time range</Label>
      <RadioGroup
        orientation="horizontal"
        onValueChange={setTimeRange}
        defaultValue="last-3"
      >
        <div className="flex items-center space-x-2">
          <RadioGroupItem value="last-3" id="last-3" />
          <Label htmlFor="last-3">Last 3 worn perfumes</Label>
        </div>
        <div className="flex items-center space-x-2">
          <RadioGroupItem value="all-time" id="all-time" />
          <Label htmlFor="all-time">
            All time (most worn perfumes or notes)
          </Label>
        </div>
      </RadioGroup>
      <Card>
        <CardContent>
          <div>Recommendations generated with OpenAI&apos;s GPT-4</div>
        </CardContent>
      </Card>
      <Button
        onClick={async () => {
          await getRecommendations();
        }}
      >
        Show recommendations
      </Button>
      {recommendations && (
        <Card>
          <CardContent>
            <ReactMarkdown>{recommendations}</ReactMarkdown>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
