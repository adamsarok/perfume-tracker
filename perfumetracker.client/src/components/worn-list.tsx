"use client";

import React, { useEffect, useState } from "react";
import PerfumeCard from "@/components/perfumecard";
import { useInView } from "react-intersection-observer";
import { PerfumeWornDTO } from "@/dto/PerfumeWornDTO";
import { getWornBeforeID } from "@/services/perfume-worn-service";

export default function WornList() {
  const [worns, setWorns] = useState<PerfumeWornDTO[]>([]);
  const [cursor, setCursor] = useState<number | null>(null);
  const { ref, inView } = useInView();
  const cardsPerPage = 10;

  const loadMoreCards = async () => {
    const newWorns = await getWornBeforeID(cursor, cardsPerPage);
    if (newWorns.length > 0) {
      const lastWorn = newWorns[newWorns.length - 1];
      setCursor(lastWorn.sequenceNumber);
      setWorns([...worns, ...newWorns]);
    }
  };

  useEffect(() => {
    if (inView) loadMoreCards();
  }, [inView]);

  useEffect(() => {
    loadMoreCards();
  }, []);

  return (
    <div className="w-full max-w-3xl mx-auto px-2">
      <div className="grid grid-cols-1 gap-4">
        {worns.map((worn) => (
          <PerfumeCard key={worn.id} worn={worn}></PerfumeCard>
        ))}
      </div>
      {cursor !== null && (
        <div ref={ref} className="text-center py-4">
          Loading...
        </div>
      )}
    </div>
  );
}
