'use client';

import React, { useCallback, useEffect, useState } from "react";
import PerfumeSelector from "../components/perfume-selector";
import PerfumeCard from "@/components/perfumecard";
import { Link } from "@nextui-org/react";
import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import * as perfumeRepo from "@/db/perfume-repo";
import { Perfume } from "@prisma/client";
import { useInView } from 'react-intersection-observer'

export default function WornList() {
    const [worns, setWorns] = useState<perfumeWornRepo.WornWithPerfume[]>([]);
    const [perfumes, setPerfumes] = useState<Perfume[]>([]);
    const [cursor, setCursor] = useState<number | null>(null);
    const [loading, setLoading] = useState(false);
    const { ref, inView } = useInView();
    const cardsPerPage = 10;

    const loadMoreCards = async () => {
        const cursor = worns && worns.length > 0 ? Math.min(...worns.map(x => x.id)) : null;
        setCursor(cursor);
        setLoading(true);
        const newWorns = await perfumeWornRepo.getWornBeforeID(cursor, cardsPerPage);
        setWorns([...worns, ...newWorns]);
        setLoading(false);
    };

    const loadPerfumes = useCallback(async () => {
        const perfumes = await perfumeRepo.getPerfumesForSelector();
        setPerfumes(perfumes);
    }, []);

    useEffect(() => {
        if (inView && (!cursor || cursor > 1)) {
            loadMoreCards();
        }
    }, [inView]);

    useEffect(() => {
        loadPerfumes();
        loadMoreCards();
    }, []);

    return (
        <div>
            {worns.map((worn) => (
                <PerfumeCard key={worn.id} worn={worn}></PerfumeCard>
            ))}
            {(!cursor || cursor > 1) && <div ref={ref}>
                Loading...
            </div>}
        </div>
    );
}
