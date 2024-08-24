'use client';

import { Button, Input, Link, Image } from "@nextui-org/react";
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
import UploadComponent from "./upload-component";
import styles from "./perfume-edit-form.module.css";

interface PerfumeEditFormProps {
    perfume: Perfume | null,
    allTags: Tag[],
    perfumesTags: Tag[]
}

export default function PerfumeEditForm({ perfume, perfumesTags, allTags }: PerfumeEditFormProps) {
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
    const [imageGuid, setImageGuid] = useState<string | null>(null);
    const [imageUrl, setImageUrl] = useState<string | null>(null);
    const onUpload = (guid: string | undefined) => {
        console.log(guid)
        if (guid) {
            console.log(guid);
            setImageGuid(guid);
            setImageGuidInRepo(guid);
            //getDownloadUrl(guid);
        }
    }
    useEffect(() => {
        getDownloadUrl(imageGuid);
    }), [imageGuid];
    const getDownloadUrl = async (guid: string | null) => {
        if (guid) {
            const res = await fetch(`/api/generate-download-url?key=${encodeURIComponent(guid)}`, {
                method: 'GET'
            });
            console.log(res);
            const json = await res.json();
            if (res.ok) {
                setImageUrl(json.url);
            } else {
                console.error(`Failed to get download url: ${json.error}`);
            }
        }
    };
    if (perfume && perfume.imageObjectKey) {
        getDownloadUrl(perfume.imageObjectKey);
    }
    const setImageGuidInRepo = async (guid: string | null) => {
        console.log(`updating image url to: ${guid}`);
        if (perfume && guid) await perfumeRepo.setImageObjectKey(perfume.id, guid)
    }
    return <div>
        {/* <Head>
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        </Head> */}

        <Link isBlock showAnchorIcon href='/' color="foreground">Back</Link>
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
                    <div></div>
                    <div className="flex mt-4 mb-2">
                        <Button color="secondary"
                            startContent={<MagicWand />}
                            onPress={() => onSprayOn(perfume?.id)}
                            className="ml-2 mr-4"
                        >Spray On</Button>
                        <Button color="primary"
                            startContent={<FloppyDisk />}
                            className="mr-4"
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
                    </div>
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