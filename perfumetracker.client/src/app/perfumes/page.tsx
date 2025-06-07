"use client";

import PerfumeWornTable from "./perfume-worn-table";
import { Separator } from "@/components/ui/separator";
import { getUserProfile, UserProfile } from "@/services/user-profiles-service";
import { getTags } from "@/services/tag-service";
import { useEffect, useState } from "react";
import { TagDTO } from "@/dto/TagDTO";

export const dynamic = 'force-dynamic'

export default function StatsPage() {
    const [tags, setTags] = useState<TagDTO[]>([]);
    const [userProfile, setUserProfile] = useState<UserProfile | null>(null);

    useEffect(() => {
        const fetchData = async () => {
            setTags(await getTags());
            setUserProfile(await getUserProfile());
        }
        fetchData();
    }, []);
    if (!userProfile || tags.length === 0) {
        return <div>Loading...</div>;
    }
    return <div>
      <Separator></Separator>
      <PerfumeWornTable allTags={tags} userProfile={userProfile}></PerfumeWornTable>
    </div>
}