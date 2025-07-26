"use client";

import { PerfumeRatingDownloadDTO } from "@/dto/PerfumeRatingDownloadDTO";
import { useAuth } from "@/hooks/use-auth";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { addPerfumeRating, getPerfumeRatings } from "@/services/perfume-rating-service"
import { showError, showSuccess } from "@/services/toasty-service";
import { useState, useEffect } from "react";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Save } from "lucide-react";
import { Separator } from "@/components/ui/separator";
import { DataTable } from "@/components/ui/data-table";
import { PerfumeRatingColumns } from "./perfume-rating-columns";

interface PerfumeRatingFormProps {
    readonly perfumeId: string;
    readonly ratings: PerfumeRatingDownloadDTO[] | undefined;
}

const formSchema = z.object({
    comment: z
        .string()
        .min(1, {
            message: "Comment must be at least 1 characters.",
        })
        .trim(),
    rating: z.coerce.number().min(0).max(10),
});

export default function PerfumeRatings({ perfumeId, ratings }: PerfumeRatingFormProps) {
    //const [isLoading, setIsLoading] = useState(true);
    const [localRatings, setLocalRatings] = useState<PerfumeRatingDownloadDTO[] | undefined>(ratings);

    const auth = useAuth();
    const form = useForm<z.infer<typeof formSchema>>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            comment: "",
            rating: 0,
        },
    });

    async function onSubmit(values: z.infer<typeof formSchema>) {
        if (auth.guardAction()) return;
        const result = await addPerfumeRating(perfumeId, values.rating, values.comment);
        if (result.ok) {
            showSuccess("Rating added");
            reload();
        } else showError("Rating failed:", result.error ?? "unknown error");
    }

    async function reload() {
        const newRatings = await getPerfumeRatings(perfumeId);
        setLocalRatings(newRatings);
    }

    // For debugging: log when localRatings changes
    useEffect(() => {
        console.log("localRatings updated:", localRatings);
    }, [localRatings]);

    return (
        <div>
            <Form {...form}>
                <form
                    onSubmit={form.handleSubmit(onSubmit)}
                    className="flex flex-col items-center justify-center mt-0"
                >
                    <div className="text-center w-full">
                        <FormField
                            control={form.control}
                            name="comment"
                            render={({ field }) => (
                                <FormItem className="w-full">
                                    <FormLabel>Comment </FormLabel>
                                    <FormControl>
                                        <Input placeholder="Comment" {...field} className="w-full" />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                        <div className="flex items-end space-x-4 w-full mb-2">
                            <FormField
                                control={form.control}
                                name="rating"
                                render={({ field }) => (
                                    <FormItem className="flex-1">
                                        <FormLabel>Rating </FormLabel>
                                        <FormControl>
                                            <Input placeholder="0" {...field} />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                            <Button color="primary" type="submit">
                                <Save /> Rate
                            </Button>
                        </div>
                        <Separator className="mb-2"></Separator>
                        {localRatings && <DataTable columns={PerfumeRatingColumns} data={localRatings} />}
                    </div>
                </form>
            </Form>

        </div>
    );
}