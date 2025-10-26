"use client";

import { useCallback, useEffect, useState } from "react";
import ChipClouds from "../../components/chip-clouds";
import { ChipProp } from "../../components/color-chip";
import MessageBox from "../../components/message-box";
import { useRouter } from "next/navigation";
import UploadComponent from "../../components/upload-component";
import SprayOnComponent from "../../components/spray-on";
import { Button } from "../../components/ui/button";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "../../components/ui/form";
import { Input } from "../../components/ui/input";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Separator } from "../../components/ui/separator";
import { PerfumeUploadDTO } from "@/dto/PerfumeUploadDTO";
import {
  addPerfume,
  deletePerfume,
  getPerfume,
  updatePerfume,
  getNextPerfumeId,
  getPreviousPerfumeId,
} from "@/services/perfume-service";
import { TagDTO } from "@/dto/TagDTO";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import { Label } from "../../components/ui/label";
import { format } from "date-fns";
import { showError, showSuccess } from "@/services/toasty-service";
import { Save, Trash2, ChevronLeft, ChevronRight } from "lucide-react";
import { getTags } from "@/services/tag-service";
import { AxiosResult, get } from "@/services/axios-service";
import { useAuth } from "@/hooks/use-auth";
import PerfumeRatings from "./perfume-ratings";
import { PerfumeDTO } from "@/dto/PerfumeDTO";

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
  amount: z.coerce.number().min(0).max(200),
  mlLeft: z.coerce.number().min(0).max(200),
  winter: z.boolean(),
  summer: z.boolean(),
  autumn: z.boolean(),
  spring: z.boolean(),
  imageObjectKey: z.string().nullable(),
  imageUrl: z.string(),
});

