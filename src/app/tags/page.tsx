import * as tagRepo from "@/db/tag-repo";
import TagTable from "@/components/tag-table";
import { Separator } from "@/components/ui/separator";

export const dynamic = 'force-dynamic'

export default async function StatsPage() {
    const tags = await tagRepo.getTags();
    tags.sort((a, b) => a.tag.localeCompare(b.tag));
    return <div>
      <Separator></Separator>
      <TagTable tags={tags}></TagTable>
    </div>
}