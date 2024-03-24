'use client'

import { Autocomplete, AutocompleteItem } from "@nextui-org/react";
import { Perfume } from "@prisma/client";
import * as actions from "@/app/actions";
import React from "react";

export interface PerfumeSelectorProps {
    perfumes: Perfume[]
}

export default function PerfumeSelector({perfumes}: PerfumeSelectorProps) {
    //const addWearAction = actions.AddPerfume().bind(null, snippet?.id);
    const [selectedKey, setSelectedKey] = React.useState(0);

    const onSelectionChange = (id: any) => {
        setSelectedKey(id);
    };
      
    const editSnippetAction = actions.AddPerfume.bind(null, selectedKey);

    return ( <div> 
        <form>
            <button type="submit" className="border p-2 rounded" formAction={editSnippetAction}
            >Spray On</button>
        </form>
        <Autocomplete 
            defaultItems={perfumes}
            label="What are you wearing today?"
            placeholder="Search for a perfume"
            // className="max-w-xs border p-2 rounded"
            className="flex w-full flex-wrap md:flex-nowrap gap-4"
            onSelectionChange={onSelectionChange}
        >
            {perfumes.map((perfume) => (
                <AutocompleteItem key={perfume.id}  value={perfume.house + " - " + perfume.perfume}>
                    {perfume.house + " - " + perfume.perfume}
                </AutocompleteItem>
            ))}
        </Autocomplete>
      </div>
      );
}