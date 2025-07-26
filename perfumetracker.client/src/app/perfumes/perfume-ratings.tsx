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
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import { Label } from "@/components/ui/label";

interface PerfumeRatingFormProps {
    readonly perfume: PerfumeWithWornStatsDTO;
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

export default function PerfumeRatings({ perfume }: PerfumeRatingFormProps) {
    //const [isLoading, setIsLoading] = useState(true);
    const [localRatings, setLocalRatings] = useState<PerfumeRatingDownloadDTO[] | undefined>(perfume.perfume.ratings);

    const auth = useAuth();
    const form = useForm<z.infer<typeof formSchema>>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            comment: perfume.lastComment ?? "",
            rating: perfume.averageRating ?? 0,
        },
    });

    async function onSubmit(values: z.infer<typeof formSchema>) {
        if (auth.guardAction()) return;
        const result = await addPerfumeRating(perfume.perfume.id, values.rating, values.comment);
        if (result.ok) {
            showSuccess("Rating added");
            reload();
        } else showError("Rating failed:", result.error ?? "unknown error");
    }

    async function reload() {
        const newRatings = await getPerfumeRatings(perfume.perfume.id);
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
                    <div className="flex items-end space-x-4 w-full mb-2">
                        <Button color="primary" type="submit">
                            <Save /> Rate
                        </Button>
                        <FormField
                            control={form.control}
                            name="rating"
                            render={({ field }) => (
                                <FormItem className="flex-1">
                                    <FormControl>
                                        <Input placeholder="0" {...field} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                        <FormField
                            control={form.control}
                            name="comment"
                            render={({ field }) => (
                                <FormItem className="flex-4">
                                    <FormControl>
                                        <Input placeholder="Comment" {...field} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                    </div>
                    <Separator className="mb-2"></Separator>
                    {localRatings && <DataTable columns={PerfumeRatingColumns} data={localRatings} />}
                </form>
            </Form>

        </div>
    );
}