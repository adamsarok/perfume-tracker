"use client";

import React, { useState } from "react";
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
import { showError, showSuccess } from "@/services/toasty-service";

export interface TagTableProps {
  tags: TagDTO[];
}

export default function TagTable({ tags }: TagTableProps) {
  const onDelete = async (id: string, tag: string) => {
    const result = await deleteTag(id);
    if (result.ok) {
      tags = tags.filter((x) => x.id != id);
      showSuccess(`Tag ${tag} deleted`);
    } else {
      showError('Tag add failed', result.error);
    }
  };
  const onUpdate = async (id: string, tag: string) => {
    const dto: TagDTO = {
      id,
      tagName: tag,
      color: tagColors[id] || "#ffffff"
    };
    const result = await updateTag(dto);
    if (result.ok) {
      showSuccess(`Tag ${tag} updated`);
    } else {
      showError('Tag update failed', result.error);
    }
  };
  const [tagColors, setTagColors] = useState<Record<string, string>>(tags.reduce((acc, tag) => {
    acc[tag.id] = tag.color;
    return acc;
  }, {} as Record<string, string>));

  const handleColorChange = (id: string, color: string) => {
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
