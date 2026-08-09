import { useParams } from "@tanstack/react-router";
import PerfumeEditForm from "@/app/perfumes/perfume-edit-form";

export default function EditPerfumePage() {
  const { id } = useParams({ from: '/perfumes/$id' });
  return <PerfumeEditForm key={id} perfumeId={id} randomsId={null} />;
}
