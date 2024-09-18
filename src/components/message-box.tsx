'use client';
import { Button, Modal, ModalBody, ModalContent, ModalFooter, ModalHeader, useDisclosure } from "@nextui-org/react";
import React from "react";

interface MessageBoxProps {
    className: string
    modalButtonText: string
    modalButtonColor: "primary" | "default" | "secondary" | "success" | "warning" | "danger" | undefined
    message: string
    button1text: string
    onButton1: (() => void) | null
    button2text: string
    onButton2: (() => void) | null
    startContent: React.ReactNode
}

export default function MessageBox({ className, startContent, modalButtonText, modalButtonColor, message, button1text, button2text, onButton1, onButton2 }: MessageBoxProps) {
    const { isOpen, onOpen, onOpenChange } = useDisclosure();
    return (
        <div>
            <Button className={className}
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