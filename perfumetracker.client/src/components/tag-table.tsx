"use client";

import React, { useEffect, useState } from "react";
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
import { deleteTag, getTags, updateTag } from "@/services/tag-service";
import { showError, showSuccess } from "@/services/toasty-service";

export default function TagTable() {
  const [tags, setTags] = useState<TagDTO[]>([]);
  const [tagColors, setTagColors] = useState<Record<string, string>>({});
  useEffect(() => {
    const fetchData = async () => {
      const fetchedTags = await getTags();
      fetchedTags.sort((a, b) => a.tagName.localeCompare(b.tagName));
      setTags(fetchedTags);
      const initialTagColors = fetchedTags.reduce((acc, tag) => {
        acc[tag.id] = tag.color;
        return acc;
      }, {} as Record<string, string>);
      setTagColors(initialTagColors);
    };
    fetchData();
  }, []);

  const onDelete = async (id: string, tag: string) => {
    const result = await deleteTag(id);
    if (result.ok) {
      setTags(tags.filter((x) => x.id != id));
      showSuccess(`Tag ${tag} deleted`);
    } else {
      showError("Tag add failed", result.error);
    }
  };
  const onUpdate = async (id: string, tag: string) => {
    const dto: TagDTO = {
      id,
      tagName: tag,
      color: tagColors[id] || "#ffffff",
    };
    const result = await updateTag(dto);
    if (result.ok) {
      showSuccess(`Tag ${tag} updated`);
    } else {
      showError("Tag update failed", result.error);
    }
  };

  const handleColorChange = (id: string, color: string) => {
    setTagColors((prev) => ({
      ...prev,
      [id]: color,
    }));
  };

  if (!tags || !tagColors) {
    return <div>Loading...</div>;
  }

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
                  onClick={() => onUpdate(tag.id, tag.tagName)}
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
