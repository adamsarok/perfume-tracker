"use client";

import { useEffect, useMemo, useState } from "react";
import { useAsyncList } from "@react-stately/data";
import React from "react";
import ChipClouds from "../../components/chip-clouds";
import { ChipProp } from "../../components/color-chip";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { getPerfumesFulltext } from "@/services/perfume-service";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import { TagDTO } from "@/dto/TagDTO";
import { columns, PerfumeListDTO } from "./perfume-worn-columns";
import { DataTable } from "@/components/ui/data-table";
import { PerfumeSettings } from "@/services/settings-service";

export interface PerfumeWornTableProps {
  readonly allTags: TagDTO[];
  readonly settings: PerfumeSettings;
}

export default function PerfumeWornTable({ allTags, settings }: PerfumeWornTableProps) {
  const [fulltext, setFulltext] = useState("");
  const list = useAsyncList({
    async load() {
      const r: PerfumeWithWornStatsDTO[] = await getPerfumesFulltext(fulltext);
      const perfumes: PerfumeListDTO[] = r.map((x) => ({
        id: x.perfume.id,
        house: x.perfume.house,
        perfume: x.perfume.perfumeName,
        ml: x.perfume.mlLeft,
        rating: x.perfume.rating,
        wornTimes: x.wornTimes,
        lastWorn: x.lastWorn,
        tags: x.perfume.tags,
      }));
      //TODO: move filtering to server side
      const hasTag = (perfume: PerfumeListDTO, tag: TagDTO) => {
        return perfume.tags.some((perfumeTag) => perfumeTag.id === tag.id);
      };
      let items: PerfumeListDTO[];
      switch (selected) {
        case "all":
          items = perfumes;
          break;
        case "favorites":
          items = perfumes.filter((x) => x.rating >= settings.minimumRating && x.ml > 0);
          break;
        case "buy-list":
          items = perfumes.filter((x) => x.rating >= settings.minimumRating && x.ml <= 0);
          break;
        case "untagged":
          items = perfumes.filter(
            (x) => x.tags.length === 0 && x.rating >= settings.minimumRating && x.ml > 0
          );
          break;
        case "tag-filter":
          items = perfumes.filter((perfume) => {
            return (
              perfume.rating >= settings.minimumRating &&
              perfume.ml > 0 &&
              tags.every((tag) => hasTag(perfume, tag))
            );
          });

          break;
        default:
          items = perfumes;
          break;
      }
      return {
        items: items,
      };
    },
  });

  const handleReload = () => {
    list.reload(); // Call the reload method to refresh the list
  };

  const [selected, setSelected] = React.useState("all");
  const [isChipCloudVisible, setIsChipCloudVisible] = useState(true);
  const [tags, setTags] = useState<TagDTO[]>([]);
  const selectChip = (chip: string) => {
    const tag = allTags.find((x) => x.tagName === chip);
    if (tag) setTags([...tags, tag]);
  };
  const unSelectChip = (chip: string) => {
    setTags((tags: TagDTO[]) => tags.filter((x) => x.tagName != chip));
  };

  useEffect(() => {
    list.reload();
    setIsChipCloudVisible(selected === "tag-filter");
  }, [selected]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    list.reload();
  }, [tags]); // eslint-disable-line react-hooks/exhaustive-deps

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      handleReload(); // Call reload on Enter key press
    }
  };

  const bottomChipProps: ChipProp[] = [];
  allTags.forEach((allTag) => {
    bottomChipProps.push({
      name: allTag.tagName,
      color: allTag.color,
      className: "",
      onChipClick: null,
    });
  });

  const totalMl = useMemo(() => {
    return list.items.reduce((sum, perfume) => sum + (perfume.ml || 0), 0);
  }, [list.items]);

  // const totalPerfumesOwned = useMemo(() => {
  //   return list.items.filter(x => x.ml > 0).length;
  // }, [list.items]);

  return (
    <div>
      <Label htmlFor="filtering">Filtering</Label>
      <div className="flex items-center space-x-2 mb-4">
        <Input
          value={fulltext} // Bind the input value to state
          onChange={(e) => setFulltext(e.target.value)} // Update state on change
          onKeyDown={handleKeyDown}
        />
        <Button onClick={handleReload}>Reload</Button>
      </div>
      <RadioGroup
        id="filtering"
        orientation="horizontal"
        onValueChange={setSelected}
        defaultValue="all"
        className="mb-4 flex items-center space-x-2"
      >
        <div className="flex items-center space-x-2">
          <RadioGroupItem value="all" id="all" />
          <Label htmlFor="all">All</Label>
        </div>
        <div className="flex items-center space-x-2">
          <RadioGroupItem value="favorites" id="favorites" />
          <Label htmlFor="favorites">Favorites</Label>
        </div>
        <div className="flex items-center space-x-2">
          <RadioGroupItem value="buy-list" id="buy-list" />
          <Label htmlFor="buy-list">Buy list</Label>
        </div>
        <div className="flex items-center space-x-2">
          <RadioGroupItem value="untagged" id="untagged" />
          <Label htmlFor="untagged">Untagged</Label>
        </div>
        <div className="flex items-center space-x-2">
          <RadioGroupItem value="tag-filter" id="tag-filter" />
          <Label htmlFor="tag-filter">Filter by tags</Label>
        </div>
      </RadioGroup>
      {isChipCloudVisible && (
        <ChipClouds
          className=""
          topChipProps={[]}
          bottomChipProps={bottomChipProps}
          selectChip={selectChip}
          unSelectChip={unSelectChip}
        />
      )}
      <Separator></Separator>
      <div className="container mx-auto py-4">
        <Label className="text-right">
          Total: {totalMl.toFixed()} ml of {list.items.length} perfumes
        </Label>
        <DataTable columns={columns} data={list.items} />
      </div>
    </div>
  );
}
