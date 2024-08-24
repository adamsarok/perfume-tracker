'use client';

import * as perfumeRepo from "@/db/perfume-repo";
import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import { Chip } from "@nextui-org/react";
import React, { useEffect, useState } from "react";
import { getContrastColor } from "@/app/colors";

export const dynamic = 'force-dynamic'

export interface CalendarPageProps {
    perfumes: perfumeRepo.PerfumeWithTagDTO[],
    perfumesWorn: perfumeWornRepo.WornWithPerfume[]
}


export default function CalendarComponent({ perfumes, perfumesWorn }: CalendarPageProps) {
    interface TagStat {
        color: string;
        mls: number;
        wornCount: number;
    }

    interface TagStats {
        [key: string]: TagStat
    }

    interface DayStats {
        [key: string]: TagStats
    }

    const [monthlyStats, setMonthlyStats] = useState<DayStats>({});
    const [currentDate, setCurrentDate] = useState(new Date());

    useEffect(() => {
        const stats: DayStats = {};
        
        perfumesWorn.forEach(pw => {
            const date = new Date(pw.wornOn);
            const dateKey = date.toISOString().split('T')[0];
            
            if (!stats[dateKey]) {
                stats[dateKey] = {};
            }

            const perfume = perfumes.find(p => p.perfume.id === pw.perfume.id);
            if (perfume && perfume.perfume.rating >= 8) {
                perfume.tags.forEach(tag => {
                    if (!stats[dateKey][tag.tag]) {
                        stats[dateKey][tag.tag] = {
                            color: tag.color,
                            mls: 0,
                            wornCount: 0
                        };
                    }
                    stats[dateKey][tag.tag].mls += perfume.perfume.ml;
                    stats[dateKey][tag.tag].wornCount += 1;
                });
            }
        });

        setMonthlyStats(stats);
    }, [perfumes, perfumesWorn]);

    const daysInMonth = new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 0).getDate();
    const firstDayOfMonth = new Date(currentDate.getFullYear(), currentDate.getMonth(), 1).getDay();

    const renderCalendar = () => {
        const calendar = [];
        let dayCount = 1;

        for (let i = 0; i < 6; i++) {
            const week = [];
            for (let j = 0; j < 7; j++) {
                if (i === 0 && j < firstDayOfMonth) {
                    week.push(<td key={`empty-${j}`} className="p-2 border"></td>);
                } else if (dayCount > daysInMonth) {
                    week.push(<td key={`empty-end-${j}`} className="p-2 border"></td>);
                } else {
                    const date = new Date(currentDate.getFullYear(), currentDate.getMonth(), dayCount);
                    const dateKey = date.toISOString().split('T')[0];
                    const dayStats = monthlyStats[dateKey];

                    week.push(
                        <td key={dayCount} className="p-2 border">
                            <div className="font-bold">{dayCount}</div>
                            {dayStats && Object.entries(dayStats).map(([tagName, tagInfo]) => (
                                <Chip
                                    key={tagName}
                                    size="sm"
                                    style={{
                                        backgroundColor: tagInfo.color,
                                        color: getContrastColor(tagInfo.color),
                                        margin: '2px',
                                    }}
                                >
                                    {tagName}
                                </Chip>
                            ))}
                        </td>
                    );
                    dayCount++;
                }
            }
            calendar.push(<tr key={i}>{week}</tr>);
            if (dayCount > daysInMonth) break;
        }

        return calendar;
    };

    return (
        <div className="max-w-4xl mx-auto">
            <div className="flex justify-between items-center mb-4">
                <button onClick={() => setCurrentDate(new Date(currentDate.getFullYear(), currentDate.getMonth() - 1, 1))}>
                    Previous Month
                </button>
                <h2 className="text-xl font-bold">
                    {currentDate.toLocaleString('default', { month: 'long', year: 'numeric' })}
                </h2>
                <button onClick={() => setCurrentDate(new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 1))}>
                    Next Month
                </button>
            </div>
            <table className="w-full border-collapse">
                <thead>
                    <tr>
                        {['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map(day => (
                            <th key={day} className="p-2 border">{day}</th>
                        ))}
                    </tr>
                </thead>
                <tbody>
                    {renderCalendar()}
                </tbody>
            </table>
        </div>
    );
}