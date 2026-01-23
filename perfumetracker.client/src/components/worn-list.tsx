"use client";

import React, { useEffect, useState } from "react";
import PerfumeCard from "@/components/perfumecard";
import { useInView } from "react-intersection-observer";
import { PerfumeWornDTO } from "@/dto/PerfumeWornDTO";
import { getWornBeforeID } from "@/services/perfume-worn-service";
import { showError } from "@/services/toasty-service";

export interface WornListProps {
  refreshTrigger?: number;
}

export default function WornList({ refreshTrigger }: WornListProps) {
  const [worns, setWorns] = useState<PerfumeWornDTO[]>([]);
  const [cursor, setCursor] = useState<number | null>(null);
  const { ref, inView } = useInView();
  const cardsPerPage = 10;

  const loadMoreCards = async () => {
    const result = await getWornBeforeID(cursor, cardsPerPage);
    if (result.error || !result.data) {
      showError("Could not load worn perfumes", result.error ?? "unknown error");
      return;
    }
    const newWorns = result.data;
    newWorns.forEach(x => x.eventDate = new Date(x.eventDate));
    if (newWorns.length > 0) {
      const lastWorn = newWorns[newWorns.length - 1];
      setCursor(lastWorn.sequenceNumber);
      setWorns([...worns, ...newWorns]);
    }
  };

  const refreshList = async () => {
    setWorns([]);
    setCursor(null);
    const result = await getWornBeforeID(null, cardsPerPage);
    if (result.error || !result.data) {
      showError("Could not load worn perfumes", result.error ?? "unknown error");
      return;
    }
    const newWorns = result.data;
    newWorns.forEach(x => x.eventDate = new Date(x.eventDate));
    if (newWorns.length > 0) {
      const lastWorn = newWorns[newWorns.length - 1];
      setCursor(lastWorn.sequenceNumber);
      setWorns(newWorns);
    }
  };

  useEffect(() => {
    if (inView) loadMoreCards();
  }, [inView]);

  useEffect(() => {
    loadMoreCards();
  }, []);

  useEffect(() => {
    if (typeof refreshTrigger === "number" && refreshTrigger > 0) {
      refreshList();
    }
  }, [refreshTrigger]);

  return (
    <div className="w-full max-w-3xl mx-auto px-2">
      <div className="grid grid-cols-1 gap-4">
        {worns.map((worn) => (
          <PerfumeCard key={worn.id} worn={worn} onDelete={refreshList}></PerfumeCard>
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
