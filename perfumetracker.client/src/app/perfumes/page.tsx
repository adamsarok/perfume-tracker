"use client";

import PerfumeWornTable from "./perfume-worn-table";
import { Separator } from "@/components/ui/separator";
import { getUserProfile, UserProfile } from "@/services/user-profiles-service";
import { getTags } from "@/services/tag-service";
import { useEffect, useState } from "react";
import { TagDTO } from "@/dto/TagDTO";
import { showError } from "@/services/toasty-service";

export const dynamic = 'force-dynamic'

export default function StatsPage() {
    const [tags, setTags] = useState<TagDTO[]>([]);
    const [userProfile, setUserProfile] = useState<UserProfile | null>(null);
    const [loading, setLoading] = useState(true);
    useEffect(() => {
        const fetchData = async () => {
            try {
            const tagsResult = await getTags();
            if (tagsResult.error || !tagsResult.data) { 
                setTags([]);
                showError("Could not load tags", tagsResult.error ?? "unknown error");
                return;
            }
            setTags(tagsResult.data);
            const userProfileResult = await getUserProfile();
            if (userProfileResult.error || !userProfileResult.data) {
                setUserProfile(null);
                showError("Could not load user profile", userProfileResult.error ?? "unknown error");
                return;
            }
            setUserProfile(userProfileResult.data);
            } catch (error) {
                showError("Could not load data", error);
            } finally {
                setLoading(false);
            }
        }
        fetchData();
    }, []);
    if (loading) {
        return <div>Loading...</div>;
    }
    if (!userProfile) {
        return <div>Could not load user profile</div>;
    }
    return <div>
      <Separator></Separator>
      <PerfumeWornTable allTags={tags} userProfile={userProfile}></PerfumeWornTable>
    </div>
}