"use client";

import { useEffect, useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Badge } from "@/components/ui/badge";

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

  useEffect(() => {
    const fetchLogs = async () => {
      try {
        const apiUrl = process.env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS;
        if (!apiUrl) {
          console.error("API URL not configured");
          return;
        }
        const response = await fetch(`${apiUrl}/logs`);
        const data = await response.json();
        setLogs(data);
      } catch (error) {
        console.error("Failed to fetch logs:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchLogs();
  }, []);

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

  return (
    <div className="container mx-auto py-6">
      <Card>
        <CardHeader>
          <CardTitle>Application Logs</CardTitle>
        </CardHeader>
        <CardContent>
          <ScrollArea className="h-[600px]">
            {loading ? (
              <div className="flex items-center justify-center h-full">
                <p>Loading logs...</p>
              </div>
            ) : (
              <div className="space-y-4">
                {logs.map((log, index) => (
                  <div key={index} className="p-4 border rounded-lg">
                    <div className="flex items-center gap-2 mb-2">
                      <Badge variant={getLevelColor(log.level)}>
                        {log.level}
                      </Badge>
                      <span className="text-sm text-gray-500">
                        {new Date(log.timestamp).toLocaleString()}
                      </span>
                    </div>
                    <p className="mb-2">{log.message}</p>
                    {log.exception && (
                      <pre className="text-sm text-red-500 bg-red-50 p-2 rounded">
                        {log.exception}
                      </pre>
                    )}
                    {log.properties && Object.keys(log.properties).length > 0 && (
                      <div className="mt-2">
                        <p className="text-sm font-semibold">Properties:</p>
                        <pre className="text-sm bg-gray-50 p-2 rounded">
                          {JSON.stringify(log.properties, null, 2)}
                        </pre>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </ScrollArea>
        </CardContent>
      </Card>
    </div>
  );
} 