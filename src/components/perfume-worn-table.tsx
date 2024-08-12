'use client';


import { Checkbox, Divider, Link, Spinner, Table, TableBody, TableCell, TableColumn, TableHeader, TableRow, getKeyValue } from "@nextui-org/react";
import { useEffect, useState } from "react";
import { useAsyncList } from "@react-stately/data";
import React from "react";
import { PerfumeWornDTO } from "@/db/perfume-worn-repo";
import {RadioGroup, Radio} from "@nextui-org/radio";
import { Perfume } from "@prisma/client";

export interface PerfumeWornTableProps {
    perfumes: PerfumeWornDTO[]
}

function Reload({ perfumes }: PerfumeWornTableProps) {

}

export default function PerfumeWornTable({ perfumes }: PerfumeWornTableProps) {
    let list = useAsyncList({
        async load({ signal }) {
            let items: PerfumeWornDTO[];
            switch(selected) {
                case 'good-stuff':
                    items = perfumes.filter((x) => x.perfume.rating >= 8 && x.perfume.ml > 0);
                    break;
                case 'buy-list':
                    items = perfumes.filter((x) => x.perfume.rating >= 8 && x.perfume.ml <= 0);
                    break;
                case 'untagged':
                    items = perfumes.filter((x) => x.tags.length == 0 && x.perfume.rating >= 8 && x.perfume.ml > 0);
                    break;
                default:
                    items = perfumes;
                    break;
            }
            return {
                items: items
            };
        },
        async sort({ items, sortDescriptor }: { items: Array<any>, sortDescriptor: any }) {
            return {
                items: items.sort((a, b) => {
                    let first = a[sortDescriptor.column];
                    let second = b[sortDescriptor.column];
                    if (!first && !second) return 0;
                    if (!first) return 1;
                    if (!second) return -1;
                    let cmp = (parseInt(first) || first) < (parseInt(second) || second) ? -1 : 1;
                    if (sortDescriptor.direction === "descending") {
                        cmp *= -1;
                    }
                    return cmp;
                }),
            };
        },
    });

    const [selected, setSelected] = React.useState("good-stuff");

    useEffect(() => {
        list.reload();
    }, [selected]);

    return <div>
        <RadioGroup
            label="Filtering"
            orientation="horizontal"
            onValueChange={setSelected}
        >
            <Radio value="good-stuff">Show only good stuff</Radio>
            <Radio value="buy-list">Buy list</Radio>
            <Radio value="untagged">Untagged</Radio>
            <Radio value="all">All</Radio>
        </RadioGroup>
    
        <Divider></Divider>
        <Table isStriped
            sortDescriptor={list.sortDescriptor}
            onSortChange={list.sort}
        >
            <TableHeader>
                <TableColumn key="house" allowsSorting>House</TableColumn>
                <TableColumn key="perfume" allowsSorting>Perfume</TableColumn>
                <TableColumn key="rating" allowsSorting>Rating</TableColumn>
                <TableColumn key="wornTimes" allowsSorting>Worn X times</TableColumn>
                <TableColumn key="lastWorn" allowsSorting>Last worn</TableColumn>
            </TableHeader>
            <TableBody items={list.items as PerfumeWornDTO[]}>
                {(perfume: PerfumeWornDTO) => (
                    <TableRow key={perfume.perfume.id} href={`/perfumes/${perfume.perfume.id}/`}>
                        <TableCell>{perfume.perfume.house}</TableCell>
                        <TableCell>{perfume.perfume.perfume}</TableCell>
                        <TableCell>{perfume.perfume.rating}</TableCell>
                        <TableCell>{perfume.wornTimes}</TableCell>
                        <TableCell>{perfume.lastWorn?.toDateString()}</TableCell>
                    </TableRow>
                )}
            </TableBody>
        </Table>
    </div>;
}