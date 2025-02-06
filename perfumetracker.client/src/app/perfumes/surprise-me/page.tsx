import PerfumeEditForm from "@/components/perfume-edit-form";
import { notFound } from "next/navigation";
import { R2_API_ADDRESS } from "@/services/conf";
import { getPerfumeSuggestion } from "@/services/perfume-suggested-service";
import { getPerfume } from "@/services/perfume-service";
import { TagDTO } from "@/dto/TagDTO";
import { getTags } from "@/services/tag-service";

export const dynamic = "force-dynamic";

export default async function SuprisePerfumePage() {
  const id = await getPerfumeSuggestion(20);
  if (!id) return notFound();
  console.log("id", id);
  const perfume = await getPerfume(id);
  if (!perfume) return notFound();
  const allTags = await getTags();
  const tags: TagDTO[] = [];
  perfume?.tags.map((x) => {
    tags.push({
      id: x.id,
      tagName: x.tagName,
      color: x.color,
    });
  });
  return (
    <PerfumeEditForm
      perfumeWithWornStats={perfume}
      perfumesTags={tags}
      allTags={allTags}
      r2_api_address={R2_API_ADDRESS}
    ></PerfumeEditForm>
  );
}
