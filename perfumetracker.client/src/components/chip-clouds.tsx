"use client";

import { useState } from "react";
import TagAddModal from "./tag-add-modal";
import ColorChip, { ChipProp } from "./color-chip";
import { TagDTO } from "@/dto/TagDTO";
import { Drawer, DrawerContent, DrawerTitle, DrawerTrigger } from "./ui/drawer";
import { Button } from "./ui/button";
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

  const handleTopChipClick = (chip: ChipProp) => {
    setTopChips(topChips.filter((x) => x.name !== chip.name));
    setBottomChips([...bottomChips, chip]);
    unSelectChip(chip.name);
  };
  const handleBottomChipClick = (chip: ChipProp) => {
    setTopChips([...topChips, chip]);
    setBottomChips(bottomChips.filter((x) => x.name !== chip.name));
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
        {topChips
          .toSorted((a, b) => a.name.localeCompare(b.name))
          .map((c) => (
            <div key={c.name} className="mt-1 cursor-pointer p-[5px] text-lg">
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
      <Drawer modal={false}>
        <DrawerTrigger asChild>
          <Button>Add Tags</Button>
        </DrawerTrigger>
        <DrawerContent className="max-h-64">
          <DrawerTitle>Available tags:</DrawerTitle>
          <ScrollArea className="rounded-md border h-64">
            <div className="flex flex-wrap min-h-[50px]">
              <div key="New" className="mt-1 cursor-pointer p-[5px] text-lg">
                <TagAddModal tagAdded={handleModalClose} />
              </div>
              {bottomChips.concat(dividers)
                .toSorted((a, b) => a.name.localeCompare(b.name))
                .map((c) => (
                  <div key={c.name} className="mt-1 cursor-pointer p-[5px] text-lg">
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
    </div>
  );
}
