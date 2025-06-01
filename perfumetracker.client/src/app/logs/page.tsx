"use client";

import { useEffect, useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ScrollArea, ScrollBar } from "@/components/ui/scroll-area";
import { Badge } from "@/components/ui/badge";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Button } from "@/components/ui/button";
import { Copy } from "lucide-react";
import { Pagination, PaginationContent, PaginationEllipsis, PaginationItem, PaginationLink, PaginationNext, PaginationPrevious } from "@/components/ui/pagination";

interface LogEntry {
  message: string;
  level: number;
  timestamp: string;
  exception?: string;
  properties?: string;
}

export default function LogsPage() {
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [minLevel, setMinLevel] = useState(4);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const pageSize = 10;

  useEffect(() => {
    const fetchLogs = async () => {
      try {
        const apiUrl = process.env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS;
        if (!apiUrl) {
          console.error("API URL not configured");
          return;
        }
        const response = await fetch(`${apiUrl}/logs?minLevel=${minLevel}&page=${currentPage}&pageSize=${pageSize}`);
        const data = await response.json();
        setLogs(data.items);
        setTotalPages(Math.ceil(data.totalCount / pageSize));
      } catch (error) {
        console.error("Failed to fetch logs:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchLogs();
  }, [minLevel, currentPage]);

  const getLevelColor = (level: number) => {
    switch (level) {
      case 3:
        return "secondary";
      case 4:
      case 5:
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

  const copyToClipboard = async (text: string) => {
    try {
      await navigator.clipboard.writeText(text);
    } catch (err) {
      console.error('Failed to copy text: ', err);
    }
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
                        <Button
                          variant="ghost"
                          size="icon"
                          className="absolute right-2 top-2"
                          onClick={() => copyToClipboard(log.exception ?? "")}
                        >
                          <Copy className="h-4 w-4" />
                        </Button>
                      </div>
                      <p className="mb-2 break-words">{log.message}</p>
                      {log.exception && (
                        <div className="relative">
                          <pre className="text-sm text-red-500 bg-red-50 p-2 rounded overflow-x-auto">
                            {log.exception}
                          </pre>
                        </div>
                      )}
                      {log.properties && (
                        <details>
                          <div className="mt-2 relative">
                            <p className="text-sm font-semibold">Properties:</p>
                            <pre className="text-sm bg-gray-50 p-2 rounded overflow-x-auto">
                              {typeof log.properties === 'string'
                                ? JSON.stringify(JSON.parse(log.properties), null, 2)
                                : JSON.stringify(log.properties, null, 2)}
                            </pre>
                          </div>
                        </details>
                      )}
                    </div>
                  ))}
                </div>
              )}
              <ScrollBar orientation="vertical" />
            </ScrollArea>
          </div>
        </CardContent>
      </Card>

      <Pagination>
        <PaginationContent>
          <PaginationItem>
            <PaginationPrevious 
              href="#" 
              onClick={(e) => {
                e.preventDefault();
                if (currentPage > 1) {
                  setCurrentPage(currentPage - 1);
                }
              }}
              className={currentPage === 1 ? "pointer-events-none opacity-50" : ""}
            />
          </PaginationItem>
          
          {Array.from({ length: totalPages }, (_, i) => i + 1).map((page) => {
            // Show first page, last page, current page, and pages around current page
            if (
              page === 1 ||
              page === totalPages ||
              (page >= currentPage - 1 && page <= currentPage + 1)
            ) {
              return (
                <PaginationItem key={page}>
                  <PaginationLink
                    href="#"
                    onClick={(e) => {
                      e.preventDefault();
                      setCurrentPage(page);
                    }}
                    isActive={currentPage === page}
                  >
                    {page}
                  </PaginationLink>
                </PaginationItem>
              );
            } else if (
              page === currentPage - 2 ||
              page === currentPage + 2
            ) {
              return <PaginationEllipsis key={page} />;
            }
            return null;
          })}

          <PaginationItem>
            <PaginationNext 
              href="#" 
              onClick={(e) => {
                e.preventDefault();
                if (currentPage < totalPages) {
                  setCurrentPage(currentPage + 1);
                }
              }}
              className={currentPage === totalPages ? "pointer-events-none opacity-50" : ""}
            />
          </PaginationItem>
        </PaginationContent>
      </Pagination>




    </div>
  );
} 