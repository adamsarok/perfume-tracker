'use client'

import { Autocomplete, AutocompleteItem, Button, Slider } from "@nextui-org/react";
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
        <Autocomplete 
            defaultItems={perfumes}
            //label="What are you wearing today?"
            label="Milyen parfümöt hordasz ma?"
            placeholder="Válassz parfümöt"
            // className="max-w-xs border p-2 rounded"
            className="flex w-full flex-wrap md:flex-nowrap gap-4"
            onSelectionChange={onSelectionChange}
            variant="faded"
        >
            {perfumes.map((perfume) => (
                <AutocompleteItem key={perfume.id}  value={perfume.house + " - " + perfume.perfume}>
                    {perfume.house + " - " + perfume.perfume}
                </AutocompleteItem>
            ))}
        </Autocomplete>
        <div className="flex">
            <form>
                <Button type="submit"  formAction={editSnippetAction}
                >Magamra fújom</Button>
            </form>
            <form className="flex">
                <Slider label="Értékelés" 
                    step={0.5} 
                    maxValue={10} 
                    minValue={0} 
                    defaultValue={8}
                    className="flex-1 w-64 ml-2 mr-2"
                >Értékelés</Slider>
                <Button type="submit" formAction={editSnippetAction}
                >Értékelés</Button>
            </form>
        </div>
      </div>
      
      );
}