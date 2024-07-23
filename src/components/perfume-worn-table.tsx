'use client';

import { PerfumeWornDTO } from "@/app/actions";
import {  Checkbox, Divider, Link, Spinner, Table, TableBody, TableCell, TableColumn, TableHeader, TableRow, getKeyValue } from "@nextui-org/react";
import { useEffect, useState } from "react";
import {useAsyncList} from "@react-stately/data";
import React from "react";
import { set } from "zod";

export interface PerfumeWornTableProps {
    perfumes: PerfumeWornDTO[]
}

function Reload({ perfumes }: PerfumeWornTableProps) {
    
}

export default function PerfumeWornTable({ perfumes }: PerfumeWornTableProps) {
    let list = useAsyncList({
        async load({signal}) {
            //console.log('isSelected in load:', isSelected);
            return {
                items: (isGoodStuff ? 
                    perfumes.filter((x) => x.perfume.rating >=8 && x.perfume.ml > 0) :
                    isBuyStuff ? perfumes.filter((x) => x.perfume.rating >=8 && x.perfume.ml <= 0) :
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

    //should be combobox TODO

    const [isGoodStuff, setIsGoodStuff] = React.useState(true);
    const [isBuyStuff, setBuyStuff] = React.useState(false);

    useEffect(() => {
        list.reload();
    }, [isGoodStuff]); // Reload the list whenever isSelected changes
    useEffect(() => {
        list.reload();
    }, [isBuyStuff]); // Reload the list whenever isSelected changes


    return <div>
        <Checkbox className="ml-2 mr-6" isSelected={isGoodStuff} onValueChange={setIsGoodStuff}>
            Show only good stuff
        </Checkbox>
        <Checkbox isSelected={isBuyStuff} onValueChange={setBuyStuff}>
            Buy list
        </Checkbox>
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