"use client";

import "./globals.css";
import { Toaster } from "sonner";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Brain, Cake, House, List, ListChecks, Plus, Settings, Tag } from "lucide-react";
import { getCurrentUser, logoutUser } from "@/services/user-service";
import { UserMissionDto } from "@/dto/MissionDto";
import * as signalR from "@microsoft/signalr";
import { toast } from "sonner";
import { initializeApiUrl } from "@/services/axios-service";
import { useUserStore } from "@/stores/user-store";

function LayoutContent({ children }: { readonly children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [hasMounted, setHasMounted] = useState(false);
  const { user, isLoading, setUser, setLoading, clearUser } = useUserStore();

  useEffect(() => {
    setHasMounted(true);

    const connectToSignalR = async () => {

      const apiUrl = await initializeApiUrl();
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
    }
    connectToSignalR();
  }, []);

  useEffect(() => {
    if (!hasMounted) return;
    const checkAuth = async () => {
      if (!hasMounted) return;
      try {
        const currentUser = await getCurrentUser();
        if (!hasMounted) return;
        setUser(currentUser);
        if (!currentUser && (pathname !== '/login' && pathname !== '/register')) {
          router.push('/login');
        } else if (currentUser && (pathname === '/login' || pathname === '/register')) {
          router.push('/');
        }
      } catch (error) {
        if (!hasMounted) return;
        console.error('Auth check failed:', error);
        if ((pathname !== '/login' && pathname !== '/register')) {
          router.push('/login');
        }
      } finally {
        if (hasMounted) {
          setLoading(false);
        }
      }
    };

    if ((pathname === '/login' || pathname === '/register')) {
      setLoading(false);
      return;
    }

    checkAuth();
  }, [pathname, hasMounted, setUser, setLoading]);

  const handleLogout = async () => {
    if (!hasMounted) return;
    try {
      await logoutUser();
    } catch (error) {
      console.error('Logout failed:', error);
    } finally {
      clearUser();
      router.push('/login');
    }
  };

  if (isLoading && (pathname !== '/login' && pathname !== '/register')) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-gray-900"></div>
      </div>
    );
  }

  if ((pathname === '/login' || pathname === '/register')) {
    return <>{children}</>;
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-center h-16">
            {user && (
              <div className="flex items-center">
                <span className="text-gray-700 mr-4">{user.email}</span>
                <Button
                  onClick={handleLogout}
                >
                  Logout
                </Button>
              </div>
            )}
          </div>
        </div>
      </nav>
      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="max-w-lg mx-auto px-4">
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
      </main>
    </div>
  );
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body>
        <LayoutContent>{children}</LayoutContent>
      </body>
    </html>
  );
}
