"use client";

import PerfumeEditForm from "@/app/perfumes/perfume-edit-form";
import { GetRandomPerfumeResponse } from "@/dto/GetRandomPerfumeResponse";
import { getPerfumeRandom } from "@/services/random-perfume-service";
import { showError } from "@/services/toasty-service";
import { useEffect, useState } from "react";

export const dynamic = "force-dynamic";


export default function SuprisePerfumeComponent() {
  const [randomPerfume, setRandomPerfume] = useState<GetRandomPerfumeResponse | null>(null);
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    async function fetchData() {
        const result = await getPerfumeRandom();
        console.log("Random perfume fetch result:", result);
        if (result.error || !result.data || !result.data.perfumeId) {
          if (result.error) showError("Could not load random perfume", result.error ?? "unknown error");
          setLoading(false);
          setRandomPerfume(null);
          return;
        }
        setRandomPerfume(result.data);
        setLoading(false);
    }

    fetchData();
  }, []);

  if (loading) {
    return <div>Loading...</div>;
  }
  if (!randomPerfume || !randomPerfume.perfumeId) {
    return <div>No eligible perfumes found :(</div>;
  }
  return (
    <PerfumeEditForm
      perfumeId={randomPerfume.perfumeId}
      randomsId={randomPerfume.randomsId}
    ></PerfumeEditForm>
  );
}
