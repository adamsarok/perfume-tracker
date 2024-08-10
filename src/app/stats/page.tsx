import * as perfumeRepo from "@/db/perfume-repo";
import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import { Card, CardBody, CardFooter, CardHeader, chip, Chip, Divider } from "@nextui-org/react";
import { getContrastColor } from "../colors";

export const dynamic = 'force-dynamic'

export default async function StatsPage() {
    let totalMl = 0;
    let totalWornCount = 0;
    const perfumes = await perfumeRepo.getPerfumesWithTags();
    const perfumesWorn = await perfumeWornRepo.getAllPerfumesWithWearCount();

    interface TagStat {
        color: string;
        mls: number;
        wornCount: number;
    }

    interface TagStats {
        [key: string]: TagStat
    }

    let tagStats: TagStats = {};

    perfumes.map(p => {
        totalMl += p.ml;
        const perfumeWorn = perfumesWorn.find(x => x.perfume.id == p.id);
        if (perfumeWorn?.wornTimes) totalWornCount += perfumeWorn.wornTimes;

        p.tags.map(t => {
            if (!tagStats[t.tag.tag]) {
                tagStats[t.tag.tag] = {
                    color: t.tag.color,
                    mls: 0,
                    wornCount: 0
                }
            }
            tagStats[t.tag.tag].mls += p.ml;
            const perfumeWorn = perfumesWorn.find(x => x.perfume.id == p.id);
            if (perfumeWorn?.wornTimes) tagStats[t.tag.tag].wornCount += perfumeWorn.wornTimes;
        }
    )});

    console.log(tagStats);

    const maxMlInTags = Math.max(...Object.values(tagStats).map(tag => tag.mls));
    const maxWornInTags = Math.max(...Object.values(tagStats).map(tag => tag.wornCount));
    const minChipWidth = 25;

    //const ml = await perfumeRepo.getp();
    return <div>
        <Card className="max-w-[412px]">
        <CardHeader className="flex gap-3">
            <div className="flex flex-col">
            <p className="text-md">Total:</p>
            <p className="text-small text-default-500">{totalMl} mls</p>
            <p className="text-small text-default-500">{totalWornCount} wears</p>
            </div>
        </CardHeader>
        <Divider/>
        <CardBody>
            Mls:
            <div>
                {Object.entries(tagStats)
                    .sort(([, a], [, b]) => b.mls - a.mls)
                    .map(([tagName, tagInfo]: [string, TagStat]) => {
                        const relativeWidth = (tagInfo.mls / maxMlInTags) * 100;
                        const chipWidth = Math.max(relativeWidth, minChipWidth);
                        return (<div key={tagName}>
                            <Chip
                                style={{ backgroundColor: tagInfo.color, 
                                    color: getContrastColor(tagInfo.color), 
                                    //minWidth: '1000px',
                                    maxWidth: '100%',
                                    width: `${chipWidth}%` }}>
                                {tagName} - {tagInfo.mls} ml
                            </Chip>
                        </div>)
                })}
            </div>
            <Divider className="mt-4 mb-4"/>
            <div>Worn X times:</div>
            <div>
                {Object.entries(tagStats)
                    .sort(([, a], [, b]) => b.wornCount - a.wornCount)
                    .map(([tagName, tagInfo]: [string, TagStat]) => {
                        const relativeWidth = (tagInfo.wornCount / maxWornInTags) * 100;
                        const chipWidth = Math.max(relativeWidth, minChipWidth);
                        console.log(relativeWidth);
                        return (<div key={tagName}>
                            <Chip
                                style={{ backgroundColor: tagInfo.color, 
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
        <Divider/>
        <CardFooter>
           
        </CardFooter>
    </Card>
    </div>
}