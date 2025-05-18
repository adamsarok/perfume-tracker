"use client";

import PerfumeEditForm from "@/components/perfume-edit-form";
import { getPerfumeRandom } from "@/services/random-perfume-service";
import { getPerfume } from "@/services/perfume-service";
import { TagDTO } from "@/dto/TagDTO";
import { getTags } from "@/services/tag-service";
import { useEffect, useState } from "react";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";

export const dynamic = "force-dynamic";

export interface SuprisePerfumeComponentProps {
    readonly r2_api_address: string | undefined
}

export default function SuprisePerfumeComponent({r2_api_address}: SuprisePerfumeComponentProps) {
  const [perfumeId, setPerfumeId] = useState<number | null>(null);
  const [perfume, setPerfume] = useState<PerfumeWithWornStatsDTO | null>(null);
  const [allTags, setAllTags] = useState<TagDTO[]>([]);
  const [tags, setTags] = useState<TagDTO[]>([]);
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    async function fetchData() {
      try {
        const id = await getPerfumeRandom();
        if (!id) {
          setPerfumeId(null);
          return;
        }
        setPerfumeId(id);
        const perfumeData = await getPerfume(id);
        if (!perfumeData) {
          setPerfume(null);
          return;
        }
        setPerfume(perfumeData);
        const allTagsData = await getTags();
        setAllTags(allTagsData);
        const tagsData: TagDTO[] = perfumeData.perfume.tags.map((x: TagDTO) => ({
          id: x.id,
          tagName: x.tagName,
          color: x.color,
        }));
        setTags(tagsData);
      } catch (error) {
        console.error("Error fetching data:", error);
        setPerfumeId(null);
      } finally {
        setLoading(false);
      }
    }

    fetchData();
  }, []);

  if (loading) {
    return <div>Loading...</div>;
  }

  if (perfumeId === null || !perfume) {
    return <div>No eligible perfumes found :(</div>;
  }
  console.log(r2_api_address);
  return (
    <PerfumeEditForm
      perfumeWithWornStats={perfume}
      perfumesTags={tags}
      allTags={allTags}
      r2_api_address={r2_api_address}
    ></PerfumeEditForm>
  );
}
