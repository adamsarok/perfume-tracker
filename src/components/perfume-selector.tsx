'use client';

import { Autocomplete, AutocompleteItem, Button, Divider, Input, Popover, PopoverContent, PopoverTrigger, Slider } from "@nextui-org/react";
import { Perfume, } from "@prisma/client";
import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import React from "react";

export interface PerfumeSelectorProps {
    perfumes: Perfume[]
}

export default function PerfumeSelector({ perfumes }: PerfumeSelectorProps) {
    const [selectedKey, setSelectedKey] = React.useState(0);
    const onSelectionChange = (id: any) => {
        setSelectedKey(id);
    };
    const handleSubmit = async () => {
        try {
            await perfumeWornRepo.wearPerfume(selectedKey);
            window.location.reload();
        } catch (error) {
            console.error(error);
        }
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
        <div className="flex mt-1 mb-1">
            {/* <form> */}
                <Button type="submit" onClick={() => {
                    handleSubmit()
                    // wearPerfume();
                    // router.push('/');
                }}
                >Spray On</Button>
            {/* </form> */}
        </div>
        <Divider className="mb-1"></Divider>
    </div>

    );
}
