"use client";

import { Perfume, Tag } from "@prisma/client";
import * as perfumeRepo from "@/db/perfume-repo";
import { useCallback, useState } from "react";
import ChipClouds from "./chip-clouds";
import { ChipProp } from "./color-chip";
import MessageBox from "./message-box";
import { toast } from "react-toastify";
import { useRouter } from "next/navigation";
import { TrashBin } from "../icons/trash-bin";
import { FloppyDisk } from "@/icons/floppy-disk";
import UploadComponent from "./upload-component";
import styles from "./perfume-edit-form.module.css";
import SprayOnComponent from "./spray-on";
import { getImageUrl } from "./r2-image";
import { ActionResult } from "@/db/action-result";
import { Button } from "./ui/button";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "./ui/form";
import { Input } from "./ui/input";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Checkbox } from "./ui/checkbox";
import { Separator } from "./ui/separator";
import { PerfumeUploadDTO } from "@/dto/PerfumeUploadDTO";
import { addPerfume, deletePerfume, updatePerfume } from "@/services/perfume-service";

interface PerfumeEditFormProps {
  perfume: Perfume | null;
  allTags: Tag[];
  perfumesTags: Tag[];
  r2_api_address: string | undefined;
}

const formSchema = z.object({
  house: z.string().min(1, {
    message: "House must be at least 1 characters.",
  }),
  perfume: z.string().min(1, {
    message: "Perfume must be at least 1 characters.",
  }),
  rating: z.coerce.number().min(0).max(10),
  amount: z.coerce.number().min(0).max(200),
  notes: z.string().min(1, {
    message: "Notes must be at least 1 characters.",
  }),
  winter: z.boolean(),
  summer: z.boolean(),
  autumn: z.boolean(),
  spring: z.boolean()
});

