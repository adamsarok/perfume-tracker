"use client";

import ReactMarkdown from "react-markdown";
import React, { useEffect } from "react";
import { useState } from "react";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { showError } from "@/services/toasty-service";
import { TagSelectTable } from "./tag-select-table";
import { TagStatDTO } from "@/dto/TagStatDTO";
import { PerfumeSelectTable } from "./perfume-select-table";
import { PerfumeSelectDto } from "./perfume-select-columns";
import { Input } from "@/components/ui/input";
import getAiRecommendations from "@/services/ai-service";
import { Textarea } from "@/components/ui/textarea";
import { getUserProfile, UserProfile } from "@/services/user-profiles-service";
import { getTagStats } from "@/services/tag-service";
import { getPerfumes } from "@/services/perfume-service";
import { useAuth } from "@/hooks/use-auth";

export const dynamic = "force-dynamic";


export default function AiComponent() {
  const auth = useAuth();
  const [tags, setTags] = useState<TagStatDTO[]>([]);
  const [perfumes, setPerfumes] = useState<PerfumeSelectDto[]>([]);
  const [userProfile, setUserProfile] = useState<UserProfile | null>(null);
  useEffect(() => {
    const load = async () => {
      setTags(await getTagStats()); //TODO: these should be filtered and paginated
      setPerfumes((await getPerfumes()).map(x => ({
        house: x.perfume.house,
        perfume: x.perfume.perfumeName,
        ml: x.perfume.ml,
        wornTimes: x.wornTimes
      })));
      setUserProfile(await getUserProfile());
      if (userProfile?.showFemalePerfumes) genderFilter.push('female');
      if (userProfile?.showMalePerfumes) genderFilter.push('male');
      if (userProfile?.showUnisexPerfumes) genderFilter.push('unisex');
    }
    load();
  }, []);

  const [recommendations, setRecommendations] = useState<string | null>(null);
  const [selectedPerfumes, setSelectedPerfumes] = useState<PerfumeSelectDto[]>(
    []
  );
  const [selectedTags, setSelectedTags] = useState<TagStatDTO[]>([]);
  const [freeText, setFreeText] = useState<string>("");
  const perfumesCountSuggest = 3; //TODO
  const genderFilter: string[] = [];
  const [query, setQuery] = useState<string>('');


  const getQuery = () => {
    let query = `Suggest ${perfumesCountSuggest} perfumes `;
    if (perfumes.length > 0) {
      query += `similar to these perfumes: ${selectedPerfumes
        .map((x) => x.house + " - " + x.perfume)
        .join(",")} `;
    }
    if (selectedTags.length > 0) {
      query += `with these perfume notes: ${selectedTags
        .map((x) => x.tagName)
        .join(",")} `;
    }
    if (freeText.length > 0) {
      query += freeText;
    }
    if (genderFilter.length != 0 && genderFilter.length != 3) {
      query += `only ${genderFilter.join(' or ')}`;
    }
    return query;
  };

  const getRecommendations = async () => {
    try {
      if (auth.guardAction()) return;
      //TODO fill query
      const queryLocal = getQuery();
      setQuery(queryLocal);
      if (queryLocal) {
        const res = await getAiRecommendations(queryLocal);
        if (res) {
          setRecommendations(res);
        } else {
          showError("AI response is empty :(");
        }
      } else {
        showError("Query generation failed, query is empty");
      }
    } catch (error) {
      showError("Failed to fetch recommendations", error);
    }
  };

  return (
    <div className="space-y-6">
      <details className="mr-1 px-2.5 pb-1">
        <summary>Similar to these perfumes...</summary>
        <PerfumeSelectTable
          perfumes={perfumes}
          onSelectionChange={setSelectedPerfumes}
        ></PerfumeSelectTable>
      </details>
      <details className="mr-1 px-2.5 pb-1">
        <summary>With these notes...</summary>
        <TagSelectTable
          tags={tags}
          onSelectionChange={setSelectedTags}
        ></TagSelectTable>
      </details>
      <details className="mr-1 px-2.5 pb-1">
        <summary>Free text...</summary>
        <Input
          placeholder=""
          value={freeText}
          onChange={(e) => setFreeText(e.target.value)}
        />
      </details>
      <Button
        onClick={async () => {
          if (
            selectedPerfumes.length > 0 ||
            selectedTags.length > 0 ||
            freeText.length > 0
          ) {
            await getRecommendations();
          } else {
            showError("Please select perfumes, tags, or a free text query!");
          }
        }}
      >
        Show recommendations
      </Button>
      <Textarea readOnly value={query} />
      {recommendations && (
        <Card>
          <CardContent>
            <ReactMarkdown>{recommendations}</ReactMarkdown>
          </CardContent>
        </Card>
      )}
      <Card>
        <CardContent>
          <div>Recommendations generated with OpenAI&apos;s GPT-4</div>
        </CardContent>
      </Card>
    </div>
  );
}
