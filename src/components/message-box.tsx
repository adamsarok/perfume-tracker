'use client';

import { FC, useState } from "react";
import { Button, Input, Modal, ModalBody, ModalContent, ModalFooter, ModalHeader, useDisclosure } from "@nextui-org/react";
import { useFormState } from "react-dom";
import * as tagRepo from "@/db/tag-repo";
import { Tag } from "@prisma/client";
import React from "react";
import { toast } from 'react-toastify';
import { IconProps } from "@/icons/icon-props";

interface MessageBoxProps {
    modalButtonText: string;
    modalButtonColor: "primary" | "default" | "secondary" | "success" | "warning" | "danger" | undefined;
    message: string;
    button1text: string;
    onButton1: (() => void) | null;
    button2text: string;
    onButton2: (() => void) | null;
    startContent: React.ReactNode;
}

export default function MessageBox({ startContent, modalButtonText, modalButtonColor, message, button1text, button2text, onButton1, onButton2 }: MessageBoxProps) {
    const { isOpen, onOpen, onOpenChange } = useDisclosure();
    return (
        <div>
            <Button 
                startContent={startContent}
                onPress={onOpen} 
                color={modalButtonColor}>
                {modalButtonText}</Button>
            <Modal
                isOpen={isOpen}
                onOpenChange={onOpenChange}
                placement="top-center"
            >
                <ModalContent>
                    {(onClose) => (
                        <>
                            <ModalHeader className="flex flex-col gap-1">{message}</ModalHeader>
                            <ModalBody>
                                
                            </ModalBody>
                            <ModalFooter>
                                <Button color="primary" onPress={() => {
                                     if (onButton1) onButton1();
                                     onClose();
                                }}> 
                                    {button1text}
                                </Button>
                                <Button color="danger" variant="flat" onPress={() => {
                                    if (onButton2) onButton2();
                                    onClose();
                                }}>
                                    {button2text}
                                </Button>
                            </ModalFooter>
                        </>
                    )}
                </ModalContent>
            </Modal>
        </div>
    )
}