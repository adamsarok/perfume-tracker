import TagTable from "@/components/tag-table";
import { Separator } from "@/components/ui/separator";
import { getTags } from "@/services/tag-service";

export const dynamic = 'force-dynamic'

export default async function StatsPage() {
    const tags = await getTags();
    tags.sort((a, b) => a.tagName.localeCompare(b.tagName));
    return <div>
      <Separator></Separator>
      <TagTable tags={tags}></TagTable>
    </div>
}