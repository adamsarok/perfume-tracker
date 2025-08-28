"use client";

import React from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Badge } from "./ui/badge";
import { Input } from "./ui/input";
import { Button } from "./ui/button";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "./ui/form";
import { TagDTO } from "@/dto/TagDTO";
import { addTag } from "@/services/tag-service";
import { showError, showSuccess } from "@/services/toasty-service";

interface TagAddModalProps {
  tagAdded: (tag: TagDTO) => void;
}

const formSchema = z.object({
  tag: z
    .string()
    .trim()
    .min(1, {
      message: "Tag must be at least 1 character.",
    }),
  color: z
    .string()
    .trim()
    .min(1, {
      message: "Color must be at least 1 character.",
    }),
});

export default function TagAddModal({ tagAdded }: TagAddModalProps) {
  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      tag: "",
      color: "",
    },
  });

  async function onSubmit(values: z.infer<typeof formSchema>) {
    const tag: TagDTO = {
      id: "",
      tagName: values.tag,
      color: values.color,
    };
    const result = await addTag(tag);
    if (result.error || !result.data) {
      showError("Tag add failed", result.error ?? "unknown error");
      return;
    }
    tag.id = result.data.id;
    showSuccess("Tag add successful");
    tagAdded(tag);
  }

  return (
    <div>
      <Dialog>
        <DialogTrigger>
          <Badge style={{ backgroundColor: "#52d91f", color: "#000000" }}>
            New
          </Badge>
        </DialogTrigger>
        <DialogContent>
          <DialogHeader className="flex flex-col gap-1">
            <DialogTitle>Create Tag</DialogTitle>
            <DialogDescription asChild>
              <Form {...form}>
                <form onSubmit={form.handleSubmit(onSubmit)}>
                  <FormField
                    control={form.control}
                    name="tag"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Tag</FormLabel>
                        <FormControl>
                          <Input placeholder="Tag" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="color"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Color</FormLabel>
                        <FormControl>
                          <Input type="color" placeholder="Color" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <Button type="submit">Add</Button>
                </form>
              </Form>
            </DialogDescription>
          </DialogHeader>
        </DialogContent>
      </Dialog>
    </div>
  );
}
