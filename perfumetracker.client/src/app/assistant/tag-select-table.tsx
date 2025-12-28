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
import { TagSelectColumns } from "./tag-select-columns";
import { TagStatDTO } from "@/dto/TagStatDTO";
import TableSelect from "@/components/ui/table-select";

export interface TagSelectTableProps {
  readonly tags: TagStatDTO[];
  readonly onSelectionChange: (selectedRows: TagStatDTO[]) => void;
}

export function TagSelectTable({ tags, onSelectionChange }: TagSelectTableProps) {
  const defaultSorting: SortingState = [{ id: "wornTimes", desc: true }];
  const [sorting, setSorting] = React.useState<SortingState>(defaultSorting);
  const [columnFilters, setColumnFilters] = React.useState<ColumnFiltersState>(
    []
  );
  const [rowSelection, setRowSelection] = React.useState({});

  const table = useReactTable({
    data: tags,
    columns: TagSelectColumns,
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
    const selectedRows = table
      .getSelectedRowModel()
      .rows.map((row) => row.original);
    onSelectionChange(selectedRows);
  }, [rowSelection, onSelectionChange, table]);

  return (
    <TableSelect<TagStatDTO> table={table} numberOfColumns={TagSelectColumns.length}></TableSelect>
  );
}
