'use client';

import { Button } from "@nextui-org/react";
import { Tag } from "@prisma/client";

export interface TagSelectorProps {
    tags: Tag[]
}

function GetNextUIColor(color: string): "default" | "primary" | "secondary" | "success" | "warning" | "danger" | undefined {
    if (color === "default"
        || color === "primary"
        || color === "secondary"
        || color === "success"
        || color === "warning"
        || color === "danger"
    ) {
        return color; //this is terrible, there must be a better way?
    }
    return "default";
}

export default function TagSelector({tags}: TagSelectorProps) {
    const tagComponents = tags.map((tag) => {
        return <Button className="mr-2" size="md" color={GetNextUIColor(tag.color)}>{tag.tag}</Button>
        
    });
    return <div>
        {tagComponents}
    </div>;
}