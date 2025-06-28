"use client";

import "./globals.css";
import { Toaster } from "sonner";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { getCurrentUser, logoutUser } from "@/services/user-service";
import { UserMissionDto } from "@/dto/MissionDto";
import * as signalR from "@microsoft/signalr";
import { toast } from "sonner";
import { initializeApiUrl } from "@/services/axios-service";
import { useUserStore } from "@/stores/user-store";
import { generateMissions } from "@/services/mission-service";
import { SidebarProvider, SidebarTrigger, SidebarInset } from "@/components/ui/sidebar"
import { AppSidebar } from "@/components/app-sidebar";

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
    <div className="flex h-screen">
      <AppSidebar />
      <SidebarInset>
        <header className="flex h-16 shrink-0 items-center gap-2 transition-[width,height] ease-linear group-has-[[data-collapsible=icon]]/sidebar-wrapper:h-12">
          <div className="flex items-center gap-2 px-4">
            <SidebarTrigger className="-ml-1" />
            <span className="text-lg font-semibold">Perfume Tracker</span>
          </div>
          <div className="ml-auto flex items-center gap-2 px-4">
            {user && (
              <div className="flex items-center gap-2">
                <span className="text-sm text-muted-foreground">{user.email}</span>
                <Button variant="outline" size="sm" onClick={handleLogout}>
                  Logout
                </Button>
              </div>
            )}
          </div>
        </header>
        <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
          {children}
        </div>
      </SidebarInset>
      <Toaster />
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
        <SidebarProvider>
          <LayoutContent>{children}</LayoutContent>
        </SidebarProvider>
      </body>
    </html>
  );
}
