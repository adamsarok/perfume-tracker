'use client';

import React, { useEffect, useState } from "react";
import PerfumeCard from "@/components/perfumecard";
import { useInView } from 'react-intersection-observer'
import { PerfumeWornDTO } from "@/dto/PerfumeWornDTO";
import { getWornBeforeID } from "@/services/perfume-worn-service";

export interface WornListProps{
    r2_api_address: string | undefined
}

export default function WornList({r2_api_address}: WornListProps) {
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
        setWorns([...worns, ...parsedWorns.sort((a, b) => { return b.wornOn.getTime() - a.wornOn.getTime()})]);
        //TODO: this sort is not 100% correct - we load by id, but worn date can be a different order
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
                <PerfumeCard key={worn.id} worn={worn} r2_api_address={r2_api_address}></PerfumeCard>
            ))}
            {(!cursor || cursor > 1) && <div ref={ref}>
                Loading...
            </div>}
        </div>
    );
}
