'use client';

import { Button, Input } from "@nextui-org/react";
import { useFormState } from "react-dom";
import *  as actions from "@/app/actions";

export default function NewPerfumePage() {
    const [formState, action] = useFormState(actions.AddPerfume, { message: '' });
    
    return <div>
        <form action={action}>
            <Input label="House" name="house">House</Input>
            <Input label="Perfume" name="perfume">Perfume</Input>
            <Input label="Rating" name="rating">Rating</Input>
            <Button type="submit">Add Perfume</Button>
            {formState.message ? <div className='my-2 p-2 bg-red-200 border rounded border-red-400'>{formState.message}</div> : null}
        </form>
    </div>
}