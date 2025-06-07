"use client";

import { useCallback, useEffect, useState } from "react";
import ChipClouds from "./chip-clouds";
import { ChipProp } from "./color-chip";
import MessageBox from "./message-box";
import { notFound, useRouter } from "next/navigation";
import UploadComponent from "./upload-component";
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
  getPerfume,
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
import { getTags } from "@/services/tag-service";
import { getR2ApiAddress } from "@/services/conf-service";

interface PerfumeEditFormProps {
  readonly perfumeId: string;
  readonly isRandomPerfume: boolean;
}

const formSchema = z.object({
  house: z
    .string()
    .min(1, {
      message: "House must be at least 1 characters.",
    })
    .trim(),
  perfume: z
    .string()
    .min(1, {
      message: "Perfume must be at least 1 characters.",
    })
    .trim(),
  rating: z.coerce.number().min(0).max(10),
  amount: z.coerce.number().min(0).max(200),
  mlLeft: z.coerce.number().min(0).max(200),
  notes: z
    .string()
    .min(1, {
      message: "Notes must be at least 1 characters.",
    })
    .trim(),
  winter: z.boolean(),
  summer: z.boolean(),
  autumn: z.boolean(),
  spring: z.boolean(),
});

export default function PerfumeEditForm({
  perfumeId,
  isRandomPerfume,
}: PerfumeEditFormProps) {
  const [allTags, setAllTags] = useState<TagDTO[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [perfume, setPerfume] = useState<PerfumeWithWornStatsDTO | null>(null);
  const [r2_api_address, setR2ApiAddress] = useState<string | undefined>(
    undefined
  );
  const [topChipProps, setTopChipProps] = useState<ChipProp[]>([]);
  const [bottomChipProps, setBottomChipProps] = useState<ChipProp[]>([]);

  useEffect(() => {
    const load = async () => {
      try {
        const loadedTags = await getTags();
        if (perfumeId) {
          const perfume = await getPerfume(perfumeId);
          setPerfume(perfume);
        }
        setAllTags(loadedTags);

        const topChips: ChipProp[] = [];
        const bottomChips: ChipProp[] = [];
        loadedTags.forEach((allTag) => {
          if (
            !perfume?.perfume.tags.some((tag) => tag.tagName === allTag.tagName)
          ) {
            bottomChips.push({
              name: allTag.tagName,
              color: allTag.color,
              className: "",
              onChipClick: null,
            });
          }
        });
        perfume?.perfume.tags.forEach((x) => {
          topChips.push({
            name: x.tagName,
            color: x.color,
            className: "",
            onChipClick: null,
          });
        });
        setTopChipProps(topChips);
        setBottomChipProps(bottomChips);
        if (!perfume) return notFound();
      } catch (error) {
        console.error("Failed to load perfume/tags:", error);
      } finally {
        setIsLoading(false);
      }
    };

    load();
    getR2ApiAddress().then((address) => setR2ApiAddress(address));
  }, []);

  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      house: "",
      perfume: "",
      rating: 0,
      amount: 0,
      mlLeft: 0,
      notes: "",
      winter: true,
      summer: true,
      autumn: true,
      spring: true,
    },
  });

  useEffect(() => {
    if (perfume) {
      form.reset({
        house: perfume.perfume.house,
        perfume: perfume.perfume.perfumeName,
        rating: perfume.perfume.rating,
        amount: perfume.perfume.ml,
        mlLeft: perfume.perfume.mlLeft,
        notes: perfume.perfume.notes,
        winter: perfume.perfume.winter,
        summer: perfume.perfume.summer,
        autumn: perfume.perfume.autumn,
        spring: perfume.perfume.spring,
      });
    }
  }, [perfume, form]);

  const router = useRouter();

  async function onSubmit(values: z.infer<typeof formSchema>) {
    const perf: PerfumeUploadDTO = {
      id: perfumeId,
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
      tags: perfume?.perfume.tags ?? [],
    };
    let result: ActionResult;
    if (!perfumeId) result = await addPerfume(perf);
    else result = await updatePerfume(perf);
    if (result.ok && result.id) {
      showSuccess("Update successful");
      reload(result.id);
    } else showError("Update failed:", result.error ?? "unknown error");
  }

  const reload = useCallback(
    (id: string | undefined) => {
      if (id) router.push(`/perfumes/${id}`);
    },
    [router]
  );

  const selectChip = (chip: string) => {
    const tag = allTags.find((x) => x.tagName === chip);
    if (tag && perfume) {
      setPerfume({
        ...perfume,
        perfume: {
          ...perfume.perfume,
          tags: [...perfume.perfume.tags, tag],
        },
      } as PerfumeWithWornStatsDTO);
    }
  };
  const unSelectChip = (chip: string) => {
    if (perfume) {
      setPerfume({
        ...perfume,
        perfume: {
          ...perfume.perfume,
          tags: [...perfume.perfume.tags.filter((x) => x.tagName != chip)],
        },
      } as PerfumeWithWornStatsDTO);
    }
  };
  const onDelete = async (id: string | undefined) => {
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
    perfume ? perfume.perfume.imageObjectKey : ""
  );
  const [imageUrl, setImageUrl] = useState<string | null>(
    perfume ? perfume.perfume.imagerUrl : ""
  );
  const onUpload = async (guid: string | undefined) => {
    if (perfume?.perfume.id && guid) {
      setImageObjectKey(guid);
      setImageUrl(getImageUrl(guid, r2_api_address));
      const result = await updateImageGuid(perfume.perfume.id, guid);
      if (result.ok) showSuccess("Image upload successful");
      else showError("Image upload failed", result.error ?? "unknown error");
    }
  };
  const amount = form.watch("amount");
  useEffect(() => {
    if (!perfume?.perfume.id) {
      form.setValue("mlLeft", amount);
    }
  }, [amount]);

  if (isLoading) {
    return <div>Loading...</div>;
  }

  return (
    <div>
      <Form {...form}>
        <form
          onSubmit={form.handleSubmit(onSubmit)}
          className="flex flex-col items-center justify-center p-5 mt-0"
        >
          <div className="flex flex-col items-center justify-center p-5 mt-0">
            <div className="w-full max-w-[250px]">
              <div className="flex items-center justify-center space-x-2 mb-2">
                <img
                  onClick={() => setShowUploadButtons(!showUploadButtons)}
                  alt={
                    imageUrl
                      ? "Image of a perfume"
                      : "Placeholder icon for a perfume"
                  }
                  className="max-w-[150px] object-contain"
                  src={imageUrl || "/perfume-icon.svg"}
                />
              </div>
              {showUploadButtons && <UploadComponent onUpload={onUpload} />}
            </div>
            <div className="text-center">
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
              <div className="w-full mb-0 mr-5">
                <ChipClouds
                  className="w-full mb-0 mr-5"
                  bottomChipProps={bottomChipProps}
                  topChipProps={topChipProps}
                  selectChip={selectChip}
                  unSelectChip={unSelectChip}
                />
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
                    onDelete(perfume?.perfume.id);
                  }}
                  button1text="Delete"
                  onButton2={null}
                  button2text="Cancel"
                ></MessageBox>
              </div>
              <Separator className="mb-2"></Separator>
              <div className="flex items-center space-x-4 mb-2 mt-2">
                <Label>{`Last worn: ${
                  perfume?.lastWorn
                    ? format(new Date(perfume?.lastWorn), "yyyy.MM.dd")
                    : ""
                }`}</Label>
                <Separator orientation="vertical" className="h-6" />
                <Label>{`Worn ${perfume?.wornTimes} times`}</Label>
              </div>
              <Separator className="mb-2"></Separator>
              <div className="flex items-center space-x-4 mb-2 mt-2">
                <Label>
                  Usage: {perfume?.burnRatePerYearMl?.toFixed(1)} ml/year
                </Label>
                <Separator orientation="vertical" className="h-6" />
                <Label>{perfume?.yearsLeft?.toFixed(1)} years left</Label>
              </div>
              <Separator className="mb-2"></Separator>
              <SprayOnComponent
                perfumeId={perfume?.perfume.id}
                onSuccess={null}
                className=""
                isRandomPerfume={isRandomPerfume}
              ></SprayOnComponent>
              <Separator className="mb-2 mt-2"></Separator>
            </div>
          </div>
        </form>
      </Form>
    </div>
  );
}
