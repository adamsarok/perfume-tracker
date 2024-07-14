'use client';

import { PerfumeWornDTO } from "@/app/actions";
import {  Checkbox, Spinner, Table, TableBody, TableCell, TableColumn, TableHeader, TableRow, getKeyValue } from "@nextui-org/react";
import { useState } from "react";
import {useAsyncList} from "@react-stately/data";
import React from "react";

export interface PerfumeWornTableProps {
    perfumes: PerfumeWornDTO[]
}
export default function PerfumeWornTable({ perfumes }: PerfumeWornTableProps) {
    const [isSelected, setIsSelected] = React.useState(true);
    const handleCheckboxChange = (newValue: boolean) => {
      setIsSelected(newValue);
      
      //TODO: how to I reload the table?
      //perfumes = await actions.GetAllPerfumesWithWearCount(isSelected);
    }

    
    let list = useAsyncList({
        async load({signal}) {
            return {
                items: (isSelected ? 
                    perfumes.filter((x) => x.perfume.rating >=8 && x.perfume.ml > 0) : 
                    perfumes)
            };
        },
        async sort({items, sortDescriptor}: {items: Array<any>, sortDescriptor: any}) {
        return {
            items: items.sort((a, b) => {
                console.log(sortDescriptor.column);
                let first = a[sortDescriptor.column];
                let second = b[sortDescriptor.column];
                console.log(first);
                console.log(second);
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


    return <div>
        <Checkbox isSelected={isSelected} onValueChange={setIsSelected}>
            Show only good stuff
        </Checkbox>
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
                    <TableRow key={perfume.perfume.id}>
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