'use client'

import { Autocomplete, AutocompleteItem, Button, Divider, Slider } from "@nextui-org/react";
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
      
    const wearPerfume = actions.WearPerfume.bind(null, selectedKey);
    const suggest = actions.getSuggestion.bind(null);
    //how do I change the selected value?

    //TODO: place suggestion button!
    return ( <div> 
        <Autocomplete 
            defaultItems={perfumes}
            label="What are you wearing today?"
            placeholder="Choose a Perfume"
            // className="max-w-xs border p-2 rounded"
            className="flex w-full flex-wrap md:flex-nowrap gap-4"
            onSelectionChange={onSelectionChange}
            variant="faded"
            //defaultSelectedKey={defaultSelectedKey}
            //defaultInputValue={defaultInput}
        >
            {perfumes.map((perfume) => (
                <AutocompleteItem key={perfume.id}  value={perfume.house + " - " + perfume.perfume}>
                    {perfume.house + " - " + perfume.perfume}
                    
                </AutocompleteItem>
            ))}
            
        </Autocomplete>
        <div className="flex mt-1 mb-1">
            <form>
                <Button type="submit"  formAction={wearPerfume}
                >Spray On</Button>
            </form>
        </div>
        <div className="flex mt-1 mb-1">
            <form>
                <Button type="submit"  formAction={suggest}
                >Suggestion</Button>
            </form>
        </div>
        <Divider className="mb-1"></Divider>
      </div>
      
      );
}