export default function PerfumeEditForm({
  perfume,
  perfumesTags,
  allTags,
  r2_api_address,
}: PerfumeEditFormProps) {
  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      house: perfume ? perfume.house : "",
      perfume: perfume ? perfume.perfume : "",
      rating: perfume ? perfume.rating : 0,
      amount: perfume ? perfume.ml : 0,
      notes: perfume ? perfume.notes : "",
      winter: perfume ? perfume.winter : true,
      summer: perfume ? perfume.summer : true,
      autumn: perfume ? perfume.autumn : true,
      spring: perfume ? perfume.spring : true,
    },
  });
  let id = perfume ? perfume.id : 0;
  const router = useRouter();
  const [tags, setTags] = useState(perfumesTags);

  async function onSubmit(values: z.infer<typeof formSchema>) {
    const perf: PerfumeUploadDTO = {
      id: id,
      house: values.house,
      perfumeName: values.perfume,
      rating: values.rating,
      notes: values.notes,
      ml: values.amount,
      imageObjectKey: imageObjectKey,
      summer: values.summer,
      winter: values.winter,
      autumn: values.autumn,
      spring: values.spring,
      tags: tags.map(tag =>  ({ id: tag.id, tagName: tag.tag, color: tag.color }))
    };
    let result: ActionResult;
    if (!id) result = await addPerfume(perf);
    else result = await updatePerfume(perf);
    console.log(result);
    if (result.ok && result.id) {
        id = result.id;
        toast.success("Update successful");
        reload(id);
    } else toast.error(`Update failed: ${result.error ?? "unknown error"}`);
    console.log(values);
  }

  const reload = useCallback(
    (id: number | undefined) => {
      if (id) router.push(`/perfumes/${id}`);
    },
    [router]
  );

  const topChipProps: ChipProp[] = [];
  const bottomChipProps: ChipProp[] = [];
  allTags.map((allTag) => {
    if (!tags.some((tag) => tag.tag === allTag.tag)) {
      bottomChipProps.push({
        name: allTag.tag,
        color: allTag.color,
        className: "",
        onChipClick: null,
      });
    }
  });
  tags.map((x) => {
    topChipProps.push({
      name: x.tag,
      color: x.color,
      className: "",
      onChipClick: null,
    });
  });
  const selectChip = (chip: string) => {
    const tag = allTags.find((x) => x.tag === chip);
    if (tag) setTags([...tags, tag]);
  };
  const unSelectChip = (chip: string) => {
    setTags((tags: Tag[]) => tags.filter((x) => x.tag != chip));
  };
  const onDelete = async (id: number | undefined) => {
    if (id) {
      const result = await deletePerfume(id);
      if (result.error) toast.error(result.error);
      else {
        toast.success("Perfume deleted!");
        router.push("/");
      }
    }
  };

  const [imageObjectKey, setImageObjectKey] = useState<string>(
    perfume ? perfume.imageObjectKey : ""
  );
  const [imageUrl, setImageUrl] = useState<string | null>(
    getImageUrl(perfume?.imageObjectKey, r2_api_address)
  );
  const onUpload = async (guid: string | undefined) => {
    if (guid) {
      setImageObjectKey(guid);
      setImageUrl(getImageUrl(guid, r2_api_address));
      const result = await setImageGuidInRepo(guid);
      if (result.ok) toast.success("Image upload successful");
      else
        toast.error(`Image upload failed: ${result.error ?? "unknown error"}`);
    }
  };
  const setImageGuidInRepo = async (
    guid: string | null
  ): Promise<ActionResult> => {
    if (perfume && guid) {
      return perfumeRepo.setImageObjectKey(perfume.id, guid);
    }
    return {
      ok: true,
    };
  };
  return (
    <div>
      <Form {...form}>
        <form
          onSubmit={form.handleSubmit(onSubmit)}
          className={styles.container}
        >
          <div className={styles.container}>
            <div className={styles.imageContainer}>
              <img
                alt={
                  imageUrl
                    ? "Image of a perfume"
                    : "Placeholder icon for a perfume"
                }
                className={styles.imageContainer}
                src={imageUrl ? imageUrl : "/perfume-icon.svg"}
              ></img>
              <UploadComponent onUpload={onUpload} />
            </div>
            <div className={styles.content}>
              <FormField
                control={form.control}
                name="house"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>House</FormLabel>
                    <FormControl>
                      <Input placeholder="House" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="perfume"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Perfume</FormLabel>
                    <FormControl>
                      <Input placeholder="Perfume" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="rating"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Rating</FormLabel>
                    <FormControl>
                      <Input placeholder="Rating" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="amount"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Amount</FormLabel>
                    <FormControl>
                      <Input placeholder="Amount" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="notes"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Notes</FormLabel>
                    <FormControl>
                      <Input placeholder="Notes" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <div className="flex items-center space-x-4 mt-2 mb-2">
                <FormField
                    control={form.control}
                    name="winter"
                    render={({ field }) => (
                    <FormItem>
                        <FormLabel>Winter </FormLabel>
                        <FormControl>
                        <Checkbox
                            checked={field.value}
                            onCheckedChange={field.onChange}
                            />
                        </FormControl>
                        <FormMessage />
                    </FormItem>
                    )}
                />
                <FormField
                    control={form.control}
                    name="spring"
                    render={({ field }) => (
                    <FormItem>
                        <FormLabel>Spring </FormLabel>
                        <FormControl>
                        <Checkbox
                            checked={field.value}
                            onCheckedChange={field.onChange}
                            />
                        </FormControl>
                        <FormMessage />
                    </FormItem>
                    )}
                />
                 <FormField
                    control={form.control}
                    name="summer"
                    render={({ field }) => (
                    <FormItem>
                        <FormLabel>Summer </FormLabel>
                        <FormControl>
                        <Checkbox
                            checked={field.value}
                            onCheckedChange={field.onChange}
                            />
                        </FormControl>
                        <FormMessage />
                    </FormItem>
                    )}
                />
                 <FormField
                    control={form.control}
                    name="autumn"
                    render={({ field }) => (
                    <FormItem>
                        <FormLabel>Autumn </FormLabel>
                        <FormControl>
                        <Checkbox
                            checked={field.value}
                            onCheckedChange={field.onChange}
                            />
                        </FormControl>
                        <FormMessage />
                    </FormItem>
                    )}
                />
              </div>
              <div></div>
              <div className="flex mt-4 mb-2">
                <Button color="primary" className="mr-4 flex-1" type="submit">
                  <FloppyDisk /> {perfume ? "Update" : "Insert"}
                </Button>
                <MessageBox
                  className="flex-1"
                  startContent={<TrashBin />}
                  modalButtonColor="danger"
                  modalButtonText="Delete"
                  message="Are you sure you want to delete this Perfume?"
                  onButton1={() => {
                    onDelete(perfume?.id);
                  }}
                  button1text="Delete"
                  onButton2={null}
                  button2text="Cancel"
                ></MessageBox>
              </div>
              <Separator className="mb-2"></Separator>
              <SprayOnComponent
                perfumeId={perfume?.id}
                onSuccess={null}
                className=""
              ></SprayOnComponent>
            </div>
          </div>
        </form>
      </Form>
      <div className={styles.chipContainer}>
        <ChipClouds
          className={styles.chipContainer}
          bottomChipProps={bottomChipProps}
          topChipProps={topChipProps}
          selectChip={selectChip}
          unSelectChip={unSelectChip}
        ></ChipClouds>
      </div>
    </div>
  );
}
