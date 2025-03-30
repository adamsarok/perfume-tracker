"use client";

import { useCallback, useEffect, useState } from "react";
import ChipClouds from "./chip-clouds";
import { ChipProp } from "./color-chip";
import MessageBox from "./message-box";
import { useRouter } from "next/navigation";
import UploadComponent from "./upload-component";
import styles from "./perfume-edit-form.module.css";
import SprayOnComponent from "./spray-on";
import { getImageUrl } from "./r2-image";
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
import {
  addPerfume,
  deletePerfume,
  updateImageGuid,
  updatePerfume,
} from "@/services/perfume-service";
import { TagDTO } from "@/dto/TagDTO";
import { ActionResult } from "@/dto/ActionResult";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import { Label } from "./ui/label";
import { format } from "date-fns";
import { showError, showSuccess } from "@/services/toasty-service";
import { Save, Trash2 } from "lucide-react";

interface PerfumeEditFormProps {
  readonly perfumeWithWornStats: PerfumeWithWornStatsDTO | null;
  readonly allTags: TagDTO[];
  readonly perfumesTags: TagDTO[];
  readonly r2_api_address: string | undefined;
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
  mlLeft: z.coerce.number().min(0).max(200),
  notes: z.string().min(1, {
    message: "Notes must be at least 1 characters.",
  }),
  winter: z.boolean(),
  summer: z.boolean(),
  autumn: z.boolean(),
  spring: z.boolean(),
});

export default function PerfumeEditForm({
  perfumeWithWornStats,
  perfumesTags,
  allTags,
  r2_api_address,
}: PerfumeEditFormProps) {
  const perfume = perfumeWithWornStats?.perfume;
  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      house: perfume ? perfume.house : "",
      perfume: perfume ? perfume.perfumeName : "",
      rating: perfume ? perfume.rating : 0,
      amount: perfume ? perfume.ml : 0,
      mlLeft: perfume ? perfume.mlLeft : 100,
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
      mlLeft: values.mlLeft,
      imageObjectKey: imageObjectKey,
      summer: values.summer,
      winter: values.winter,
      autumn: values.autumn,
      spring: values.spring,
      tags: tags.map((tag) => ({
        id: tag.id,
        tagName: tag.tagName,
        color: tag.color,
      })),
    };
    let result: ActionResult;
    if (!id) result = await addPerfume(perf);
    else result = await updatePerfume(perf);
    if (result.ok && result.id) {
      id = result.id;
      showSuccess("Update successful");
      reload(id);
    } else showError("Update failed:", result.error ?? "unknown error");
  }

  const reload = useCallback(
    (id: number | undefined) => {
      if (id) router.push(`/perfumes/${id}`);
    },
    [router]
  );

  const topChipProps: ChipProp[] = [];
  const bottomChipProps: ChipProp[] = [];
  allTags.forEach((allTag) => {
    if (!tags.some((tag) => tag.tagName === allTag.tagName)) {
      bottomChipProps.push({
        name: allTag.tagName,
        color: allTag.color,
        className: "",
        onChipClick: null,
      });
    }
  });
  tags.forEach((x) => {
    topChipProps.push({
      name: x.tagName,
      color: x.color,
      className: "",
      onChipClick: null,
    });
  });
  const selectChip = (chip: string) => {
    const tag = allTags.find((x) => x.tagName === chip);
    if (tag) setTags([...tags, tag]);
  };
  const unSelectChip = (chip: string) => {
    setTags((tags: TagDTO[]) => tags.filter((x) => x.tagName != chip));
  };
  const onDelete = async (id: number | undefined) => {
    if (id) {
      const result = await deletePerfume(id);
      if (result.error) showError("Perfume deletion failed", result.error);
      else {
        showSuccess("Perfume deleted!");
        router.push("/");
      }
    }
  };
  const [showUploadButtons, setShowUploadButtons] = useState<boolean>(false);
  const [imageObjectKey, setImageObjectKey] = useState<string>(
    perfume ? perfume.imageObjectKey : ""
  );
  const [imageUrl, setImageUrl] = useState<string | null>(
    perfume ? perfume.imagerUrl : ""
  );
  const onUpload = async (guid: string | undefined) => {
    if (perfume?.id && guid) {
      setImageObjectKey(guid);
      setImageUrl(getImageUrl(guid, r2_api_address));
      const result = await updateImageGuid(perfume.id, guid);
      if (result.ok) showSuccess("Image upload successful");
      else showError("Image upload failed", result.error ?? "unknown error");
    }
  };
  const amount = form.watch("amount");
  useEffect(() => {
    if (!perfume?.id) {
       form.setValue("mlLeft", amount);
    }
  }, [amount]);

  return (
    <div>
      <Form {...form}>
        <form
          onSubmit={form.handleSubmit(onSubmit)}
          className={styles.container}
        >
          <div className={styles.container}>
            <div className={styles.imageContainer}>
              <div className="flex items-center justify-center space-x-2 mb-2">
                <img
                  onClick={() => setShowUploadButtons(!showUploadButtons)}
                  alt={
                    imageUrl
                      ? "Image of a perfume"
                      : "Placeholder icon for a perfume"
                  }
                  className={styles.img}
                  src={imageUrl || "/perfume-icon.svg"}
                ></img>
              </div>
              {showUploadButtons && (
                <UploadComponent
                  onUpload={onUpload}
                  r2_api_address={r2_api_address}
                />
              )}
            </div>
            <div className={styles.content}>
              {/* TODO implement <PlaylistDrawer className="mb-2 mt-2"></PlaylistDrawer> */}
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

              <div className="flex">
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
                      <FormLabel>Bottle (ml)</FormLabel>
                      <FormControl>
                        <Input placeholder="Ml" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="mlLeft"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Remaining (ml)</FormLabel>
                      <FormControl>
                        <Input placeholder="Ml left" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
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
              <div className={styles.chipContainer}>
                <ChipClouds
                  className={styles.chipContainer}
                  bottomChipProps={bottomChipProps}
                  topChipProps={topChipProps}
                  selectChip={selectChip}
                  unSelectChip={unSelectChip}
                ></ChipClouds>
              </div>

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
                  <Save /> {perfume ? "Update" : "Insert"}
                </Button>
                <MessageBox
                  className="flex-1"
                  startContent={<Trash2 />}
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
              <div className="flex items-center space-x-4 mb-2 mt-2">
                <Label>{`Last worn: ${
                  perfumeWithWornStats?.lastWorn
                    ? format(
                        new Date(perfumeWithWornStats.lastWorn),
                        "yyyy.MM.dd"
                      )
                    : ""
                }`}</Label>
                <Separator orientation="vertical" className="h-6" />
                <Label>{`Worn ${perfumeWithWornStats?.wornTimes} times`}</Label>
              </div>
              <Separator className="mb-2"></Separator>
              <SprayOnComponent
                perfumeId={perfume?.id}
                onSuccess={null}
                className=""
              ></SprayOnComponent>
              <Separator className="mb-2 mt-2"></Separator>
            </div>
          </div>
        </form>
      </Form>
    </div>
  );
}
