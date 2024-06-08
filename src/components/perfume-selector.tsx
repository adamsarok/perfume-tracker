'use client'

import { Autocomplete, AutocompleteItem, Button, Divider, Input, Popover, PopoverContent, PopoverTrigger, Slider } from "@nextui-org/react";
import { Perfume } from "@prisma/client";
import * as actions from "@/app/actions";
import React, { useCallback } from "react";
import PerfumeCard from "./perfumecard";

export interface PerfumeSelectorDTO {
    wornOn: Date | null;
    // id: number
    // house: string
    // perfume: string
    // rating: number
    // nsfw: boolean
    perfume: Perfume,
    isSuggested: boolean
}

export interface PerfumeSelectorProps {
    perfumes: PerfumeSelectorDTO[] 
}

export default function PerfumeSelector({perfumes}: PerfumeSelectorProps) {
    //const addWearAction = actions.AddPerfume().bind(null, snippet?.id);
    const suggested = perfumes.filter((x) => x.isSuggested);
    const [selectedKey, setSelectedKey] = React.useState(0);

    const onSelectionChange = (id: any) => {
        setSelectedKey(id);
    };
      
    const wearPerfume = actions.WearPerfume.bind(null, selectedKey);
    //this is most likely terrible, maybe not necessary? dont want to test now
    const doNothingPromise = async (): Promise<void> => {};
    const firstSugg = suggested.length > 0 ? actions.WearPerfume.bind(null, suggested[0].perfume.id) : doNothingPromise;
    const secondSugg = suggested.length > 1 ? actions.WearPerfume.bind(null, suggested[1].perfume.id) : doNothingPromise;
    const thirdSugg = suggested.length > 2 ? actions.WearPerfume.bind(null, suggested[2].perfume.id) : doNothingPromise;

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
            {perfumes.map((perfumeDto) => (
                <AutocompleteItem key={perfumeDto.perfume.id}  value={perfumeDto.perfume.house + " - " + perfumeDto.perfume.perfume}>
                    {perfumeDto.perfume.house + " - " + perfumeDto.perfume.perfume}
                    
                </AutocompleteItem>
            ))}
            
        </Autocomplete>
        <div className="flex mt-1 mb-1">
            <form>
                <Button type="submit"  formAction={wearPerfume}
                >Spray On</Button>
            </form>
        </div>
        {/* <div className="flex mt-1 mb-1">
            <form>
                <Button type="submit"  formAction={suggest}
                >Suggestion</Button>
            </form>
        </div> */}
        <Popover placement="bottom" showArrow offset={10}>
        <PopoverTrigger>
            <Button color="primary">Suggestion</Button>
        </PopoverTrigger>
        <PopoverContent className="w-[400px]">
            {(titleProps) => (
            <div className="px-1 py-2 w-full">
                <p className="text-small font-bold text-foreground" {...titleProps}>
                Suggestions
                </p>
                <div className="mt-2 flex flex-col gap-2 w-full">
                    <PerfumeCard children 
                        perfume={suggested[0].perfume} 
                        id={suggested[0].perfume.id} 
                        wornOn={suggested[0].wornOn} 
                        avatar={suggested[0].perfume.rating.toString()}>
                    </PerfumeCard>
                    <form>
                        <Button type="submit" size="sm" formAction={firstSugg}>Wear</Button>
                    </form>
                    <Divider></Divider>
                    <PerfumeCard children
                        perfume={suggested[1].perfume}
                        id={suggested[1].perfume.id}
                        wornOn={suggested[1].wornOn} 
                        avatar={suggested[1].perfume.rating.toString()}>
                    </PerfumeCard>
                    <form>
                        <Button type="submit" size="sm" formAction={secondSugg}>Wear</Button>
                    </form>
                    <Divider></Divider>
                    <PerfumeCard children 
                        perfume={suggested[2].perfume} 
                        id={suggested[2].perfume.id} 
                        wornOn={suggested[2].wornOn} 
                        avatar={suggested[2].perfume.rating.toString()}>
                    </PerfumeCard>
                    <form>
                        <Button type="submit" size="sm" formAction={thirdSugg}>Wear</Button>
                    </form>
                    <Divider></Divider>
                    {/* <Button type="submit" className="h-[80px] mb-2" style={{ whiteSpace: 'normal', padding: '16px', textAlign: 'center' }} formAction={firstSugg}>{getDispName(suggested[0])}</Button>
                    <Button type="submit" className="h-[80px] mb-2" style={{ whiteSpace: 'normal', padding: '16px', textAlign: 'center' }} formAction={secondSugg}>{getDispName(suggested[1])}</Button>
                    <Button type="submit" className="h-[80px] mb-2" style={{ whiteSpace: 'normal', padding: '16px', textAlign: 'center' }} formAction={thirdSugg}>{getDispName(suggested[2])}</Button> */}
                
                </div>
            </div>
            )}
        </PopoverContent>
        </Popover>
        <Divider className="mb-1"></Divider>
      </div>
      
      );
}

function getDispName(dto: PerfumeSelectorDTO) : string {
    return `${dto.perfume.house} - ${dto.perfume.perfume} Rating: ${dto.perfume.rating} Worn: ${dto.wornOn ? dto.wornOn : ""}`;
}