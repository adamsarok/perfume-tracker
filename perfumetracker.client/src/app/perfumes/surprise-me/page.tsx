import PerfumeEditForm from "@/components/perfume-edit-form";
import { notFound } from "next/navigation";
import { Tag } from "@prisma/client";
import * as perfumeRepo from "@/db/perfume-repo";
import db from "@/db";
import { R2_API_ADDRESS } from "@/services/conf";

export const dynamic = "force-dynamic";

export default async function SuprisePerfumePage() {
  console.log('wtfffffffffff');
  const id = await perfumeRepo.getSurpriseId(20);
  if (!id) return notFound();
  const perfume = await perfumeRepo.getPerfumeWithTags(id);
  if (!perfume) return notFound();
  const allTags = await db.tag.findMany();
  const tags: Tag[] = [];
  perfume?.tags.map((x) => {
    tags.push({
      id: x.tag.id,
      tag: x.tag.tag,
      color: x.tag.color,
    });
  });
  return (
    <PerfumeEditForm
      perfume={perfume}
      perfumesTags={tags}
      allTags={allTags}
      r2_api_address={R2_API_ADDRESS}
    ></PerfumeEditForm>
  );
}
