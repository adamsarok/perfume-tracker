import { Chip } from "@nextui-org/chip";
import { Divider } from "@nextui-org/react";
import { useState } from "react"
import styles from './chip-clouds.module.css'

export interface ChipCloudProps {
    topChipProps: ChipProp[],
    bottomChipProps: ChipProp[] 
}

export interface ChipProp {
    id: number,
    label: string
}

export default function ChipClouds({topChipProps, bottomChipProps}: ChipCloudProps) {
    const [topChips, setTopChips] = useState(topChipProps);
    const [bottomChips, setBottomChips] = useState(bottomChipProps);

    const handleTopChipClick = (chip: ChipProp) => {
        setTopChips(topChips.filter(x => x.id !== chip.id));
        setBottomChips([...bottomChips, chip]);
    }
    const handleBottomChipClick = (chip: ChipProp) => {
        setTopChips([...topChips, chip]);
        setBottomChips(bottomChips.filter(x => x.id !== chip.id));
    }
    //const test = styles.chip-container;
    //todo color!!!
    return (<div>
        <div className={styles.chipContainer}>
        {topChips.map(c => (
            <div className={styles.chipItem}>
                <Chip key={c.id}
                    className={styles.chipRed}
                    onClick={() => handleTopChipClick(c)}>
                    {c.label}
                </Chip>
            </div>
        ))} 
        </div>
        <Divider></Divider>
        <div className={styles.chipContainer}>
        {bottomChips.map(c => (
            <div className={styles.chipItem}>
            <Chip key={c.id}
                className={styles.chipGreen}
                onClick={() => handleBottomChipClick(c)}>
                    {c.label}
            </Chip>
            </div>
        ))}
        </div>        
    </div>);
}
