import { Button, Input, Link } from "@nextui-org/react";
import { useFormState } from "react-dom";
import *  as actions from "@/app/actions";
import PerfumeEditForm from "@/components/perfume-edit-form";
import { db } from "@/db";

export default async function NewPerfumePage() {
    const tags = await db.tag.findMany();
    return <PerfumeEditForm perfume={null} tags={tags}></PerfumeEditForm>
}