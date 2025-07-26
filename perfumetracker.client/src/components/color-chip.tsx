import { getContrastColor } from "@/app/colors";
import { Badge } from "./ui/badge";

export interface ChipProp {
  name: string;
  color: string;
  className: string;
  onChipClick: ((chip: ChipProp) => void) | null;
  enabled?: boolean;
}

export default function ColorChip(c: ChipProp) {
  const isEnabled = c.enabled ?? true;
  return (
    <Badge
      className={`h-9 px-4 py-2 text-sm rounded-md flex items-center justify-center ${c.className}`}
      style={{ backgroundColor: c.color, color: getContrastColor(c.color) }}
      onClick={() => {
        if (isEnabled && c.onChipClick) c.onChipClick(c);
      }}
    >
      {c.name}
    </Badge>
  );
}
