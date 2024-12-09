"use client";

import React from "react";
import * as tagRepo from "@/db/tag-repo";
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

export interface TagTableProps {
  tags: TagDTO[];
}

export default function TagTable({ tags }: TagTableProps) {
  const onDelete = async (id: number, tag: string) => {
    const result = await tagRepo.deleteTag(id);
    if (result.success) {
      tags = tags.filter((x) => x.id != id);
      toast.success(`Tag ${tag} deleted`);
    } else {
      toast.error(result.error);
    }
  };
  const onUpdate = async (id: number, tag: string, color: string) => {
    const result = await tagRepo.updateTag(id, tag, color);
    if (result.success) {
      //TODO: doesn't work, also refresh
      toast.success(`Tag ${tag} updated`);
    } else {
      toast.error(result.error);
    }
  };
  return (
    <div>
      <Table>
        {/* <TableCaption>ayooo</TableCaption> */}
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
                <Input defaultValue={tag.color} type="color"></Input>
              </TableCell>
              <TableCell>
                <Button
                  color="primary"
                  onClick={() => onUpdate(tag.id, tag.tagName, tag.color)}
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
