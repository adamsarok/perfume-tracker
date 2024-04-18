'use client';

import { Button, Input, Link } from "@nextui-org/react";
import { Perfume } from "@prisma/client";
import { useFormState } from "react-dom";
import * as actions from "@/app/actions";

interface PerfumeEditFormProps {
    perfume: Perfume
}

export default function PerfumeEditForm({perfume}: PerfumeEditFormProps) {
    const [formState, action] = useFormState(actions.UpdatePerfume.bind(null, perfume.id), { errors: {} });
    return <div>
        <Link isBlock showAnchorIcon href='/' color="foreground">Back</Link>
        <form action={action}>
            <Input label="House" name="house" defaultValue={perfume.house}
                isInvalid={!!formState.errors.house}
                errorMessage={formState.errors.house?.join(',')}
            ></Input>
            <Input label="Perfume" name="perfume" defaultValue={perfume.perfume}
                isInvalid={!!formState.errors.perfume}
                errorMessage={formState.errors.perfume?.join(',')}
                ></Input>
            <Input label="Rating" name="rating" defaultValue={perfume.rating.toString()}
                isInvalid={!!formState.errors.rating}
                errorMessage={formState.errors.rating?.join(',')}
                ></Input>
            <Input label="Notes" name="notes" defaultValue={perfume.notes}
                isInvalid={!!formState.errors.notes}
                errorMessage={formState.errors.notes?.join(',')}
            ></Input>
            <Button type="submit">Update Perfume</Button>
            {formState.errors._form ? 
                <div className="p-2 bg-red-200 border border-red-400 rounded">
                    {formState.errors._form?.join(',')}
                </div> : null
            }
        </form>
    </div>
}