'use client';

import { Button, Input, Link } from "@nextui-org/react";
import { useFormState } from "react-dom";
import *  as actions from "@/app/actions";

export default function NewPerfumePage() {
    const [formState, action] = useFormState(actions.AddPerfume, { errors: {} });
    
    return <div>
        <Link isBlock showAnchorIcon href='/' color="foreground">Back</Link>
        <form action={action}>
            <Input label="House" name="house"
                isInvalid={!!formState.errors.house}
                errorMessage={formState.errors.house?.join(',')}
            >House</Input>
            <Input label="Perfume" name="perfume"
                isInvalid={!!formState.errors.perfume}
                errorMessage={formState.errors.perfume?.join(',')}
            >Perfume</Input>
            <Input label="Rating" name="rating"
                isInvalid={!!formState.errors.rating}
                errorMessage={formState.errors.rating?.join(',')}
            >Rating</Input>
            <Input label="Notes" name="notes"
                isInvalid={!!formState.errors.notes}
                errorMessage={formState.errors.notes?.join(',')}
            >Notes</Input>
            <Button type="submit">Add Perfume</Button>
            {formState.errors._form ? <div className='my-2 p-2 bg-red-200 border rounded border-red-400'>{formState.errors._form}</div> : null}
        </form>
    </div>
}