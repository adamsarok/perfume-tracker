import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { TagStatDTO } from "@/dto/TagStatDTO";
import { ColumnDef } from "@tanstack/react-table";
import { ArrowUpDown } from "lucide-react";
import { getContrastColor } from "../colors";

export const TagSelectColumns: ColumnDef<TagStatDTO>[] = [
  {
    id: "select",
    header: ({ table }) => (
      <Checkbox
        checked={
          table.getIsAllPageRowsSelected() ||
          (table.getIsSomePageRowsSelected() && "indeterminate")
        }
        onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)}
        aria-label="Select all"
      />
    ),
    cell: ({ row }) => (
      <Checkbox
        checked={row.getIsSelected()}
        onCheckedChange={(value) => row.toggleSelected(!!value)}
        aria-label="Select row"
      />
    ),
    enableSorting: false,
    enableHiding: false,
  },
  {
    accessorKey: "tagName",
    header: "Name",
    cell: ({ row }) => (
      <div
        className="capitalize"
        style={{
          backgroundColor: row.original.color,
          color: getContrastColor(row.original.color),
        }}
      >
        {row.getValue("tagName")}
      </div>
    ),
  },
  {
    accessorKey: "wornTimes",
    header: ({ column }) => {
      return (
        <Button
          variant="ghost"
          onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
        >
          Worn X Times
          <ArrowUpDown />
        </Button>
      );
    },
    cell: ({ row }) => <div className="lowercase">{row.getValue("wornTimes")}</div>,
  },
  {
    id: "actions",
    enableHiding: false,
    //cell: ({ row }) => {
    //const payment = row.original

    // return (
    //   <DropdownMenu>
    //     <DropdownMenuTrigger asChild>
    //       <Button variant="ghost" className="h-8 w-8 p-0">
    //         <span className="sr-only">Open menu</span>
    //         <MoreHorizontal />
    //       </Button>
    //     </DropdownMenuTrigger>
    //     <DropdownMenuContent align="end">
    //       <DropdownMenuLabel>Actions</DropdownMenuLabel>
    //       <DropdownMenuItem
    //         onClick={() => navigator.clipboard.writeText(payment.id)}
    //       >
    //         Copy payment ID
    //       </DropdownMenuItem>
    //       <DropdownMenuSeparator />
    //       <DropdownMenuItem>View customer</DropdownMenuItem>
    //       <DropdownMenuItem>View payment details</DropdownMenuItem>
    //     </DropdownMenuContent>
    //   </DropdownMenu>
    // )
    //},
  },
];
