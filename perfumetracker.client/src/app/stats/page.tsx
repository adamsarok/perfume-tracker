"use client";

import { getCurrentUserStats, UserStatsDTO } from "@/services/stats-service";
import { useEffect, useState } from "react";
import { showError } from "@/services/toasty-service";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";

export const dynamic = 'force-dynamic'

export default function StatsPage() {
    const [stats, setStats] = useState<UserStatsDTO | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchData = async () => {
            try {
                const result = await getCurrentUserStats();
                if (result.error || !result.data) {
                    setStats(null);
                    showError("Could not load stats", result.error ?? "unknown error");
                    return;
                }
                setStats(result.data);
            } catch (error) {
                showError("Could not load stats", error);
            } finally {
                setLoading(false);
            }
        }
        fetchData();
    }, []);

    if (loading) {
        return <div className="p-6">Loading...</div>;
    }

    if (!stats) {
        return <div className="p-6">Could not load user stats</div>;
    }

    const formatDate = (dateStr: string | null) => {
        if (!dateStr) return "N/A";
        return new Date(dateStr).toLocaleDateString();
    };

    return (
        <div className="p-6 space-y-6">
            <div>
                <h1 className="text-3xl font-bold">User Statistics</h1>
                <p className="text-muted-foreground">Overview of your perfume collection and usage</p>
            </div>

            <Separator />

            {/* Overview Stats */}
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
                <Card>
                    <CardHeader className="pb-2">
                        <CardTitle className="text-sm font-medium text-muted-foreground">Total Perfumes</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="text-2xl font-bold">{stats.perfumesCount}</div>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader className="pb-2">
                        <CardTitle className="text-sm font-medium text-muted-foreground">Total Wears</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="text-2xl font-bold">{stats.wearCount}</div>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader className="pb-2">
                        <CardTitle className="text-sm font-medium text-muted-foreground">Current Streak</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="text-2xl font-bold">{stats.currentStreak ?? "N/A"}</div>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader className="pb-2">
                        <CardTitle className="text-sm font-medium text-muted-foreground">Best Streak</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="text-2xl font-bold">{stats.bestStreak ?? "N/A"}</div>
                    </CardContent>
                </Card>
            </div>

            {/* Dates and Progress */}
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                <Card>
                    <CardHeader className="pb-2">
                        <CardTitle className="text-sm font-medium text-muted-foreground">Start Date</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="text-lg font-semibold">{formatDate(stats.startDate)}</div>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader className="pb-2">
                        <CardTitle className="text-sm font-medium text-muted-foreground">Last Wear</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="text-lg font-semibold">{formatDate(stats.lastWear)}</div>
                    </CardContent>
                </Card>

                {stats.level !== null && (
                    <Card>
                        <CardHeader className="pb-2">
                            <CardTitle className="text-sm font-medium text-muted-foreground">Level & XP</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <div className="text-lg font-semibold">
                                Level {stats.level} • {stats.xp?.toLocaleString()} XP
                            </div>
                            {stats.xpMultiplier && (
                                <div className="text-sm text-muted-foreground">
                                    {stats.xpMultiplier}x multiplier
                                </div>
                            )}
                        </CardContent>
                    </Card>
                )}
            </div>

            {/* Collection Stats */}
            <Card>
                <CardHeader>
                    <CardTitle>Collection</CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="grid gap-4 md:grid-cols-2">
                        <div>
                            <p className="text-sm text-muted-foreground">Total Volume</p>
                            <p className="text-xl font-bold">{stats.totalPerfumesMl.toFixed(1)} ml</p>
                        </div>
                        <div>
                            <p className="text-sm text-muted-foreground">Remaining Volume</p>
                            <p className="text-xl font-bold">{stats.totalPerfumesMlLeft.toFixed(1)} ml</p>
                        </div>
                        <div>
                            <p className="text-sm text-muted-foreground">Monthly Usage</p>
                            <p className="text-xl font-bold">{stats.monthlyUsageMl.toFixed(2)} ml</p>
                        </div>
                        <div>
                            <p className="text-sm text-muted-foreground">Yearly Usage</p>
                            <p className="text-xl font-bold">{stats.yearlyUsageMl.toFixed(2)} ml</p>
                        </div>
                    </div>
                </CardContent>
            </Card>

            {/* Favorite Perfumes */}
            {stats.favoritePerfumes.length > 0 && (
                <Card>
                    <CardHeader>
                        <CardTitle>Favorite Perfumes</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-3">
                            {stats.favoritePerfumes.map((perfume) => (
                                <div key={perfume.id} className="flex justify-between items-center border-b pb-2">
                                    <div>
                                        <p className="font-medium">{perfume.house}</p>
                                        <p className="text-sm text-muted-foreground">{perfume.perfumeName}</p>
                                    </div>
                                    <div className="text-right">
                                        <p className="font-semibold">⭐ {perfume.averageRating.toFixed(1)}</p>
                                        <p className="text-sm text-muted-foreground">{perfume.wearCount} wears</p>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </CardContent>
                </Card>
            )}

            {/* Favorite Tags */}
            {stats.favoriteTags.length > 0 && (
                <Card>
                    <CardHeader>
                        <CardTitle>Favorite Tags</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-3">
                            {stats.favoriteTags.map((tag) => (
                                <div key={tag.id} className="flex justify-between items-center border-b pb-2">
                                    <div className="flex items-center gap-2">
                                        <div 
                                            className="w-4 h-4 rounded-full" 
                                            style={{ backgroundColor: tag.color }}
                                        />
                                        <p className="font-medium">{tag.tagName}</p>
                                    </div>
                                    <div className="text-right">
                                        <p className="font-semibold">{tag.wearCount} wears</p>
                                        <p className="text-sm text-muted-foreground">{tag.totalMl.toFixed(1)} ml</p>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </CardContent>
                </Card>
            )}

            {/* Recommendation Stats */}
            {stats.recommendationStats.length > 0 && (
                <Card>
                    <CardHeader>
                        <CardTitle>Recommendation Statistics</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-3">
                            {stats.recommendationStats.map((rec, index) => (
                                <div key={index} className="flex justify-between items-center border-b pb-2">
                                    <p className="font-medium">{rec.strategy}</p>
                                    <div className="text-right">
                                        <p className="font-semibold">
                                            {rec.acceptedRecommendations} / {rec.totalRecommendations}
                                        </p>
                                        <p className="text-sm text-muted-foreground">
                                            {rec.totalRecommendations > 0 
                                                ? `${((rec.acceptedRecommendations / rec.totalRecommendations) * 100).toFixed(1)}%` 
                                                : "0%"}
                                        </p>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </CardContent>
                </Card>
            )}
        </div>
    );
}
