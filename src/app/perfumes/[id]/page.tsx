//'use client';

import { Button, Input, Link } from "@nextui-org/react";
import { useFormState } from "react-dom";
import *  as actions from "@/app/actions";
import PerfumeEditForm from "@/components/perfume-edit-form";
import { db } from "@/db";
import { notFound } from "next/navigation";

interface EditPerfumePageProps {
    params: {
        id: string
    }
}

export default async function EditPerfumePage({params}: EditPerfumePageProps) {
    const perfume = await db.perfume.findFirst({
        where: {
            id: parseInt(params.id)
        },
    });
    if (!perfume) return notFound();
    return <PerfumeEditForm perfume={perfume}></PerfumeEditForm>
}