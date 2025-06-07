"use client";

import PerfumeEditForm from "@/components/perfume-edit-form";

export default function NewPerfumePage() {
    return <PerfumeEditForm 
        perfumeId={""} 
        isRandomPerfume={false}
    />;
}