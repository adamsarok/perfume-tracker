"use client";

import { PerfumeRatingDownloadDTO } from "@/dto/PerfumeRatingDownloadDTO";
import { useAuth } from "@/hooks/use-auth";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { addPerfumeRating } from "@/services/perfume-rating-service"
import { showError, showSuccess } from "@/services/toasty-service";
import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Save } from "lucide-react";
import { Separator } from "@/components/ui/separator";
import { Label } from "@/components/ui/label";

interface PerfumeRatingFormProps {
    readonly perfumeId: string;
    readonly ratings: PerfumeRatingDownloadDTO[] | undefined;
}

const formSchema = z.object({
    perfumeId: z
        .string()
        .min(1, {
            message: "PerfumeId must be at least 1 characters.",
        })
        .trim(),
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

    const router = useRouter();
    const auth = useAuth();
    const form = useForm<z.infer<typeof formSchema>>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            perfumeId: "",
            comment: "",
            rating: 0,
        },
    });

    async function onSubmit(values: z.infer<typeof formSchema>) {
        if (auth.guardAction()) return;
        const result = await addPerfumeRating(perfumeId, values.rating, values.comment);
        if (result.ok && result.id) {
            showSuccess("Rating added");
            reload(result.id);
        } else showError("Rating failed:", result.error ?? "unknown error");
    }

    const reload = useCallback(
        (id: string | undefined) => {
            if (id) router.push(`/perfumes/${id}`);
        },
        [router]
    );

    // if (isLoading) {
    //     return <div>Loading...</div>;
    // }

    //todo: existing ratings

    return (
        <div>
            <Form {...form}>
                <form
                    onSubmit={form.handleSubmit(onSubmit)}
                    className="flex flex-col items-center justify-center mt-0"
                >
                    <div className="text-center">
                        <FormField
                            control={form.control}
                            name="comment"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Comment </FormLabel>
                                    <FormControl>
                                        <Input placeholder="Comment" {...field} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                        <div className="flex items-center justify-left space-x-4 ">
                            <FormField
                                control={form.control}
                                name="rating"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Rating </FormLabel>
                                        <FormControl>
                                            <Input placeholder="0" {...field} />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                            <Button color="primary" className="mr-4 flex-1" type="submit" >
                                <Save /> "Rate"
                            </Button>
                        </div>
                        <Separator className="mb-2"></Separator>
                    </div>
                </form>
            </Form>

        </div>
    );
}