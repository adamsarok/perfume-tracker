"use client";

import PerfumeEditForm from "@/app/perfumes/perfume-edit-form";

export default function NewPerfumePage() {
    return <PerfumeEditForm 
        perfumeId={""} 
        isRandomPerfume={false}
    />;
}