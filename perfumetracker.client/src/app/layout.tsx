"use client";

import { Inter } from "next/font/google";
import "./globals.css";
import { Button } from "@/components/ui/button";
import { Brain, Cake, House, List, Plus, Settings, Tag, ListChecks } from "lucide-react";
import Link from "next/link";
import { Toaster } from "@/components/ui/sonner"
import { toast } from "sonner";
import { useEffect } from "react";
import * as signalR from "@microsoft/signalr";
import { UserMissionDto } from "@/dto/MissionDto";

const inter = Inter({ subsets: ["latin"] });


export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  useEffect(() => {
    const apiUrl = process.env.NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS;
    console.log("apiUrl", apiUrl);
    if (!apiUrl) {
      console.error("API URL not configured");
      return;
    }

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${apiUrl}/hubs/mission-progress`)
      .withAutomaticReconnect()
      .build();

    connection.on("ReceiveMissionProgress", (mission: UserMissionDto) => {
      console.log("received missions", mission);
      if (mission.isCompleted) {
        toast.success(`Mission Completed: ${mission.name}`, {
          description: mission.description,
        });
      } else {
        toast.info(`Mission Updated: ${mission.name}`, {
          description: `Progress: ${mission.progress}%`,
        });
      }
    }
    );

    connection.start()
      .then(() => console.log("SignalR Connected"))
      .catch(err => console.error("SignalR Connection Error: ", err));

    return () => {
      connection.stop();
    };
  }, []);

  return (
    <html lang="en">
      <body className={inter.className}>
        <div className="max-w-lg mx-auto px-4 py-8">
          <div className="flex items-center justify-center space-x-2">
            <Link href="/" passHref>
              <Button variant="outline" size="icon" title="Home" aria-label="Home">
                <House />
              </Button>
            </Link>
            <Link href="/perfumes/surprise-me" passHref>
              <Button variant="outline" size="icon" title="Surprise Me!" aria-label="Surprise Me!">
                <Cake />
              </Button>
            </Link>
            <Link href="/perfumes/new-perfume" passHref>
              <Button variant="outline" size="icon" title="New Perfume" aria-label="New Perfume">
                <Plus />
              </Button>
            </Link>
            <Link href="/perfumes" passHref>
              <Button variant="outline" size="icon" title="Perfume Finder" aria-label="Perfume Finder">
                <List />
              </Button>
            </Link>
            <Link href="/tags" passHref>
              <Button variant="outline" size="icon" title="Tags" aria-label="Tags">
                <Tag />
              </Button>
            </Link>
            <Link href="/progress" passHref>
              <Button variant="outline" size="icon" title="Missions & Achievements" aria-label="Missions & Achievements">
                <ListChecks />
              </Button>
            </Link>
            <Link href="/ai" passHref>
              <Button variant="outline" size="icon" title="Ai Recommendations" aria-label="Ai Recommendations">
                <Brain />
              </Button>
            </Link>
            <Link href="/settings" passHref>
              <Button variant="outline" size="icon" title="Settings" aria-label="Settings">
                <Settings />
              </Button>
            </Link>
          </div>
          {children}
          <Toaster />
        </div>
      </body>
    </html>
  );
}
