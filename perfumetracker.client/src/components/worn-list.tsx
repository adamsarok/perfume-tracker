'use client';

import React, { useEffect, useState } from "react";
import PerfumeCard from "@/components/perfumecard";
import { useInView } from 'react-intersection-observer'
import { PerfumeWornDTO } from "@/dto/PerfumeWornDTO";
import { getWornBeforeID } from "@/services/perfume-worn-service";

export default function WornList() {
    const [worns, setWorns] = useState<PerfumeWornDTO[]>([]);
    const [cursor, setCursor] = useState<number | null>(null);
    const { ref, inView } = useInView();
    const cardsPerPage = 10;

    const loadMoreCards = async () => {
        const cursor = worns && worns.length > 0 ? Math.min(...worns.map(x => x.id)) : null;
        setCursor(cursor);
        const newWorns = await getWornBeforeID(cursor, cardsPerPage);
        const parsedWorns = newWorns.map(worn => ({
            ...worn,
            wornOn: new Date(worn.wornOn)
        }));
        setWorns([...worns, ...parsedWorns.toSorted((a, b) => { return b.wornOn.getTime() - a.wornOn.getTime()})]);
    };
    
    useEffect(() => {
        if (inView) loadMoreCards();
    }, [inView]); // eslint-disable-line react-hooks/exhaustive-deps

    useEffect(() => {
        loadMoreCards();
    }, []); // eslint-disable-line react-hooks/exhaustive-deps

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
