"use client";

import React, { useState } from "react";
import { toast } from "react-toastify";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "./ui/table";
import { Input } from "./ui/input";
import { Button } from "./ui/button";
import { TagDTO } from "@/dto/TagDTO";
import { deleteTag, updateTag } from "@/services/tag-service";

export interface TagTableProps {
  tags: TagDTO[];
}

export default function TagTable({ tags }: TagTableProps) {
  const onDelete = async (id: number, tag: string) => {
    const result = await deleteTag(id);
    if (result.ok) {
      tags = tags.filter((x) => x.id != id);
      toast.success(`Tag ${tag} deleted`);
    } else {
      toast.error(result.error);
    }
  };
  const onUpdate = async (id: number, tag: string) => {
    const dto: TagDTO = {
      id,
      tagName: tag,
      color: tagColors[id] || "#ffffff"
    };
    const result = await updateTag(dto);
    if (result.ok) {
      toast.success(`Tag ${tag} updated`);
    } else {
      toast.error(result.error);
    }
  };
  const [tagColors, setTagColors] = useState<Record<number, string>>(tags.reduce((acc, tag) => {
    acc[tag.id] = tag.color;
    return acc;
  }, {} as Record<number, string>));

  const handleColorChange = (id: number, color: string) => {
    setTagColors(prev => ({
      ...prev,
      [id]: color,
    }));
  };

  
  return (
    <div>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead key="tag">Tag</TableHead>
            <TableHead key="color" className="w-[20%]">
              Color
            </TableHead>
            <TableHead key="update">Update</TableHead>
            <TableHead key="delete">Delete</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {tags.map((tag) => (
            <TableRow key={tag.id}>
              <TableCell className="font-medium">{tag.tagName}</TableCell>
              <TableCell typeof="color">
                <Input
                value={tagColors[tag.id]}
                onChange={(e) => handleColorChange(tag.id, e.target.value)}
                  type="color"
                ></Input>
              </TableCell>
              <TableCell>
                <Button
                  color="primary"
                  onClick={() =>
                    onUpdate(tag.id, tag.tagName)
                  }
                >
                  Update
                </Button>
              </TableCell>
              <TableCell>
                <Button onClick={() => onDelete(tag.id, tag.tagName)}>
                  Delete
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
