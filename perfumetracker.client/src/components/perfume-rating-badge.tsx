import { Star } from "lucide-react";

export default function PerfumeRatingBadge({ rating }: { readonly rating: number }) {
  const rated = rating > 0;
  const color = !rated
    ? "bg-gray-100 text-gray-600"
    : rating >= 5
      ? "bg-gradient-to-r from-yellow-200 via-amber-200 to-yellow-400 text-amber-800 shadow-sm"
      : rating >= 4
      ? "bg-green-100 text-green-800"
      : rating >= 3
        ? "bg-amber-100 text-amber-800"
        : "bg-red-100 text-red-800";

  return (
    <span
      className={`inline-flex shrink-0 items-center gap-1 rounded-full px-2 py-1 text-xs font-semibold ${color}`}
      aria-label={rated ? `Rating ${rating.toFixed(1)} out of 5` : "Not rated"}
    >
      <Star className={`h-3 w-3 ${rating >= 5 ? "fill-current" : ""}`} aria-hidden="true" />
      {rated ? rating.toFixed(1) : "Unrated"}
    </span>
  );
}
