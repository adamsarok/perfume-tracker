'use client';

import { PerfumeWornDTO } from "@/app/actions";
import { Table, TableBody, TableCell, TableColumn, TableHeader, TableRow } from "@nextui-org/react";

export interface PerfumeWornTableProps {
    perfumes: PerfumeWornDTO[]
}
export default function PerfumeWornTable({perfumes}: PerfumeWornTableProps) {
    return <div>
    <Table isStriped>
            <TableHeader>
                <TableColumn>House</TableColumn>
                <TableColumn>Perfume</TableColumn>
                <TableColumn>Worn X times</TableColumn>
                <TableColumn>Last worn</TableColumn>
            </TableHeader>
            <TableBody items={perfumes}>
                {(perfume) => (
                    <TableRow key={perfume.perfumeId}>
                        <TableCell>{perfume.house}</TableCell>
                        <TableCell>{perfume.perfume}</TableCell>
                        <TableCell>{perfume.wornTimes}</TableCell>
                        <TableCell>{perfume.lastWorn?.toDateString()}</TableCell>
                    </TableRow>
                )}
            </TableBody>
        </Table>
    </div>;
}