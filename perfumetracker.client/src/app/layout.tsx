"use client";

import "./globals.css";
import { Toaster } from "sonner";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import {
  getCurrentUser,
  logoutUser,
  getUserXP,
  UserXPResponse,
} from "@/services/user-service";
import { UserMissionDto } from "@/dto/MissionDto";
import * as signalR from "@microsoft/signalr";
import { toast } from "sonner";
import { initializeApiUrl } from "@/services/axios-service";
import { useUserStore } from "@/stores/user-store";
import { generateMissions } from "@/services/mission-service";
import AppNavigationMenu from "@/components/app-navigation-menu";
import { LogOut } from "lucide-react";
import { Progress } from "@/components/ui/progress";
import { showError } from "@/services/toasty-service";

function LayoutContent({ children }: { readonly children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [hasMounted, setHasMounted] = useState(false);
  const { user, isLoading, setUser, setLoading, clearUser } = useUserStore();
  const [xp, setXp] = useState<UserXPResponse | null>(null);

  useEffect(() => {
    setHasMounted(true);

    let connection: signalR.HubConnection | null = null;
    let cancelled = false;
    (async () => {
      const apiUrl = await initializeApiUrl();
      if (!apiUrl) {
        console.error("API URL not configured");
        return;
      }
      connection = new signalR.HubConnectionBuilder()
        .withUrl(`${apiUrl}/hubs/mission-progress`)
        .withAutomaticReconnect()
        .build();
      connection.on("ReceiveMissionProgress", (mission: UserMissionDto) => {
        if (mission.isCompleted) {
          toast.success(`Mission Completed: ${mission.name}`, {
            description: mission.description,
          });
        } else {
          toast.info(`Mission Updated: ${mission.name}`, {
            description: `Progress: ${mission.progress}%`,
          });
        }
      });
      try {
        await connection.start();
        console.log("SignalR Connected");
      } catch (err) {
        console.error("SignalR Connection Error: ", err);
      }
      if (cancelled && connection) {
        try {
          await connection.stop();
        } catch (error) {
          console.error("SignalR Disconnected: ", error);
        }
      }
    })();
    return () => {
      cancelled = true;
      if (connection) {
        try {
          connection.stop();
        } catch (error) {
          console.error("SignalR Disconnected: ", error);
        }
      }
    };
  }, []);

  useEffect(() => {
    if (!hasMounted) return;
    const checkAuth = async () => {
      if (!hasMounted) return;
      try {
        const currentUserResult = await getCurrentUser();
        if (!hasMounted) return;
        if (currentUserResult.ok && currentUserResult.data) {
          setUser(currentUserResult.data);
        } else {
          if (pathname !== "/login" && pathname !== "/register") {
            router.push("/login");
          }
          return;
        }
        if (pathname === "/login" || pathname === "/register") {
          router.push("/");
        }
        await generateMissions();
      } catch (error) {
        if (!hasMounted) return;
        console.error("Auth check failed:", error);
        if (pathname !== "/login" && pathname !== "/register") {
          router.push("/login");
        }
      } finally {
        if (hasMounted) {
          setLoading(false);
        }
      }
    };

    if (pathname === "/login" || pathname === "/register") {
      setLoading(false);
      return;
    }

    checkAuth();
  }, [pathname, hasMounted, setUser, setLoading]);

  useEffect(() => {
    const fetchUserXP = async () => {
      if (!hasMounted || !user) return;
      const result = await getUserXP();
      if (result.error || !result.data) {
        showError("Could not load user XP", result.error ?? "unknown error");
        return;
      }
      setXp(result.data);
    };
    fetchUserXP();
  }, [hasMounted, user]);

  const handleLogout = async () => {
    if (!hasMounted) return;
    const result = await logoutUser();
    if (result.error) {
      toast.error("Error during logout: " + result.error);
    }
    clearUser();
    router.push("/login");
  };

  if (isLoading && pathname !== "/login" && pathname !== "/register") {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-gray-900"></div>
      </div>
    );
  }

  if (pathname === "/login" || pathname === "/register") {
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
                  <Button size="icon" onClick={handleLogout}>
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
              <span className="text-xs text-gray-500">
                {xp.xp} XP Level {xp.level}
              </span>
              <Progress
                value={
                  xp.xpNextLevel - xp.xpLastLevel > 0
                    ? Math.min(
                        100,
                        Math.max(
                          0,
                          ((xp.xp - xp.xpLastLevel) /
                            (xp.xpNextLevel - xp.xpLastLevel)) *
                            100
                        )
                      )
                    : 0
                }
              />
              <span className="text-xs text-gray-500">
                {xp.streakLength}d streak x{xp.xpMultiplier}
              </span>
            </div>
          </div>
        )}
      </nav>
      <main className="max-w-xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="max-w-lg mx-auto px-4">
          {children}
          <Toaster
            visibleToasts={5}
            position="top-right"
            expand={true}
            richColors={false}
          />
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
