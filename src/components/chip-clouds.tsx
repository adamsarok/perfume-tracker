'use client';

import { Chip } from "@nextui-org/chip";
import { useEffect, useState } from "react"
import styles from './chip-clouds.module.css'
import { Button, Input, Modal, ModalBody, ModalContent, ModalFooter, ModalHeader, useDisclosure } from "@nextui-org/react";
import { useFormState } from "react-dom";
import * as actions from "@/app/actions";

export interface ChipCloudProps {
    topChipProps: ChipProp[],
    bottomChipProps: ChipProp[],
    selectChip: any,
    unSelectChip: any
}

export interface ChipProp {
    name: string,
    color: string
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

    const getContrastColor = (bgColor: string) => {
        const color = bgColor.charAt(0) === '#' ? bgColor.substring(1, 7) : bgColor;
        const r = parseInt(color.substring(0, 2), 16);
        const g = parseInt(color.substring(2, 4), 16);
        const b = parseInt(color.substring(4, 6), 16);
        const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
        return luminance > 0.5 ? '#000000' : '#ffffff';
    };

    //todo: separate modal and cleanup
    const { isOpen, onOpen, onOpenChange } = useDisclosure();
    const [formState, addTag] = useFormState(actions.InsertTag, { errors: {}, result: null });
    const [tag, setTag] = useState('');
    const [color, setColor] = useState('');
    const handleTagChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setTag(e.target.value);
    };
    const handleColorChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setColor(e.target.value);
    };
    const handleSave = async (onClose: () => void) => {
        const formData = new FormData();
        formData.append('tag', tag);
        formData.append('color', color);
        console.log(formData);
        try {
            await addTag(formData);
            onClose();
        } catch (error) {
            console.error('Failed to add tag:', error);
            //todo: show error
        }
    };

    useEffect(() => {
        if (formState.result) {
            setBottomChips([...bottomChips, { name: formState.result.tag, color: formState.result.color }]);
        }
    }, [formState.result]);

    return (<div>
        <Modal
            isOpen={isOpen}
            onOpenChange={onOpenChange}
            placement="top-center"
        >
            <ModalContent>
                {(onClose) => (
                    <>
                        <ModalHeader className="flex flex-col gap-1">Log in</ModalHeader>
                        <ModalBody>
                            <Input
                                autoFocus
                                label="Tag"
                                placeholder="Enter tag name"
                                variant="bordered"
                                value={tag}
                                onChange={handleTagChange}
                            />
                            <Input
                                label="Color"
                                placeholder="Select tag color"
                                type="color"
                                value={color}
                                variant="bordered"
                                onChange={handleColorChange}
                            />
                        </ModalBody>
                        <ModalFooter>
                            <Button color="danger" variant="flat" onPress={onClose}>
                                Close
                            </Button>
                            <Button color="primary" onPress={() => handleSave(onClose)}>
                                Save Tag
                            </Button>
                        </ModalFooter>
                    </>
                )}
            </ModalContent>
        </Modal>

        Tags:
        <div className={styles.chipContainer}>
            {topChips.map(c => (
                <div key={c.name} className={styles.chipItem}>
                    <Chip
                        style={{ backgroundColor: c.color, color: getContrastColor(c.color) }}
                        onClick={() => handleTopChipClick(c)}>
                        {c.name}
                    </Chip>
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
            {bottomChips.map(c => (
                <div key={c.name} className={styles.chipItem}>
                    <Chip
                        style={{ backgroundColor: c.color, color: getContrastColor(c.color) }}
                        onClick={() => handleBottomChipClick(c)}>
                        {c.name}
                    </Chip>
                </div>
            ))}
        </div>
    </div>);
}