export default function PerfumeEditForm({
  perfumeId,
  isRandomPerfume,
}: PerfumeEditFormProps) {
  const [allTags, setAllTags] = useState<TagDTO[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [perfume, setPerfume] = useState<PerfumeWithWornStatsDTO | null>(null);
  const [topChipProps, setTopChipProps] = useState<ChipProp[]>([]);
  const [bottomChipProps, setBottomChipProps] = useState<ChipProp[]>([]);
  const [imageUrl, setImageUrl] = useState<string | null>(null);
  const [showUploadButtons, setShowUploadButtons] = useState(false);

  const auth = useAuth();

  useEffect(() => {
    const load = async () => {
      if (perfumeId) {
        const result = await getPerfume(perfumeId);
        if (result.error || !result.data) {
          showError("Could not load perfume", result.error ?? "unknown error");
          setIsLoading(false);
          return;
        }
        await loadPerfume(result.data);
      } else {
        await loadPerfume(null);
      }
    };
    load();
  }, []);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "ArrowLeft") {
        handlePrev();
      } else if (e.key === "ArrowRight") {
        handleNext();
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [perfume]);

  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      house: "",
      perfume: "",
      amount: 0,
      mlLeft: 0,
      winter: true,
      summer: true,
      autumn: true,
      spring: true,
      imageObjectKey: "",
      imageUrl: "",
    },
  });

  useEffect(() => {
    if (perfume) {
      form.reset({
        house: perfume.perfume.house,
        perfume: perfume.perfume.perfumeName,
        amount: perfume.perfume.ml,
        mlLeft: perfume.perfume.mlLeft,
        winter: perfume.perfume.winter,
        summer: perfume.perfume.summer,
        autumn: perfume.perfume.autumn,
        spring: perfume.perfume.spring,
        imageObjectKey: perfume.perfume.imageObjectKey ?? "",
        imageUrl: perfume.perfume.imageUrl,
      });
    }
  }, [perfume, form]);

  const router = useRouter();

  async function loadPerfume(perfume: PerfumeWithWornStatsDTO | null) {
    try {
      const result = await getTags();
      if (result.error || !result.data) {
        showError("Could not load tags", result.error ?? "unknown error");
        setIsLoading(false);
        return;
      }
      const loadedTags = result.data;
      const topChips: ChipProp[] = [];
      const bottomChips: ChipProp[] = [];
      if (perfume) {
        setPerfume(perfume);
        setImageUrl(perfume.perfume.imageUrl);
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
      } else {
        loadedTags.forEach((allTag) => {
          bottomChips.push({
            name: allTag.tagName,
            color: allTag.color,
            className: "",
            onChipClick: null,
          });
        });
      }
      setAllTags(loadedTags);
      setTopChipProps(topChips);
      setBottomChipProps(bottomChips);
    } catch (error) {
      console.error("Failed to load perfume/tags:", error);
    } finally {
      setIsLoading(false);
    }
  }

  async function onSubmit(values: z.infer<typeof formSchema>) {
    if (auth.guardAction()) return;
    const perf: PerfumeUploadDTO = {
      id: perfumeId,
      house: values.house,
      perfumeName: values.perfume,
      ml: values.amount,
      mlLeft: values.mlLeft,
      summer: values.summer,
      winter: values.winter,
      autumn: values.autumn,
      spring: values.spring,
      tags: perfume?.perfume.tags ?? [],
    };
    let result: AxiosResult<PerfumeDTO>;
    if (!perfumeId) result = await addPerfume(perf);
    else result = await updatePerfume(perf);
    if (result.error || !result.data) {
      showError("Update failed:", result.error ?? "unknown error");
    } else {
      showSuccess("Update successful");
      reload(result.data.id);
    }
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
    if (auth.guardAction()) return;
    if (id) {
      const result = await deletePerfume(id);
      if (result.error) showError("Perfume deletion failed", result.error);
      else {
        showSuccess("Perfume deleted!");
        router.push("/");
      }
    }
  };

  const onUpload = async (guid: string | undefined) => {
    if (perfume?.perfume.id && guid) {
      const qryPresigned = `/images/get-presigned-url/${encodeURIComponent(
        guid
      )}`;
      const result = await get<string>(qryPresigned);
      if (result.error || !result.data) {
        showError("Could not get presigned url", result.error);
        return;
      }
      setPerfume(prev =>
        prev
          ? {
              ...prev,
              perfume: { ...prev.perfume, imageUrl: result.data ?? "" },
            }
          : prev
      );
      setImageUrl(result.data);
    }
  };

  // Navigation handlers
  const handlePrev = async () => {
    if (!perfume?.perfume.id) return;
    const prev = await getPreviousPerfumeId(perfume.perfume.id);
    if (prev.error || !prev.data) {
      showError("Could not load perfume", prev.error ?? "unknown error");
      return;
    }
    router.push(`/perfumes/${prev.data}`);
  };

  const handleNext = async () => {
    if (!perfume?.perfume.id) return;
    const next = await getNextPerfumeId(perfume.perfume.id);
    if (next.error || !next.data) {
      showError("Could not load perfume", next.error ?? "unknown error");
      return;
    }
    router.push(`/perfumes/${next.data}`);
  };

  if (isLoading) {
    return <div>Loading...</div>;
  }

  return (
    <div>
      <Form {...form}>
        <form
          onSubmit={form.handleSubmit(onSubmit)}
          className="flex flex-col items-center justify-center w-full"
        >
          <div className="flex flex-col items-center justify-center w-full">
            <div className="w-full">
              <div className="flex items-center justify-center space-x-2 mb-2 w-full">
                <Button
                  type="button"
                  size="icon"
                  onClick={handlePrev}
                  disabled={isLoading}
                  aria-label="Previous perfume"
                >
                  <ChevronLeft />
                </Button>
                <div
                  style={{
                    width: 200,
                    height: 200,
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    background: !imageUrl ? "#f3f3f3" : "transparent",
                    borderRadius: 8,
                    overflow: "hidden",
                  }}
                >
                  <img
                    onClick={() => {
                      if (auth.guardAction()) return;
                      setShowUploadButtons(!showUploadButtons);
                    }}
                    alt={
                      imageUrl
                        ? "Image of a perfume"
                        : "Placeholder icon for a perfume"
                    }
                    style={{
                      maxWidth: "100%",
                      maxHeight: "100%",
                      objectFit: "contain",
                      cursor: "pointer",
                      display: "block",
                    }}
                    src={imageUrl || "/perfume-icon.svg"}
                  />
                </div>
                <Button
                  type="button"
                  size="icon"
                  onClick={handleNext}
                  disabled={isLoading}
                  aria-label="Next perfume"
                >
                  <ChevronRight />
                </Button>
              </div>
              {showUploadButtons && (
                <UploadComponent
                  perfumeId={perfume?.perfume.id}
                  onUpload={onUpload}
                />
              )}
            </div>
            <div className="text-center w-full">
              <div className="flex w-full space-x-2">
                <FormField
                  control={form.control}
                  name="house"
                  render={({ field }) => (
                    <FormItem className="w-64">
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
                    <FormItem className="w-64">
                      <FormLabel>Perfume</FormLabel>
                      <FormControl>
                        <Input placeholder="Perfume" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
              <div className="flex w-full space-x-2">
                <FormField
                  control={form.control}
                  name="amount"
                  render={({ field }) => (
                    <FormItem className="w-64">
                      <FormLabel>Bottle (ml)</FormLabel>
                      <FormControl>
                        <Input placeholder="Ml" {...field} className="w-full" />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="mlLeft"
                  render={({ field }) => (
                    <FormItem className="w-64">
                      <FormLabel>Remaining (ml)</FormLabel>
                      <FormControl>
                        <Input
                          placeholder="Ml left"
                          {...field}
                          className="w-full"
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
              <div className="w-full">
                <ChipClouds
                  className="w-full"
                  bottomChipProps={bottomChipProps}
                  topChipProps={topChipProps}
                  selectChip={selectChip}
                  unSelectChip={unSelectChip}
                />
              </div>
              <Separator className="mb-2"></Separator>
              <div className="flex mb-2 justify-between w-full">
                <Button color="primary" className="w-32" type="submit">
                  <Save /> {perfume ? "Update" : "Insert"}
                </Button>
                <MessageBox
                  className="w-32"
                  startContent={<Trash2 />}
                  modalButtonColor="danger"
                  modalButtonText="Delete"
                  message="Are you sure you want to delete this perfume?"
                  onButton1={() => {
                    onDelete(perfume?.perfume.id);
                  }}
                  button1text="Delete"
                  onButton2={null}
                  button2text="Cancel"
                ></MessageBox>
              </div>

              <Separator className="mb-2"></Separator>
              <div className="flex items-center space-x-4 mb-2 mt-2 w-full">
                <Label>{`Last worn: ${
                  perfume?.lastWorn
                    ? format(new Date(perfume?.lastWorn), "yyyy.MM.dd")
                    : ""
                }`}</Label>
                <Separator orientation="vertical" className="h-6" />
                <Label>{`Worn ${perfume?.wornTimes} times`}</Label>
              </div>
              <Separator className="mb-2"></Separator>
              <div className="flex items-center space-x-4 mb-2 mt-2 w-full">
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
                className="w-full"
                isRandomPerfume={isRandomPerfume}
              ></SprayOnComponent>
              <Separator className="mb-2 mt-2"></Separator>
            </div>
          </div>
        </form>
        {perfume && <PerfumeRatings perfume={perfume}></PerfumeRatings>}
      </Form>
    </div>
  );
}
