import { Chip } from "@nextui-org/react";
import { getContrastColor } from "@/app/colors";

export interface ChipProp {
    name: string,
    color: string,
    className: string,
    onChipClick: ((chip: ChipProp) => void) | null
}

export default function ColorChip(c: ChipProp) {
    return (
        <Chip className={c.className}
            style={{ backgroundColor: c.color, color: getContrastColor(c.color) }}
            onClick={() => { if (c.onChipClick) c.onChipClick(c)}}>
            {c.name}
        </Chip>
    )
}