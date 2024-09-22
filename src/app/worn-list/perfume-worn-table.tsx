'use client';


import { Divider, Table, TableBody, TableCell, TableColumn, TableHeader, TableRow } from "@nextui-org/react";
import { useEffect, useState } from "react";
import { useAsyncList } from "@react-stately/data";
import React from "react";
import { PerfumeWornDTO } from "@/db/perfume-worn-repo";
import {RadioGroup, Radio} from "@nextui-org/radio";
import { Tag } from "@prisma/client";
import ChipClouds from "../../components/chip-clouds";
import { ChipProp } from "../../components/color-chip";

export interface PerfumeWornTableProps {
    perfumes: PerfumeWornDTO[],
    allTags: Tag[]
}

export default function PerfumeWornTable({ perfumes, allTags }: PerfumeWornTableProps) {
    const list = useAsyncList({
        async load() {
            let items: PerfumeWornDTO[];
            switch(selected) {
                case 'good-stuff':
                    items = perfumes.filter((x) => x.perfume.rating >= 8 && x.perfume.ml > 0);
                    break;
                case 'buy-list':
                    items = perfumes.filter((x) => x.perfume.rating >= 8 && x.perfume.ml <= 0);
                    break;
                case 'untagged':
                    items = perfumes.filter((x) => x.tags.length === 0 && x.perfume.rating >= 8 && x.perfume.ml > 0);
                    break;
                case 'tag-filter':
                    items = perfumes.filter((perfume) => {
                        return (
                            perfume.perfume.rating >= 8 &&
                            perfume.perfume.ml > 0 &&
                            tags.every((tag) => perfume.tags.some((perfumeTag) => perfumeTag.id === tag.id))
                        );
                    });
                    break;
                default:
                    items = perfumes;
                    break;
            }
            return {
                items: items
            };
        },
        /* eslint-disable @typescript-eslint/no-explicit-any */
        async sort({ items, sortDescriptor }: { items: any[], sortDescriptor: any }) { //TODO figure this out later
            return {
                items: items.sort((a, b) => {
                    const first = a[sortDescriptor.column];
                    const second = b[sortDescriptor.column];
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
        /* eslint-enable @typescript-eslint/no-explicit-any */
    });

    const [selected, setSelected] = React.useState("good-stuff");
    const [isChipCloudVisible, setIsChipCloudVisible] = useState(true);
    const [tags, setTags] = useState<Tag[]>([]);
    const selectChip = (chip: string) => {
        const tag = allTags.find(x => x.tag === chip);
        if (tag) setTags([...tags, tag]);
    };
    const unSelectChip = (chip: string) => {
        setTags((tags: Tag[]) => tags.filter(x => x.tag != chip));
    };

    useEffect(() => {
        list.reload();
        setIsChipCloudVisible(selected === "tag-filter");
    }, [selected]);

    useEffect(() => {
        list.reload();
    }, [tags]);

    const bottomChipProps: ChipProp[] = [];
    allTags.map(allTag => {
        bottomChipProps.push({
            name: allTag.tag,
            color: allTag.color,
            className: "",
            onChipClick: null
        })
    });

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
            <Radio value="tag-filter">Filter by tags</Radio>
        </RadioGroup>
        {isChipCloudVisible && <ChipClouds className="" topChipProps={[]} bottomChipProps={bottomChipProps} selectChip={selectChip} unSelectChip={unSelectChip} />}
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