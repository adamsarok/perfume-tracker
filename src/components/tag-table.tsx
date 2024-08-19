'use client';

import { Button, Checkbox, Divider, Input, Link, Spinner, Table, TableBody, TableCell, TableColumn, TableHeader, TableRow, getKeyValue } from "@nextui-org/react";
import { Tag } from "@prisma/client";
import React from "react";
import * as tagRepo from "@/db/tag-repo";
import { toast } from "react-toastify";

export interface TagTableProps {
    tags: Tag[]
}

// function Reload({ perfumes }: PerfumeWornTableProps) {

// }

export default function TagTable({ tags }: TagTableProps) {

    const onDelete = async (id: number, tag: string) => {
        const result = await tagRepo.deleteTag(id);
        if (result.success) {
            tags = tags.filter(x => x.id != id);
            toast.success(`Tag ${tag} deleted`);
        } else {
            toast.error(result.error);
        }
    }
    const onUpdate = async (id: number, tag: string, color: string) => {
        toast.error("TODO implement")
        // const result = await tagRepo.updateTag(id, tag, color);
        // if (result.success) {
        //     //refresh
        //     toast.success(`Tag ${tag} deleted`);
        // } else {
        //     toast.error(result.error);
        // }
    }
    return <div>
        <Table isStriped
        >
            <TableHeader>
                <TableColumn key="tag">Tag</TableColumn>
                <TableColumn key="color" width='20%'>Color</TableColumn>
                <TableColumn key="update">Actions</TableColumn>
            </TableHeader>
            <TableBody items={tags as Tag[]}>
                {(tag: Tag) => (
                    <TableRow key={tag.id}>
                        <TableCell><Input defaultValue={tag.tag}></Input></TableCell>
                        <TableCell ><Input defaultValue={tag.color} type="color"></Input></TableCell>
                        <TableCell className=""><Button color="primary"
                            onPress={() => onUpdate(tag.id, tag.tag, tag.color)}>Update</Button>
                            <Button className="ml-8" color="danger"
                            onPress={() => onDelete(tag.id, tag.tag)}>Delete</Button></TableCell>
                    </TableRow>
                )}
            </TableBody>
        </Table>
    </div>;
}