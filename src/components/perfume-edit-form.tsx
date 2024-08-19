'use client';

import { Button, Checkbox, Divider, Input, Link } from "@nextui-org/react";
import { Perfume, Tag } from "@prisma/client";
import { useFormState } from "react-dom";
import * as perfumeRepo from "@/db/perfume-repo";
import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import { useEffect, useState } from "react";
import ChipClouds from "./chip-clouds";
import { ChipProp } from "./color-chip";
import MessageBox from "./message-box";
import { toast } from "react-toastify";
import { useRouter } from 'next/navigation';
import { TrashBin } from '../icons/trash-bin'
import { FloppyDisk } from "@/icons/floppy-disk";
import { MagicWand } from "@/icons/magic-wand";
import { error } from "console";
import { Result } from "postcss";
import UploadComponent from "./upload-component";

interface PerfumeEditFormProps {
    perfume: Perfume | null,
    allTags: Tag[],
    perfumesTags: Tag[]
}

export default function PerfumeEditForm({perfume, perfumesTags, allTags}: PerfumeEditFormProps) {
    const router = useRouter();
    const [tags, setTags] = useState(perfumesTags);
    const [formState, action] = useFormState(
        perfumeRepo.upsertPerfume.bind(null, (perfume ? perfume.id : null), tags) 
        , { errors: {}, result: null, state: 'init' });

    useEffect(() => {
        if (formState.state === 'success') {
            toast.success("Perfume saved!");
            router.push(`/perfumes/${formState.result?.id}`);
        } else if (formState.state === 'failed') {
            toast.error("Perfume save failed! " 
                + (formState.errors._form && formState.errors._form.length > 0 ? formState.errors._form?.join(',') :  ""));
        }
        formState.state = 'init';
    }
    ), [formState.result]

    //todo toast
    let topChipProps: ChipProp[] = [];
    let bottomChipProps: ChipProp[] = [];
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
    const selectChip = (chip: string) => {
        const tag = allTags.find(x => x.tag === chip);
        if (tag) setTags([...tags, tag]);
    };
    const unSelectChip = (chip: string) => {
        setTags((tags: Tag[]) => tags.filter(x => x.tag != chip));
    };
    const onDelete = async (id: number | undefined) => {
        if (id) {
            var result = await perfumeRepo.deletePerfume(id);
            if (result.error) toast.error(result.error);
            else { 
                toast.success("Perfume deleted!");
                router.push('/');
            }
        }
    }
    const onSprayOn = async (id: number | undefined) => {
        if (id) {
            var result = await perfumeWornRepo.wearPerfume(id);
            if (result.ok) toast.success("Smell on!");
            else toast.error(result.error);
        }
    }
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
            <div className="flex mt-4 mb-2">         
                <Button color="secondary"
                    startContent={<MagicWand/>}
                    onPress={() => onSprayOn(perfume?.id)}
                    className="ml-4 mr-8" 
                >Spray On</Button>
                <Button color="primary"
                    startContent={<FloppyDisk/>} 
                    className="mr-8" 
                    type="submit">{perfume ? "Update" : "Insert"}
                </Button>
                <MessageBox 
                    startContent={<TrashBin />}
                    modalButtonColor="danger"
                    modalButtonText="Delete"
                    message="Are you sure you want to delete this Perfume?"
                    onButton1={() => { onDelete(perfume?.id) }}
                    button1text="Delete"
                    onButton2={null}
                    button2text="Cancel"></MessageBox>
                <UploadComponent className='ml-8'/>
            </div>
            {formState.errors._form ? 
                <div className="p-2 bg-red-200 border border-red-400 rounded">
                    {formState.errors._form?.join(',')}
                </div> : null
            }
            <ChipClouds bottomChipProps={bottomChipProps} topChipProps={topChipProps} selectChip={selectChip} unSelectChip={unSelectChip}></ChipClouds>
        </form>
    </div>
}