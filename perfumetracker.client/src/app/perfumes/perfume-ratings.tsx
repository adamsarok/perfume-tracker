"use client";

import { PerfumeRatingDownloadDTO } from "@/dto/PerfumeRatingDownloadDTO";
import { useAuth } from "@/hooks/use-auth";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { addPerfumeRating, deletePerfumeRating, getPerfumeRatings } from "@/services/perfume-rating-service"
import { showError, showSuccess } from "@/services/toasty-service";
import { useState } from "react";
import { Form, FormControl, FormField, FormItem, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Save } from "lucide-react";
import { Separator } from "@/components/ui/separator";
import { DataTable } from "@/components/ui/data-table";
import { PerfumeRatingColumns } from "./perfume-rating-columns";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";

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

/**
 * Renders a rating form and ratings table for a given perfume and manages rating actions.
 *
 * The component displays a form to submit a numeric rating and optional comment, loads and
 * shows existing ratings in a table, and supports adding and deleting ratings with UI feedback.
 *
 * @param perfume - Perfume data used to populate defaults and initial ratings (including id, average/last values, and existing ratings)
 * @returns A React element that renders the rating form, a separator, and a data table of the perfume's ratings
 */
export default function PerfumeRatings({ perfume }: PerfumeRatingFormProps) {
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
        const result = await getPerfumeRatings(perfume.perfume.id);
        if (result.error || !result.data) {
            showError("Could not load ratings", result.error ?? "unknown error");
            return;
        }
        setLocalRatings(result.data);
    }

    const onDeleteRating = async (perfumeId: string | undefined, ratingId: string | undefined) => {
        if (auth.guardAction()) return;
        if (perfumeId && ratingId) {
            const result = await deletePerfumeRating(perfumeId, ratingId);
            if (result.error) showError("Rating deletion failed", result.error);
            else {
                showSuccess("Rating deleted!");
                await reload();
            }
        }
    };


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
                                <FormItem className="flex-1 min-w-12">
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
                </form>
            </Form>
            <Separator className="mb-2"></Separator>
            {localRatings && <DataTable columns={PerfumeRatingColumns(onDeleteRating)} data={localRatings} />}
        </div>
    );
}