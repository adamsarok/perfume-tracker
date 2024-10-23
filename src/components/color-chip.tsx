import { getContrastColor } from "@/app/colors";
import { Badge } from "./ui/badge";

export interface ChipProp {
  name: string;
  color: string;
  className: string;
  onChipClick: ((chip: ChipProp) => void) | null;
}

export default function ColorChip(c: ChipProp) {
  return (
    <Badge
      className={c.className}
      style={{ backgroundColor: c.color, color: getContrastColor(c.color) }}
      onClick={() => {
        if (c.onChipClick) c.onChipClick(c);
      }}
    >
      {c.name}
    </Badge>
  );
}
