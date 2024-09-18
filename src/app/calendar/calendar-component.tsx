'use client';

import * as perfumeRepo from "@/db/perfume-repo";
import * as perfumeWornRepo from "@/db/perfume-worn-repo";
import { Chip } from "@nextui-org/react";
import React, { useEffect, useState, Component } from "react";
import { getContrastColor } from "@/app/colors";
import Chart from "react-apexcharts";
import { stat } from "fs";
import { ApexOptions } from "apexcharts";

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

            //console.log(stats)
        });

        setMonthlyStats(stats);
    }, [perfumes, perfumesWorn]);

     const days = Object.entries(monthlyStats).flatMap(([date]) => date).slice(1,12);
    console.log(days);

    const options: ApexOptions = {
        chart: {
          id: "basic-bar",
          height: "100%",
          stacked: true,
        },
        xaxis: {
          categories: days,
        },
        plotOptions: {
            bar: {
                horizontal: true,
                dataLabels: {
                  total: {
                    enabled: true,
                    offsetX: 0,
                    style: {
                      fontSize: '13px',
                      fontWeight: 900
                    }
                  }
                }
            },
        },
        stroke: {
            width: 1,
            colors: ['#fff']
        },
        fill: {
            opacity: 1
        },
        legend: {
            position: 'top',
            horizontalAlign: 'left',
            offsetX: 40
        }
      };

    

      const seriesData = Object.entries(monthlyStats).reduce((acc: any, [date, tags]) => {
        Object.entries(tags).forEach(([tagName, stat]) => {
            if (!acc[tagName]) {
                acc[tagName] = { name: tagName, data: [], color: stat.color };
            }
            // Check if the date already exists in the data array
            const existingDataPoint = acc[tagName].data.find((dp: { x: string; }) => dp.x === date);
            if (existingDataPoint) {
                existingDataPoint.y += stat.wornCount; // Aggregate wornCount
            } else {
                acc[tagName].data.push({ x: date, y: stat.wornCount });
            }
        });
        return acc;
    }, {});
    console.log(seriesData)

    const apexSeries: ApexAxisChartSeries = Object.values(seriesData)
    //TODO: each day has multiple rows

    const state = {
        options,
        //series: apexSeries
        series : [
          {
            name: "TODO",
            data: [30, 40, 45, 50, 49, 60, 70, 91]
          },
          {
            name: "TODO-2",
            data: [30, 40, 45, 50, 49, 60, 70, 91]
          },
        ],
    };
    

    return (
        <div className="max-w-4xl mx-auto">
             <div className="mixed-chart">
            <Chart
              options={state.options}
              series={state.series}
              type="bar" 
              width="500"
              height="800"
            />
          </div>
        </div>
    );
}