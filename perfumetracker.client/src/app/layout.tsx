"use client";

import "./globals.css";
import { Toaster } from "sonner";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { getCurrentUser, logoutUser, getUserXP, UserXPResponse } from "@/services/user-service";
import { UserMissionDto } from "@/dto/MissionDto";
import * as signalR from "@microsoft/signalr";
import { toast } from "sonner";
import { initializeApiUrl } from "@/services/axios-service";
import { useUserStore } from "@/stores/user-store";
import { generateMissions } from "@/services/mission-service";
import AppNavigationMenu from "@/components/app-navigation-menu";
import { LogOut } from "lucide-react";
import { Progress } from "@/components/ui/progress";


function LayoutContent({ children }: { readonly children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [hasMounted, setHasMounted] = useState(false);
  const { user, isLoading, setUser, setLoading, clearUser } = useUserStore();
  const [xp, setXp] = useState<UserXPResponse | null>(null);

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
        if (currentUser) {
          await generateMissions();
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

  useEffect(() => {
    if (!hasMounted || !user) return;
    getUserXP().then(setXp).catch(() => setXp(null));
  }, [hasMounted, user]);

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
        <div className="max-w-xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex h-16">
            {user && (
              <div className="flex items-center justify-between w-full">
                <AppNavigationMenu></AppNavigationMenu>
                <div className="flex items-center gap-4">
                  <span className="text-gray-700 text-sm">{user.email}</span>
                  <Button size="icon"
                    onClick={handleLogout}
                  >
                    <LogOut />
                  </Button>
                </div>
              </div>
            )}
          </div>
        </div>
        {user && xp && (
          <div className="max-w-xl mx-auto px-4 sm:px-6 lg:px-8 py-2">
            <div className="flex items-center gap-4">
              <Progress value={((xp.xp - xp.xpLastLevel) / (xp.xpNextLevel - xp.xpLastLevel)) * 100}   />
              <span className="text-xs text-gray-500">{xp.xp} XP Level {xp.level}</span>
            </div>
          </div>
        )}
      </nav>
      <main className="max-w-xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="max-w-lg mx-auto px-4">

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
