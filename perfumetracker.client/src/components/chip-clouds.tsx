"use client";

import { useState } from "react";
import styles from "./chip-clouds.module.css";
import TagAddModal from "./tag-add-modal";
import ColorChip, { ChipProp } from "./color-chip";
import { TagDTO } from "@/dto/TagDTO";
import { Drawer, DrawerContent, DrawerTitle, DrawerTrigger } from "./ui/drawer";
import { Button } from "./ui/button";

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
      { name: tag.tagName, color: tag.color, onChipClick: null, className: "" },
    ]);
  };

  const groupedChips: Record<string, ChipProp[]> = bottomChips.reduce(
    (groups, chip) => {
      const firstLetter = chip.name[0].toUpperCase();
      if (!groups[firstLetter]) {
        groups[firstLetter] = [];
      }
      groups[firstLetter].push(chip);
      return groups;
    },
    {} as Record<string, ChipProp[]>
  );

  return (
    <div className={className}>
      Tags:
      <div className={styles.chipContainer}>
        {topChips
          .toSorted((a, b) => a.name.localeCompare(b.name))
          .map((c) => (
            <div key={c.name} className={styles.chipItem}>
              <ColorChip
                className={c.className}
                color={c.color}
                name={c.name}
                onChipClick={() => handleTopChipClick(c)}
              ></ColorChip>
            </div>
          ))}
      </div>
      <Drawer modal={false}>
        <DrawerTrigger asChild>
          <Button variant="outline">Available tags</Button>
        </DrawerTrigger>
        <DrawerContent className="max-h-128">
          <DrawerTitle>Available tags:</DrawerTitle>
          <div className={styles.chipContainer}>
            <div key="*">
              <ColorChip
                className=""
                color="#FFFFFF"
                name="*"
                onChipClick={null}
              />
              <div key="New" className={styles.chipItem}>
                <TagAddModal tagAdded={handleModalClose}></TagAddModal>
              </div>
            </div>
            {Object.keys(groupedChips)
              .toSorted((a, b) => a.localeCompare(b))
              .map((letter) => (
                <div key={letter}>
                  <ColorChip
                    className=""
                    color="#FFFFFF"
                    name={letter}
                    onChipClick={null}
                  />
                  {groupedChips[letter].map((c) => (
                    <div key={c.name} className={styles.chipItem}>
                      <ColorChip
                        className={c.className}
                        color={c.color}
                        name={c.name}
                        onChipClick={() => handleBottomChipClick(c)}
                      />
                    </div>
                  ))}
                </div>
              ))}
          </div>
        </DrawerContent>
      </Drawer>
    </div>
  );
}
