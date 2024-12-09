import { getPerfumes } from "./perfume-service";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";

export interface TagWithCount {
    id: number,
    tag: string,
    color: string,
    wornCount: number
}
export interface UserPreference {
    perfumes: PerfumeWithWornStatsDTO[] | null, //not exactly correct as the worncount is total, not 3 days...
    tags: TagWithCount[] | null
}

export interface UserPreferences {
  last3perfumes: UserPreference
  allTime: UserPreference
}

export function GetQuery(userPreferences: UserPreferences, buyOrWear: "buy" | "wear" | null, basedOnNotes: "notes" | "perfumes" | null) : string {
    if (!userPreferences || !buyOrWear || !basedOnNotes) return '';
    let query;
    if (basedOnNotes === "perfumes") {
        if (!userPreferences.last3perfumes) return '';
        const last3perfumes = userPreferences.last3perfumes.perfumes?.map(p => `${p.perfume.house} - ${p.perfume.perfumeName}`).join(', ') ?? '';
        query = `Based on these past choices: ${last3perfumes}, suggest 3 perfumes.`;
    } else {
        if (!userPreferences.last3perfumes.tags) return '';
        const last3perfumesTags = userPreferences.last3perfumes.tags?.map(t => t.tag).join(', ') ?? '';
        query = `Based on these perfume notes: ${last3perfumesTags}, suggest 3 perfumes.`;
    }

    //we use less tokens if we group perfumes by house eg
    const ownedPerfumes = userPreferences.allTime.perfumes?.reduce((acc, p) => {
        const house = p.perfume.house;
        if (!acc[house]) {
            acc[house] = [];
        }
        acc[house].push(p.perfume.perfumeName);
        return acc;
    }, {} as Record<string, string[]>);   
    const formattedOwnedPerfumes = ownedPerfumes ? 
        Object.entries(ownedPerfumes).map(([house, perfumes]) => `${house} { ${perfumes.join(', ')} }`).join(', ') 
        : '';
    if (buyOrWear === "buy") query += ` Suggest perfumes not in this list: ${formattedOwnedPerfumes}`;  
    else query += ` Suggest perfumes only from this list: ${formattedOwnedPerfumes}`;
    return query;
}

function aggregatePerfumesTags(perfumes: PerfumeWithWornStatsDTO[]) : UserPreference {
    const result: UserPreference = {
        perfumes: [],
        tags: []
    }

    //TODO: fix logical issue - worncount is calculated in repo and thus not filtered for 3 days

    const acc = perfumes.reduce((acc, perfume) => {
        perfume.tags.forEach(t => {
            if (!acc[t.id]) {
                acc[t.id] = {
                    id: t.id,
                    tag: t.tagName,
                    color: t.color,
                    wornCount: 0
                };
            }
            acc[t.id].wornCount += perfume.wornTimes ?? 0;
        });
        return acc;
    }, {} as Record<string, TagWithCount>);

    result.tags = Object.values(acc);
    result.perfumes = perfumes;

    return result;
}

export default async function GetUserPreferences() : Promise<UserPreferences> {
    
    //TODO: filter on server side!
    const perfumes = (await getPerfumes())
        .filter(x => x.perfume.ml > 0 && x.perfume.rating >= 8);

    const past = new Date(0); //todo refactor
    const lastThreePerfumes = perfumes.sort((a, b) => {
        const dateA = a.lastWorn ? a.lastWorn : past;
        const dateB = b.lastWorn ? b.lastWorn : past;
        return dateB.getTime() - dateA.getTime();
    }).slice(0, 3);

    return {
        last3perfumes: aggregatePerfumesTags(lastThreePerfumes),
        allTime: aggregatePerfumesTags(perfumes)
    }
}