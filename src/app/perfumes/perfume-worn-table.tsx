"use client";

import { useEffect, useState } from "react";
import { useAsyncList } from "@react-stately/data";
import React from "react";
import { PerfumeWornDTO } from "@/db/perfume-worn-repo";
import { Tag } from "@prisma/client";
import ChipClouds from "../../components/chip-clouds";
import { ChipProp } from "../../components/color-chip";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Label } from "@/components/ui/label";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Separator } from "@/components/ui/separator";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

export interface PerfumeWornTableProps {
  apiAddress: string | undefined;
  allTags: Tag[];
}

export default function PerfumeWornTable({
  apiAddress,
  allTags,
}: PerfumeWornTableProps) {
  const [fulltext, setFulltext] = useState("");
  const list = useAsyncList({
    async load() {
      if (!apiAddress) throw new Error("PerfumeAPI address not set");
      const qry = `${apiAddress}/perfumes/${
        fulltext ? `fulltext/${fulltext}` : ""
      }`;
      const response = await fetch(qry);
      if (!response.ok) {
        throw new Error("Failed to fetch perfumes");
      }
      const perfumes: PerfumeWornDTO[] =  await response.json();

      //TODO: move filtering to server side

      let items: PerfumeWornDTO[];
      switch (selected) {
        case "all":
          items = perfumes;
          break;
        case "favorites":
          items = perfumes.filter(
            (x) => x.perfume.rating >= 8 && x.perfume.ml > 0
          );
          break;
        case "buy-list":
          items = perfumes.filter(
            (x) => x.perfume.rating >= 8 && x.perfume.ml <= 0
          );
          break;
        case "untagged":
          items = perfumes.filter(
            (x) =>
              x.tags.length === 0 && x.perfume.rating >= 8 && x.perfume.ml > 0
          );
          break;
        case "tag-filter":
          items = perfumes.filter((perfume) => {
            return (
              perfume.perfume.rating >= 8 &&
              perfume.perfume.ml > 0 &&
              tags.every((tag) =>
                perfume.tags.some((perfumeTag) => perfumeTag.id === tag.id)
              )
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
    async sort({
      items,
      sortDescriptor,
    }: {
      items: PerfumeWornDTO[];
      sortDescriptor: any; // eslint-disable-line @typescript-eslint/no-explicit-any
    }) {
      //TODO: figure this out with Shadcn: SortDescriptor
      return {
        items: items.sort((a, b) => {
          const compare = (first: string, second: string) => {
            if (!first && !second) return 0;
            if (!first) return 1;
            if (!second) return -1;
            let cmp =
              (parseInt(first) || first) < (parseInt(second) || second)
                ? -1
                : 1;
            if (sortDescriptor.direction === "descending") {
              cmp *= -1;
            }
            return cmp;
          };

          switch (sortDescriptor.column) {
            case "house":
              return compare(a.perfume.house, b.perfume.house);
            case "perfume":
              return compare(a.perfume.perfume, b.perfume.perfume);
            case "rating":
              return compare(
                a.perfume.rating.toString(),
                b.perfume.rating.toString()
              );
            case "wornTimes":
              return compare(
                (a.wornTimes ?? 0).toString(),
                (b.wornTimes ?? 0).toString()
              );
            case "lastWorn":
              return compare(
                a.lastWorn?.toDateString() ?? "",
                b.lastWorn?.toDateString() ?? ""
              );
          }
          return 0;
        }),
      };
    },
  });

  const handleReload = () => {
    list.reload(); // Call the reload method to refresh the list
  };

  const [selected, setSelected] = React.useState("favorites");
  const [isChipCloudVisible, setIsChipCloudVisible] = useState(true);
  const [tags, setTags] = useState<Tag[]>([]);
  const selectChip = (chip: string) => {
    const tag = allTags.find((x) => x.tag === chip);
    if (tag) setTags([...tags, tag]);
  };
  const unSelectChip = (chip: string) => {
    setTags((tags: Tag[]) => tags.filter((x) => x.tag != chip));
  };

  useEffect(() => {
    list.reload();
    setIsChipCloudVisible(selected === "tag-filter");
  }, [selected]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    list.reload();
  }, [tags]); // eslint-disable-line react-hooks/exhaustive-deps

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      handleReload(); // Call reload on Enter key press
    }
  };

  const bottomChipProps: ChipProp[] = [];
  allTags.map((allTag) => {
    bottomChipProps.push({
      name: allTag.tag,
      color: allTag.color,
      className: "",
      onChipClick: null,
    });
  });

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
        defaultValue="favorites"
        className="mb-4"
      >
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
          <RadioGroupItem value="all" id="all" />
          <Label htmlFor="all">All</Label>
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
      <Table
      // sortDescriptor={list.sortDescriptor}
      // onSortChange={list.sort}
      >
        <TableHeader>
          <TableRow>
            <TableHead key="house">House</TableHead>
            <TableHead key="perfume">Perfume</TableHead>
            <TableHead key="notes">Notes</TableHead>
            <TableHead key="rating">Rating</TableHead>
            <TableHead key="wornTimes">Worn X times</TableHead>
            <TableHead key="lastWorn">Last worn</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {list.items.map((perfume) => (
            <TableRow key={perfume.perfume.id}>
              <TableCell>
                <a href={`/perfumes/${perfume.perfume.id}/`}>
                  {perfume.perfume.house}
                </a>
              </TableCell>
              <TableCell>
                <a href={`/perfumes/${perfume.perfume.id}/`}>
                  {perfume.perfume.perfume}
                </a>
              </TableCell>
              <TableCell>
                <a href={`/perfumes/${perfume.perfume.id}/`}>
                  {perfume.perfume.notes}
                </a>
              </TableCell>
              <TableCell>
                <a href={`/perfumes/${perfume.perfume.id}/`}>
                  {perfume.perfume.rating}
                </a>
              </TableCell>
              <TableCell>
                <a href={`/perfumes/${perfume.perfume.id}/`}>
                  {perfume.wornTimes}
                </a>
              </TableCell>
              <TableCell>
                <a href={`/perfumes/${perfume.perfume.id}/`}>
                  {perfume.lastWorn
                    ? new Date(perfume.lastWorn).toDateString()
                    : ""}
                </a>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
