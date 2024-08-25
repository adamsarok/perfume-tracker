import * as perfumeWornRepo from "@/db/perfume-worn-repo";

export interface TagWithCount {
    id: number,
    tag: string,
    color: string,
    wornCount: number
}

// export interface Perfume {
//     house: string,
//     perfume: string,
//     wornCount: number
// }

export interface UserPreference {
    perfumes: perfumeWornRepo.PerfumeWornDTO[] | null, //not exactly correct as the worncount is total, not 3 days...
    tags: TagWithCount[] | null
}

export interface UserPreferences {
  last3perfumes: UserPreference
  allTime: UserPreference
}

function aggregatePerfumesTags(perfumes: perfumeWornRepo.PerfumeWornDTO[]) : UserPreference {
    let result: UserPreference = {
        perfumes: [],
        tags: []
    }

    //TODO: fix logical issue - worncount is calculated in repo and thus not filtered for 3 days

    let acc = perfumes.reduce((acc, perfume) => {
        perfume.tags.forEach(t => {
            if (!acc[t.id]) {
                acc[t.id] = {
                    id: t.id,
                    tag: t.tag,
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
    
    const perfumes = await perfumeWornRepo.getAllPerfumesWithWearCount();

    const past = new Date(0); //todo refactor
    const lastThreePerfumes = perfumes.sort((a, b) => {
        let dateA = a.lastWorn ? a.lastWorn : past;
        let dateB = b.lastWorn ? b.lastWorn : past;
        return dateB.getTime() - dateA.getTime();
    }).slice(0, 3);

    return {
        last3perfumes: aggregatePerfumesTags(lastThreePerfumes),
        allTime: aggregatePerfumesTags(perfumes)
    }
}