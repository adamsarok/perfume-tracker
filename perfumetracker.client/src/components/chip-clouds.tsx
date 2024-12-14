"use client";

import { useState } from "react";
import styles from "./chip-clouds.module.css";
import TagAddModal from "./tag-add-modal";
import ColorChip, { ChipProp } from "./color-chip";
import { Card, CardHeader } from "./ui/card";
import { TagDTO } from "@/dto/TagDTO";

export interface ChipCloudProps {
  topChipProps: ChipProp[];
  bottomChipProps: ChipProp[];
  selectChip: (chip: string) => void;
  unSelectChip: (chip: string) => void;
  className: string;
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
          .sort((a, b) => a.name.localeCompare(b.name))
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
      Available tags:
      <div className={styles.chipContainer}>
        <Card>
          <CardHeader>
            <div key="New" className={styles.chipItem}>
              <TagAddModal tagAdded={handleModalClose}></TagAddModal>
            </div>
          </CardHeader>
        </Card>
        {Object.keys(groupedChips)
          .sort()
          .map((letter) => (
            <Card key={letter} className={styles.chipGroup}>
              <CardHeader>
                {letter}
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
              </CardHeader>
            </Card>
          ))}
      </div>
    </div>
  );
}
