import {
  ColumnFiltersState,
  getCoreRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  getSortedRowModel,
  SortingState,
  useReactTable,
} from "@tanstack/react-table";
import React, { useEffect } from "react";
import { PerfumeSelectColumns, PerfumeSelectDto } from "./perfume-select-columns";
import TableSelect from "@/components/ui/table-select";

export interface PerfumeSelectTableProps {
  readonly perfumes: PerfumeSelectDto[];
  readonly onSelectionChange: (selectedRows: PerfumeSelectDto[]) => void;
}

export function PerfumeSelectTable({ perfumes, onSelectionChange }: PerfumeSelectTableProps) {
  const defaultSorting: SortingState = [{ id: "wornTimes", desc: true }];
  const [sorting, setSorting] = React.useState<SortingState>(defaultSorting);
  const [columnFilters, setColumnFilters] = React.useState<ColumnFiltersState>(
    []
  );
  const [rowSelection, setRowSelection] = React.useState({});
  
  const table = useReactTable({
    data: perfumes,
    columns: PerfumeSelectColumns,
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    getCoreRowModel: getCoreRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    onRowSelectionChange: setRowSelection,
    state: {
      sorting,
      columnFilters,
      rowSelection,
    },
  });

  useEffect(() => {
    const selectedRows = table.getSelectedRowModel().rows.map(row => row.original);
    onSelectionChange(selectedRows);
  }, [rowSelection, onSelectionChange, table]);

  return (
    <TableSelect<PerfumeSelectDto> table={table} numberOfColumns={PerfumeSelectColumns.length}></TableSelect>
  );
}
