import { Button } from "@/components/ui/button";
import { PerfumeRatingDownloadDTO } from "@/dto/PerfumeRatingDownloadDTO";
import { ColumnDef } from "@tanstack/react-table";
import { ArrowUpDown } from "lucide-react";

export const PerfumeRatingColumns: ColumnDef<PerfumeRatingDownloadDTO>[] = [
    {
        accessorKey: "comment",
        header: ({ column }) => {
            return (
                <Button
                    variant="ghost"
                    onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
                >
                    Comment
                    <ArrowUpDown className="ml-2 h-4 w-4" />
                </Button>
            );
        },
        cell: ({ row }) => {
            return <span>{row.original.comment}</span>
        },
    },
    {
        accessorKey: "rating",
        header: ({ column }) => {
            return (
                <Button
                    variant="ghost"
                    onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
                >
                    Rating
                    <ArrowUpDown className="ml-2 h-4 w-4" />
                </Button>
            );
        },
        cell: ({ row }) => {
            return <span>{row.original.rating}</span>
        },
    },
    {
        accessorKey: "ratingDate",
        header: ({ column }) => {
            return (
                <Button
                    variant="ghost"
                    onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
                >
                    Date
                    <ArrowUpDown className="ml-2 h-4 w-4" />
                </Button>
            );
        },
        cell: ({ row }) => {
            const date = row.original.ratingDate ? new Date(row.original.ratingDate) : null;
            return <span>{date ? date.toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' }) : ''}</span>;
        },
    },
];
