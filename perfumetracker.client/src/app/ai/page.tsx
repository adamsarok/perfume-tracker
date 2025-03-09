import { getTagStats } from "@/services/tag-service";
import AiComponent from "./ai-component"
import { getPerfumes } from "@/services/perfume-service";
import { PerfumeSelectDto } from "./perfume-select-columns";

export const dynamic = 'force-dynamic'


export default async function AiPage() {
    const tags = await getTagStats(); //TODO: these should be filtered and paginated
    const perfumes: PerfumeSelectDto[] = (await getPerfumes()).map(x => ({
      house: x.perfume.house,
      perfume: x.perfume.perfumeName,
      ml: x.perfume.ml,
      wornTimes: x.wornTimes
    }));
    return <div>
      <AiComponent tags={tags} perfumes={perfumes}></AiComponent>
    </div>
}