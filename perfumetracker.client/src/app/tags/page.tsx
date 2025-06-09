"use client";

import TagTable from "@/components/tag-table";
import { Separator } from "@/components/ui/separator";

export const dynamic = 'force-dynamic'

export default function StatsPage() {
    return <div>
      <Separator></Separator>
      <TagTable></TagTable>
    </div>
}