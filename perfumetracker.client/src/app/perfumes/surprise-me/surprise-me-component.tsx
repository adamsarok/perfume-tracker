"use client";

import PerfumeEditForm from "@/components/perfume-edit-form";
import { getPerfumeRandom } from "@/services/random-perfume-service";
import { useEffect, useState } from "react";

export const dynamic = "force-dynamic";


export default function SuprisePerfumeComponent() {
  const [perfumeId, setPerfumeId] = useState<string | null>(null);
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
  if (!perfumeId) {
    return <div>No eligible perfumes found :(</div>;
  }
  return (
    <PerfumeEditForm
      perfumeId={perfumeId}
      isRandomPerfume={true}
    ></PerfumeEditForm>
  );
}
