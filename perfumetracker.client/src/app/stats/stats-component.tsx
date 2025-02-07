'use client';

import React, { useEffect, useState } from "react";
import { getContrastColor } from "@/app/colors";
import { Card, CardContent, CardFooter, CardHeader } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { getPerfumeStats } from "@/services/perfume-service";
import { getTagStats } from "@/services/tag-service";
import { TagStatDTO } from "@/dto/TagStatDTO";
import { PerfumeStatsDTO } from "@/dto/PerfumeStatsDTO";

export const dynamic = 'force-dynamic'

type TagStats = Record<string, TagStatDTO>;

export default function StatsComponent() {
    const [tagStats, setTagStats] = useState<TagStats>();
    const [perfumeStats, setPerfumeStats] = useState<PerfumeStatsDTO>();
    useEffect(() => {
        async function fetchData() {
          try {
            const perfumeStats = await getPerfumeStats();
            const tagStats = await getTagStats();
            const transformedTagStats = tagStats.reduce<TagStats>((acc, tag) => {
                acc[tag.tagName] = tag;
                return acc;
            }, {});
            setPerfumeStats(perfumeStats);
            setTagStats(transformedTagStats);
            setMaxMlInTags(Math.max(...Object.values(tagStats).map(tag => tag.ml)));
            setMaxWornInTags(Math.max(...Object.values(tagStats).map(tag => tag.wornTimes)));
            console.log(tagStats);
          } catch (error) {
            //TODO
            console.error(error);
          }
        }
        fetchData();
      }, []);
      


    const minChipWidth = 25;

    const [maxMlInTags, setMaxMlInTags] = useState(0);
    const [maxWornInTags, setMaxWornInTags] = useState(0);

    return <div>
        <Card className="max-w-[412px]">
            <CardHeader className="flex gap-3">
                <div className="flex flex-col">
                    <p className="text-md">Total:</p>
                    <p className="text-small text-default-500">{perfumeStats?.totalMls} mls</p>
                    <p className="text-small text-default-500">{perfumeStats?.totalWears} wears</p>
                    <p className="text-small text-default-500">{perfumeStats?.perfumesTested} perfumes tested</p>
                </div>
            </CardHeader>
            <Separator />
            <CardContent>
                Mls:
                <div>
                    {tagStats && Object.entries(tagStats)
                        .sort(([, a], [, b]) => b.ml - a.ml)
                        .map(([tagName, tagInfo]: [string, TagStatDTO]) => {
                            const relativeWidth = (tagInfo.ml / maxMlInTags) * 100;
                            const chipWidth = Math.max(relativeWidth, minChipWidth);
                            return (<div key={tagName}>
                                <Badge
                                    style={{
                                        background: `${tagInfo.color}`,
                                        color: getContrastColor(tagInfo.color),
                                        //minWidth: '1000px',
                                        maxWidth: '100%',
                                        width: `${chipWidth}%`
                                    }}>
                                    {tagName} - {tagInfo.ml} ml
                                </Badge>
                            </div>)
                        })}
                </div>
                <Separator className="mt-4 mb-4" />
                <div>Worn X times:</div>
                <div>
                    {tagStats && Object.entries(tagStats)
                        .sort(([, a], [, b]) => b.wornTimes - a.wornTimes)
                        .map(([tagName, tagInfo]: [string, TagStatDTO]) => {
                            const relativeWidth = (tagInfo.wornTimes / maxWornInTags) * 100;
                            const chipWidth = Math.max(relativeWidth, minChipWidth);
                            return (<div key={tagName}>
                                <Badge
                                    style={{
                                        backgroundColor: tagInfo.color,
                                        color: getContrastColor(tagInfo.color),
                                        maxWidth: '100%',
                                        width: `${chipWidth}%`
                                    }}>
                                    {tagName} - {tagInfo.wornTimes} times
                                </Badge>
                            </div>);
                        })}
                </div>
            </CardContent>
            <Separator />
            <CardFooter>

            </CardFooter>
        </Card>
    </div>
}