'use client';

import * as perfumeRepo from "@/db/perfume-repo";
import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import { Card, CardBody, CardFooter, CardHeader, Chip, Divider } from "@nextui-org/react";
import React, { useEffect, useState } from "react";
import { getContrastColor } from "@/app/colors";

export const dynamic = 'force-dynamic'

export interface StatsPageProps {
    perfumes: perfumeRepo.PerfumeWithTagDTO[],
    perfumesWorn: perfumeWornRepo.PerfumeWornDTO[]
}

export default function StatsComponent({ perfumes, perfumesWorn }: StatsPageProps) {
    interface TagStat {
        color: string;
        mls: number;
        wornCount: number;
    }

    interface TagStats {
        [key: string]: TagStat
    }

    const [view, setView] = React.useState('tags');
    const [tagStats, setTagStats] = useState<TagStats>({});
    const [totalMl, setTotalMl] = useState(0);
    const [tryCount, setTryCount] = useState(0);
    const [totalWornCount, setTotalWornCount] = useState(0);
    const [maxMlInTags, setMaxMlInTags] = useState(0);
    const [maxWornInTags, setMaxWornInTags] = useState(0);

    useEffect(() => {
        let stats: TagStats = {};
        let ml = 0;
        let worncount = 0;
        let tries = 0;

        const generateGradient = (colors: string[]): string => {
            return `linear-gradient(90deg, ${colors.join(", ")})`;
        };

        perfumes.forEach(p => {
            tries++;
            if (p.perfume.rating >= 8) {
                ml += p.perfume.ml;
                const perfumeWorn = perfumesWorn.find(x => x.perfume.id === p.perfume.id);
                if (perfumeWorn?.wornTimes) worncount += perfumeWorn.wornTimes;
                //if (view === 'tags') {
                    p.tags.forEach(t => {
                        if (!stats[t.tag]) {
                            stats[t.tag] = {
                                color: t.color,
                                mls: 0,
                                wornCount: 0
                            };
                        }
                        stats[t.tag].mls += p.perfume.ml;
                        if (perfumeWorn?.wornTimes) stats[t.tag].wornCount += perfumeWorn.wornTimes;
                    });
                // } else { //the colors look bad... for now lets skip this
                //     let tagCombo = p.tags.map(tag => tag.tag).join(" ");
                //     let tagColors = p.tags.map(tag => tag.color);
                //     console.log(tagCombo);
                //     console.log(tagColors);
                //     if (!stats[tagCombo]) {
                //         stats[tagCombo] = {
                //             color: generateGradient(tagColors),
                //             mls: 0,
                //             wornCount: 0
                //         };
                //     }
                //     stats[tagCombo].mls += p.perfume.ml;
                //     if (perfumeWorn?.wornTimes) stats[tagCombo].wornCount += perfumeWorn.wornTimes;
                // }
            }
        });

        setTagStats(stats);
        setTotalMl(ml);
        setTryCount(tries);
        setTotalWornCount(worncount);
        setMaxMlInTags(Math.max(...Object.values(stats).map(tag => tag.mls)));
        setMaxWornInTags(Math.max(...Object.values(stats).map(tag => tag.wornCount)));

    }, [perfumes, perfumesWorn, view]);

    const minChipWidth = 25;

    return <div>
        <Card className="max-w-[412px]">
            <CardHeader className="flex gap-3">
                <div className="flex flex-col">
                    <p className="text-md">Total:</p>
                    <p className="text-small text-default-500">{totalMl} mls</p>
                    <p className="text-small text-default-500">{totalWornCount} wears</p>
                    <p className="text-small text-default-500">{tryCount} perfumes tried</p>
                </div>
            </CardHeader>
            <Divider />
            <CardBody>
                Mls:
                <div>
                    {Object.entries(tagStats)
                        .sort(([, a], [, b]) => b.mls - a.mls)
                        .map(([tagName, tagInfo]: [string, TagStat]) => {
                            const relativeWidth = (tagInfo.mls / maxMlInTags) * 100;
                            const chipWidth = Math.max(relativeWidth, minChipWidth);
                            console.log(tagInfo.color);
                            return (<div key={tagName}>
                                <Chip
                                    style={{
                                        background: `${tagInfo.color}`,
                                        color: getContrastColor(tagInfo.color),
                                        //minWidth: '1000px',
                                        maxWidth: '100%',
                                        width: `${chipWidth}%`
                                    }}>
                                    {tagName} - {tagInfo.mls} ml
                                </Chip>
                            </div>)
                        })}
                </div>
                <Divider className="mt-4 mb-4" />
                <div>Worn X times:</div>
                <div>
                    {Object.entries(tagStats)
                        .sort(([, a], [, b]) => b.wornCount - a.wornCount)
                        .map(([tagName, tagInfo]: [string, TagStat]) => {
                            const relativeWidth = (tagInfo.wornCount / maxWornInTags) * 100;
                            const chipWidth = Math.max(relativeWidth, minChipWidth);
                            return (<div key={tagName}>
                                <Chip
                                    style={{
                                        backgroundColor: tagInfo.color,
                                        color: getContrastColor(tagInfo.color),
                                        maxWidth: '100%',
                                        width: `${chipWidth}%`
                                    }}>
                                    {tagName} - {tagInfo.wornCount} times
                                </Chip>
                            </div>);
                        })}
                </div>
            </CardBody>
            <Divider />
            <CardFooter>

            </CardFooter>
        </Card>
    </div>
}