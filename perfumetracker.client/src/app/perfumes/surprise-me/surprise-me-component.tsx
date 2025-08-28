"use client";

import PerfumeEditForm from "@/app/perfumes/perfume-edit-form";
import { getPerfumeRandom } from "@/services/random-perfume-service";
import { showError } from "@/services/toasty-service";
import { useEffect, useState } from "react";

export const dynamic = "force-dynamic";


export default function SuprisePerfumeComponent() {
  const [perfumeId, setPerfumeId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    async function fetchData() {
        const result = await getPerfumeRandom();
        if (result.error || !result.data) {
          if (result.error) showError("Could not load random perfume", result.error ?? "unknown error");
          setLoading(false);
          setPerfumeId(null);
          return;
        }
        setPerfumeId(result.data);
        setLoading(false);
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
