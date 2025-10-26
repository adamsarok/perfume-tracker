import MessageBox from "@/components/message-box";
import { Button } from "@/components/ui/button";
import { PerfumeRatingDownloadDTO } from "@/dto/PerfumeRatingDownloadDTO";
import { ColumnDef } from "@tanstack/react-table";
import { ArrowUpDown, Trash2 } from "lucide-react";

export const PerfumeRatingColumns = (deletePerfumeRating: (perfumeId: string, ratingId: string) => Promise<void>)
    : ColumnDef<PerfumeRatingDownloadDTO>[] => [
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
    {   accessorKey: "delete",
        header: ({ column }) => {
            return "";
        },
        cell: ({ row }) => {
            return <MessageBox
                  className="w-8"
                  startContent={<Trash2 />}
                  modalButtonColor="danger"
                  modalButtonText=""
                  message="Are you sure you want to delete this rating?"
                  onButton1={async () => {
                    await deletePerfumeRating(row.original.perfumeId, row.original.id);
                  }}
                  button1text="Delete"
                  onButton2={null}
                  button2text="Cancel"
                />;
        },
    },
];