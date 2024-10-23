"use client";
import React from "react";
import { Button } from "./ui/button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";

interface MessageBoxProps {
  className: string;
  modalButtonText: string;
  modalButtonColor:
    | "primary"
    | "default"
    | "secondary"
    | "success"
    | "warning"
    | "danger"
    | undefined;
  message: string;
  button1text: string;
  onButton1: (() => void) | null;
  button2text: string;
  onButton2: (() => void) | null;
  startContent: React.ReactNode;
}

export default function MessageBox({
  className,
  startContent,
  modalButtonText,
  modalButtonColor,
  message,
  button1text,
  button2text,
  onButton1,
  onButton2,
}: MessageBoxProps) {
  return (
    <div>
      <Dialog>
        <DialogTrigger>
          <Button
            type="button"
            className={className}
            color={modalButtonColor}
          >
            {startContent} {modalButtonText}
          </Button>
        </DialogTrigger>

          <DialogContent>
            <DialogHeader className="flex flsex-col gap-1">
              <DialogTitle>{message}</DialogTitle>
              <DialogDescription>
                <DialogClose asChild>
                <Button
                    color="primary"
                    type="button"
                    onClick={() => {
                      if (onButton1) onButton1();
                    }}
                  >
                    {button1text}
                  </Button>
                </DialogClose>
                <DialogClose>
                  <Button
                    color="danger"
                    type="button"
                    onClick={() => {
                      if (onButton2) onButton2();
                    }}
                  >
                    {button2text}
                  </Button>
                  </DialogClose>
              </DialogDescription>
            </DialogHeader>
          </DialogContent>
      </Dialog>
    </div>
  );
}
