import { useEffect, useState } from "react";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { PerfumeWithWornStatsDTO } from "@/dto/PerfumeWithWornStatsDTO";
import { getPerfume } from "@/services/perfume-service";

const perfumeCache = new Map<string, Promise<PerfumeWithWornStatsDTO | null>>();

function loadPerfume(perfumeId: string) {
  let request = perfumeCache.get(perfumeId);
  if (!request) {
    request = getPerfume(perfumeId).then((result) => result.data ?? null);
    perfumeCache.set(perfumeId, request);
  }
  return request;
}

export default function ConversationPerfumeCard({ perfumeId }: { readonly perfumeId: string }) {
  const [perfume, setPerfume] = useState<PerfumeWithWornStatsDTO | null>();

  useEffect(() => {
    let active = true;
    void loadPerfume(perfumeId).then((result) => {
      if (active) setPerfume(result);
    });
    return () => {
      active = false;
    };
  }, [perfumeId]);

  if (perfume === undefined) {
    return <span className="block rounded-lg border bg-white p-3 text-sm text-gray-500">Loading perfume...</span>;
  }
  if (perfume === null) return null;

  const { perfume: details } = perfume;
  const initials = details.perfumeName
    .split(" ")
    .map((part) => part[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();

  return (
    <a
      href={`/perfumes/${details.id}`}
      className="my-2 flex items-center gap-3 rounded-lg border bg-white p-3 text-gray-900 shadow-sm transition hover:border-blue-300 hover:shadow no-underline"
    >
      <Avatar className="h-12 w-12 flex-shrink-0">
        <AvatarImage className="object-cover" src={details.imageUrl} />
        <AvatarFallback>{initials}</AvatarFallback>
      </Avatar>
      <span className="min-w-0">
        <span className="block truncate font-semibold">{details.house} — {details.perfumeName}</span>
        <span className="block text-xs text-gray-500">
          {details.family || "Unknown family"} · {details.mlLeft} ml left
        </span>
      </span>
    </a>
  );
}
