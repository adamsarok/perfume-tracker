'use client';

import { Chip } from "@nextui-org/chip";
import { useEffect, useState } from "react"
import styles from './chip-clouds.module.css'
import { getContrastColor } from "@/app/colors";
import TagAddModal from "./tag-add-modal";
import { Tag } from "@prisma/client";
import { useDisclosure } from "@nextui-org/react";
import ColorChip, { ChipProp } from "./color-chip";

export interface ChipCloudProps {
    topChipProps: ChipProp[],
    bottomChipProps: ChipProp[],
    selectChip: any,
    unSelectChip: any
}


export default function ChipClouds({ topChipProps, bottomChipProps, selectChip, unSelectChip }: ChipCloudProps) {
    const [topChips, setTopChips] = useState(topChipProps);
    const [bottomChips, setBottomChips] = useState(bottomChipProps);

    const handleTopChipClick = (chip: ChipProp) => {
        setTopChips(topChips.filter(x => x.name !== chip.name));
        setBottomChips([...bottomChips, chip]);
        unSelectChip(chip.name);
    }
    const handleBottomChipClick = (chip: ChipProp) => {
        setTopChips([...topChips, chip]);
        setBottomChips(bottomChips.filter(x => x.name !== chip.name));
        selectChip(chip.name);
    }

    const handleModalClose = (tag: Tag) => {
        setBottomChips([...bottomChips, { name: tag.tag, color: tag.color, onChipClick: null, className: "" }]);
    }

    const { isOpen, onOpen, onOpenChange } = useDisclosure();


    return (<div>
        <TagAddModal 
            tagAdded={handleModalClose} 
            isOpen={isOpen}
            onOpen={onOpen}
            onOpenChange={onOpenChange}></TagAddModal>
        Tags:
        <div className={styles.chipContainer}>
            {topChips.sort((a, b) => a.name.localeCompare(b.name)).map(c => (
                <div key={c.name} className={styles.chipItem}>
                    <ColorChip className={c.className} color={c.color} name={c.name} onChipClick={() => handleTopChipClick(c)}></ColorChip>
                </div>
            ))}
        </div>
        Available tags:
        <div className={styles.chipContainer}>
            <div key='New' className={styles.chipItem}>
                <Chip
                    style={{ backgroundColor: '#52d91f', color: '#000000' }}
                    onClick={onOpen}>
                    New
                </Chip>
            </div>
            {bottomChips.sort((a, b) => a.name.localeCompare(b.name)).map(c => (
                <div key={c.name} className={styles.chipItem}>
                    <ColorChip className={c.className} color={c.color} name={c.name} onChipClick={() => handleBottomChipClick(c)}></ColorChip>
                </div>
            ))}
        </div>
    </div>);
}
