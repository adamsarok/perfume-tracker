'use client';

import { Autocomplete, AutocompleteItem, Divider } from "@nextui-org/react";
import { Perfume, } from "@prisma/client";
import React, { Key } from "react";
import SprayOnComponent from "./spray-on";

export interface PerfumeSelectorProps {
    perfumes: Perfume[]
}

export default function PerfumeSelector({ perfumes }: PerfumeSelectorProps) {
    const [selectedKey, setSelectedKey] = React.useState<number>(0);
    const onSelectionChange = (id: Key) => {
        setSelectedKey(id.valueOf() as number);
    };
    const onSprayOn = async () => {
        window.location.reload();
    };
    return (<div data-testid="perfume-selector">
        <Autocomplete
            defaultItems={perfumes}
            label="What are you wearing today?"
            placeholder="Choose a Perfume"
            className="flex w-full flex-wrap md:flex-nowrap gap-4"
            onSelectionChange={onSelectionChange}
            variant="faded"
        >
            {perfumes.map((perfume) => (
                <AutocompleteItem key={perfume.id} value={perfume.house + " - " + perfume.perfume}>
                    {perfume.house + " - " + perfume.perfume}

                </AutocompleteItem>
            ))}

        </Autocomplete>
        <SprayOnComponent perfumeId={selectedKey} onSuccess={onSprayOn} className="mb-2 mt-2"></SprayOnComponent> 
        <Divider className="mb-1"></Divider>
    </div>

    );
}
