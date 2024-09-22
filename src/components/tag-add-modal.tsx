'use client';

import { useState } from "react";
import { Button, Input, Modal, ModalBody, ModalContent, ModalFooter, ModalHeader } from "@nextui-org/react";
import { useFormState } from "react-dom";
import * as tagRepo from "@/db/tag-repo";
import { Tag } from "@prisma/client";
import React from "react";
import { toast } from 'react-toastify';

interface TagAddModalProps {
    tagAdded: (tag: Tag) => void;
    isOpen: boolean;
    onOpen: () => void;
    onOpenChange: () => void;
}

export default function TagAddModal({ tagAdded, isOpen, onOpenChange }: TagAddModalProps) {
    const [formState, addTag] = useFormState(tagRepo.insertTag, { errors: {}, result: null });
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
        await addTag(formData);
        if (formState.result) {
            tagAdded({
                id: formState.result.id,
                tag: formState.result.tag,
                color: formState.result.color
            });
        }
        //TODO first add shows failure even if add is successful
        if (formState.result) toast.success("Tag added!");
        else {
            if (formState.errors._form && formState.errors._form.length > 0) {
                const errorMessages = formState.errors._form.join(', ');
                toast.error(errorMessages);
            } else toast.error("Tag add failed")
        }
        onClose();
    };
    return (
        <div>
            <Modal
                isOpen={isOpen}
                onOpenChange={onOpenChange}
                placement="top-center"
            >
                <ModalContent>
                    {(onClose) => (
                        <>
                            <ModalHeader className="flex flex-col gap-1">Create Tag</ModalHeader>
                            <ModalBody>
                                <Input
                                    autoFocus
                                    label="Tag"
                                    placeholder="Enter tag name"
                                    variant="bordered"
                                    value={tag}
                                    onChange={handleTagChange}
                                    isInvalid={!!formState.errors.name}
                                    errorMessage={formState.errors.name?.join(',')}
                                />
                                <Input
                                    label="Color"
                                    placeholder="Select tag color"
                                    type="color"
                                    value={color}
                                    variant="bordered"
                                    onChange={handleColorChange}
                                    isInvalid={!!formState.errors.color}
                                    errorMessage={formState.errors.color?.join(',')}
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
        </div>
    )
}