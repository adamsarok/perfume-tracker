"use client";

import { useState } from "react";
import TagAddModal from "./tag-add-modal";
import ColorChip, { ChipProp } from "./color-chip";
import { TagDTO } from "@/dto/TagDTO";
import { Drawer, DrawerContent, DrawerTitle, DrawerTrigger } from "./ui/drawer";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { ScrollArea } from "./ui/scroll-area";

export interface ChipCloudProps {
  readonly topChipProps: ChipProp[];
  readonly bottomChipProps: ChipProp[];
  readonly selectChip: (chip: string) => void;
  readonly unSelectChip: (chip: string) => void;
  readonly className: string;
}

export default function ChipClouds({
  topChipProps,
  bottomChipProps,
  selectChip,
  unSelectChip,
  className,
}: ChipCloudProps) {
  const dividers: ChipProp[] = [];

  const distinctFirstLetters = Array.from(
    new Set(bottomChipProps.map((chip) => chip.name[0].toUpperCase()))
  ).sort((a, b) => a.localeCompare(b));
  
  distinctFirstLetters.forEach((letter) => {
    dividers.push({
      name: letter,
      color: "#FFFFFF",
      className: "",
      onChipClick: null,
      enabled: false
    })
  });

  const [topChips, setTopChips] = useState(topChipProps);
  const [bottomChips, setBottomChips] = useState(bottomChipProps);
  const [search, setSearch] = useState("");

  const handleTopChipClick = (chip: ChipProp) => {
    setTopChips(topChips.filter((x) => x.name !== chip.name));
    setBottomChips([...bottomChips, chip]);
    unSelectChip(chip.name);
  };
  const handleBottomChipClick = (chip: ChipProp) => {
    setTopChips([...topChips, chip]);
    setBottomChips(bottomChips.filter((x) => x.name !== chip.name));
    setSearch("");
    selectChip(chip.name);
  };

  const handleModalClose = (tag: TagDTO) => {
    setBottomChips([
      ...bottomChips,
      { name: tag.tagName, color: tag.color, onChipClick: null, className: "", enabled:true },
    ]);
  };

  return (
    <div className={className}>
      <div className="flex flex-wrap min-h-[50px]">


      <Drawer modal={false}>
        <DrawerTrigger asChild>
          <Button className="ml-1 mt-2 h-6 px-2 py-0 text-xs">Add Tags</Button>
        </DrawerTrigger>
        <DrawerContent className="max-h-72">
          <DrawerTitle>Available tags:</DrawerTitle>
          <Input
            placeholder="Search tags..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="mx-2 my-1 w-auto"
            autoFocus
          />
          <ScrollArea className="rounded-md border h-52">
            <div className="flex flex-wrap min-h-[50px]">
              <div key="New" className="ml-1 mt-2 cursor-pointer">
                <TagAddModal tagAdded={handleModalClose} />
              </div>
              {bottomChips.concat(dividers)
                .filter((c) => c.name.toLowerCase().includes(search.toLowerCase()))
                .toSorted((a, b) => a.name.localeCompare(b.name))
                .map((c) => (
                  <div key={c.name} className="ml-1 mt-2 cursor-pointer">
                    <ColorChip
                      className={c.className}
                      color={c.color}
                      name={c.name}
                      onChipClick={() => handleBottomChipClick(c)}
                      enabled={c.enabled}
                    />
                  </div>
                ))}
            </div>
          </ScrollArea>
        </DrawerContent>
      </Drawer>
      {topChips
          .toSorted((a, b) => a.name.localeCompare(b.name))
          .map((c) => (
            <div key={c.name} className="ml-1 mt-2 cursor-pointer">
              <ColorChip
                className={c.className}
                color={c.color}
                name={c.name}
                onChipClick={() => handleTopChipClick(c)}
                enabled
              />
            </div>
          ))}
      </div>
    </div>
  );
}
