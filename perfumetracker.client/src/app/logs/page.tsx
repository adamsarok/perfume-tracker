"use client";

import { useEffect, useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ScrollArea, ScrollBar } from "@/components/ui/scroll-area";
import { Badge } from "@/components/ui/badge";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

interface LogEntry {
  message: string;
  level: number;
  timestamp: string;
  exception?: string;
  properties?: Record<string, any>;
}

export default function LogsPage() {
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [minLevel, setMinLevel] = useState(4);

  useEffect(() => {
    const fetchLogs = async () => {
      try {
        const apiUrl = process.env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS;
        if (!apiUrl) {
          console.error("API URL not configured");
          return;
        }
        const response = await fetch(`${apiUrl}/logs?minLevel=${minLevel}`);
        const data = await response.json();
        setLogs(data);
      } catch (error) {
        console.error("Failed to fetch logs:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchLogs();
  }, [minLevel]);

  const getLevelColor = (level: number) => {
    switch (level) {
      case 3:
        return "secondary";
      case 4:
      case 5:
      case 6:
        return "destructive";
      default:
        return "default";
    }
  };

  const LOG_LEVELS = [
    { value: 0, label: "Verbose" },
    { value: 1, label: "Debug" },
    { value: 2, label: "Information" },
    { value: 3, label: "Warning" },
    { value: 4, label: "Error" },
    { value: 5, label: "Fatal" },
  ];

  const getLevelLabel = (level: number) => {
    return LOG_LEVELS.find(l => l.value === level)?.label || `Level ${level}`;
  };

  return (
    <div className="container mx-auto py-6">
      <Card className="w-full">
        <CardHeader>
          <CardTitle>Application Logs</CardTitle>
          <Select value={minLevel.toString()} onValueChange={(value) => setMinLevel(parseInt(value))}>
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Select minimum level" />
            </SelectTrigger>
            <SelectContent>
              {LOG_LEVELS.map((level) => (
                <SelectItem key={level.value} value={level.value.toString()}>
                  {level.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </CardHeader>
        <CardContent className="p-0">
          <div className="px-6">
            <ScrollArea className="h-[600px] px-1">
              {loading ? (
                <div className="flex items-center justify-center h-full">
                  <p>Loading logs...</p>
                </div>
              ) : (
                <div className="space-y-4 pr-4">
                  {logs.map((log, index) => (
                    <div key={index} className="p-4 border rounded-lg">
                      <div className="flex items-center gap-2 mb-2">
                        <Badge variant={getLevelColor(log.level)}>
                          {getLevelLabel(log.level)}
                        </Badge>
                        <span className="text-sm text-gray-500">
                          {new Date(log.timestamp).toLocaleString()}
                        </span>
                      </div>
                      <p className="mb-2 break-words">{log.message}</p>
                      {log.exception && (
                        <pre className="text-sm text-red-500 bg-red-50 p-2 rounded overflow-x-auto">
                          {log.exception}
                        </pre>
                      )}
                      {log.properties && Object.keys(log.properties).length > 0 && (
                        <div className="mt-2">
                          <p className="text-sm font-semibold">Properties:</p>
                          <pre className="text-sm bg-gray-50 p-2 rounded overflow-x-auto">
                            {JSON.stringify(log.properties, null, 2)}
                          </pre>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
              <ScrollBar orientation="vertical"/>
            </ScrollArea>
          </div>
        </CardContent>
      </Card>
    </div>
  );
} 