import PerfumeEditForm from "@/app/perfumes/perfume-edit-form";

export const dynamic = 'force-dynamic'

interface EditPerfumePageProps {
    readonly params: Promise<{
        id: string
    }>
}

export default async function EditPerfumePage(props: EditPerfumePageProps) {
    const params = await props.params;
    return <PerfumeEditForm perfumeId={params.id} randomsId={null}></PerfumeEditForm>
}