'use client';

import { Button, Checkbox, Divider, Input, Link } from "@nextui-org/react";
import { Perfume, Tag } from "@prisma/client";
import { useFormState } from "react-dom";
import * as perfumeRepo from "@/db/perfume-repo";
import { useState } from "react";
import ChipClouds from "./chip-clouds";
import { ChipProp } from "./color-chip";

interface PerfumeEditFormProps {
    perfume: Perfume | null,
    allTags: Tag[],
    perfumesTags: Tag[]
}

export default function PerfumeEditForm({perfume, perfumesTags, allTags}: PerfumeEditFormProps) {
    const [tags, setTags] = useState(perfumesTags);
    const [isNsfw, setIsChecked] = useState(false);
    const handleCheckboxChange = (e: any) => setIsChecked(e.target.checked);
    const [formState, action] = useFormState(
        perfumeRepo.upsertPerfume.bind(null, (perfume ? perfume.id : null), isNsfw, tags) 
        , { errors: {} });
    //todo toast
    let topChipProps: ChipProp[] = [];
    let bottomChipProps: ChipProp[] = [];
    console.log(tags);
    allTags.map(allTag => {
        if (!tags.some(tag => tag.tag === allTag.tag)) {
            bottomChipProps.push({
                name: allTag.tag,
                color: allTag.color,
                className: "",
                onChipClick: null
            })
        }
    });
    tags.map(x => {
        topChipProps.push({
            name: x.tag,
            color: x.color,
            className: "",
            onChipClick: null
        })
    });
    console.log(tags);
    const selectChip = (chip: string) => {
        const tag = allTags.find(x => x.tag == chip);
        if (tag) tags.push(tag);
    };
    const unSelectChip = (chip: string) => {
        setTags((tags: Tag[]) => tags.filter(x => x.tag != chip));
    };
    return <div>
        <Link isBlock showAnchorIcon href='/' color="foreground">Back</Link>
        <form action={action} >
            <Input label="House" name="house" defaultValue={perfume?.house}
                isInvalid={!!formState.errors.house}
                errorMessage={formState.errors.house?.join(',')}
            ></Input>
            <Input label="Perfume" name="perfume" defaultValue={perfume?.perfume}
                isInvalid={!!formState.errors.perfume}
                errorMessage={formState.errors.perfume?.join(',')}
                ></Input>
            <Input label="Rating" name="rating" defaultValue={perfume?.rating.toString()}
                isInvalid={!!formState.errors.rating}
                errorMessage={formState.errors.rating?.join(',')}
                ></Input>
            <Input label="Amount (ml)" name="ml" defaultValue={perfume?.ml.toString()}
                isInvalid={!!formState.errors.ml}
                errorMessage={formState.errors.ml?.join(',')}
            ></Input>
            <Input label="Notes" name="notes" defaultValue={perfume?.notes}
                isInvalid={!!formState.errors.notes}
                errorMessage={formState.errors.notes?.join(',')}
            ></Input>
            <div></div>
            <Button className="mb-2 mt-2" type="submit">Update Perfume</Button>
            {formState.errors._form ? 
                <div className="p-2 bg-red-200 border border-red-400 rounded">
                    {formState.errors._form?.join(',')}
                </div> : null
            }
            <ChipClouds bottomChipProps={bottomChipProps} topChipProps={topChipProps} selectChip={selectChip} unSelectChip={unSelectChip}></ChipClouds>
        </form>
    </div>
}