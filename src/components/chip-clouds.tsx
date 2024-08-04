import { Chip } from "@nextui-org/chip";
import { Divider, Input, select } from "@nextui-org/react";
import { useState } from "react"
import styles from './chip-clouds.module.css'
import { color } from "framer-motion";

export interface ChipCloudProps {
    topChipProps: ChipProp[],
    bottomChipProps: ChipProp[],
    selectChip: any, //should be a func passed from parunt
    unSelectChip: any
}

export interface ChipProp {
    //id: number,
    name: string,
    color: string
}

export default function ChipClouds({topChipProps, bottomChipProps, selectChip, unSelectChip}: ChipCloudProps) {
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
    const getColor = (color: string) => {
        switch (color) {
            case "Red": return styles.chipRed;
            case "Blue": return styles.chipBlue;
            case "Black": return styles.chipBlack;
            case "Orange": return styles.chipOrange;
            case "Yellow": return styles.chipYellow;
            case "Brown": return styles.chipBrown;
            case "DarkBrown": return styles.chipDarkBrown;
            case "Pink": return styles.chipPink;
        }
    }
    return (<div>
        Tags: 
        <div className={styles.chipContainer}>
            {/* <div className={styles.chipItem}>
                <Chip key='newTag' 
                    className={styles.chipGreen}
                    onClick={() => handleNewTag(c)}
                    >New Tag</Chip>
            </div> */}
            {topChips.map(c => (
                <div key={c.name} className={styles.chipItem}>
                    <Chip //key={c.name}
                        className={getColor(c.color)}
                        onClick={() => handleTopChipClick(c)}>
                        {c.name}
                    </Chip>
                </div>
            ))}
        </div>
        Available tags:
        <div className={styles.chipContainer}>
        {bottomChips.map(c => (
            <div key={c.name} className={styles.chipItem}>
            <Chip //key={c.name}
                className={getColor(c.color)}
                onClick={() => handleBottomChipClick(c)}>
                    {c.name}
            </Chip>
            </div>
        ))}
        </div>        
    </div>);
}
