'use client';

import { Button, Input, Image, Checkbox, Divider } from "@nextui-org/react";
import { Perfume, Tag } from "@prisma/client";
import { useFormState } from "react-dom";
import * as perfumeRepo from "@/db/perfume-repo";
import { useEffect, useState } from "react";
import ChipClouds from "./chip-clouds";
import { ChipProp } from "./color-chip";
import MessageBox from "./message-box";
import { toast } from "react-toastify";
import { useRouter } from 'next/navigation';
import { TrashBin } from '../icons/trash-bin'
import { FloppyDisk } from "@/icons/floppy-disk";
import UploadComponent from "./upload-component";
import styles from "./perfume-edit-form.module.css";
import SprayOnComponent from "./spray-on";
import { getImageUrl } from "./r2-image";

interface PerfumeEditFormProps {
    perfume: Perfume | null,
    allTags: Tag[],
    perfumesTags: Tag[],
    r2_api_address: string | undefined
}

export default function PerfumeEditForm({ perfume, perfumesTags, allTags, r2_api_address }: PerfumeEditFormProps) {
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
                + (formState.errors._form && formState.errors._form.length > 0 ? formState.errors._form?.join(',') : ""));
        }
        const resetFormState = () => {
            formState.state = 'init';
        };
        resetFormState();
    }, [formState.result]);

    //todo toast
    const topChipProps: ChipProp[] = [];
    const bottomChipProps: ChipProp[] = [];
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
            const result = await perfumeRepo.deletePerfume(id);
            if (result.error) toast.error(result.error);
            else {
                toast.success("Perfume deleted!");
                router.push('/');
            }
        }
    }


    const [summer, setSummer] = useState<boolean>(perfume ? perfume.summer : true);
    const [autumn, setAutumn] = useState<boolean>(perfume ? perfume.autumn : true);
    const [winter, setWinter] = useState<boolean>(perfume ? perfume.winter : true);
    const [spring, setSpring] = useState<boolean>(perfume ? perfume.spring : true);
    const [imageUrl, setImageUrl] = useState<string | null>(getImageUrl(perfume?.imageObjectKey, r2_api_address));
    const onUpload = (guid: string | undefined) => {
        if (guid) {
            setImageUrl(getImageUrl(guid, r2_api_address));
            setImageGuidInRepo(guid);
        }
    }

    const setImageGuidInRepo = async (guid: string | null) => {
        if (perfume && guid) await perfumeRepo.setImageObjectKey(perfume.id, guid)
    }
    return <div>
        <form action={action} className={styles.container}>
            <div className={styles.container}>
                <div className={styles.imageContainer}>
                    <Image className={styles.imageContainer} src={imageUrl ? imageUrl : '/perfume-icon.svg'}></Image>
                    <UploadComponent className={styles.imageContainer} onUpload={onUpload} />
                </div>
                <div className={styles.content}>
                    <Input label="House" className="content"
                        name="house"
                        defaultValue={perfume?.house}
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
                    <div className="flex items-center space-x-4 mt-2 mb-2">
                        <Checkbox name="winter" className="flex-1" value="Winter" onValueChange={setWinter} isSelected={winter}>Winter</Checkbox>
                        <Checkbox name="spring" className="flex-1" value="Spring" onValueChange={setSpring} isSelected={spring}>Spring</Checkbox>
                    </div>
                    <div className="flex items-center space-x-4 mt-2 mb-2">
                        <Checkbox name="summer" value="Summer" onValueChange={setSummer} isSelected={summer}>Summer</Checkbox>
                        <Checkbox name="autumn" value="Autumn" onValueChange={setAutumn} isSelected={autumn}>Autumn</Checkbox>
                    </div>
                    <div></div>
                    <div className="flex mt-4 mb-2">

                        <Button color="primary"
                            startContent={<FloppyDisk />}
                            className="mr-4 flex-1"
                            type="submit">{perfume ? "Update" : "Insert"}
                        </Button>
                        <MessageBox className="flex-1"
                            startContent={<TrashBin />}
                            modalButtonColor="danger"
                            modalButtonText="Delete"
                            message="Are you sure you want to delete this Perfume?"
                            onButton1={() => { onDelete(perfume?.id) }}
                            button1text="Delete"
                            onButton2={null}
                            button2text="Cancel"></MessageBox>
                    </div>
                    <Divider className="mb-2"></Divider>
                    <SprayOnComponent perfumeId={perfume?.id} onSuccess={null} className=""></SprayOnComponent>
                    {formState.errors._form ?
                        <div className="p-2 bg-red-200 border border-red-400 rounded">
                            {formState.errors._form?.join(',')}
                        </div> : null
                    }
                </div>

            </div>
        </form>
        <div className={styles.chipContainer}>
            <ChipClouds className={styles.chipContainer} bottomChipProps={bottomChipProps} topChipProps={topChipProps} selectChip={selectChip} unSelectChip={unSelectChip}></ChipClouds>
        </div>
    </div>
}