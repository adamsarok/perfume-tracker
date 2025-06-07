"use client";

import "./globals.css";
import { Toaster } from "sonner";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { getCurrentUser, logout } from "@/services/axios-service";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Brain, Cake, House, List, ListChecks, Plus, Settings, Tag } from "lucide-react";

function LayoutContent({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(true);
  const [user, setUser] = useState<{ email: string } | null>(null);
  const [hasMounted, setHasMounted] = useState(false);

  useEffect(() => {
    setHasMounted(true);
  }, []);

  useEffect(() => {
    if (!hasMounted) return;

    console.log('Layout effect running, pathname:', pathname);

    const checkAuth = async () => {
      if (!hasMounted) return;

      console.log('Checking auth status...');
      try {
        const currentUser = await getCurrentUser();
        if (!hasMounted) return;

        console.log('Auth check result:', currentUser ? 'authenticated' : 'not authenticated');
        setUser(currentUser);

        // Only redirect if we're not on the login page and there's no user
        if (!currentUser && pathname !== '/login') {
          console.log('No user found, redirecting to login');
          router.push('/login');
        } else if (currentUser && pathname === '/login') {
          console.log('User found on login page, redirecting to home');
          router.push('/');
        }
      } catch (error) {
        if (!hasMounted) return;

        console.error('Auth check failed:', error);
        // Don't redirect on error if we're already on the login page
        if (pathname !== '/login') {
          console.log('Auth check error, redirecting to login');
          router.push('/login');
        }
      } finally {
        if (hasMounted) {
          setIsLoading(false);
        }
      }
    };

    // Don't check auth on the login page
    if (pathname === '/login') {
      console.log('On login page, skipping auth check');
      setIsLoading(false);
      return;
    }

    checkAuth();
  }, [pathname, hasMounted]);

  const handleLogout = async () => {
    if (!hasMounted) return;
    console.log('Logout initiated');
    try {
      await logout();
      setUser(null);
      console.log('Logout successful, redirecting to login');
      router.push('/login');
    } catch (error) {
      console.error('Logout failed:', error);
    }
  };

  // Don't show loading state on login page
  if (isLoading && pathname !== '/login') {
    console.log('Showing loading state');
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-gray-900"></div>
      </div>
    );
  }

  // Don't show navigation on login page
  if (pathname === '/login') {
    console.log('On login page, showing login form');
    return <>{children}</>;
  }

  console.log('Rendering main layout, user:', user?.email);
  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex">
              <div className="flex-shrink-0 flex items-center">
                <span className="text-xl font-bold text-gray-900">Perfume Tracker</span>
              </div>
            </div>
            {user && (
              <div className="flex items-center">
                <span className="text-gray-700 mr-4">{user.email}</span>
                <button
                  onClick={handleLogout}
                  className="text-gray-700 hover:text-gray-900"
                >
                  Logout
                </button>
              </div>
            )}
          </div>
        </div>
      </nav>
      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
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
        <Toaster />
        <LayoutContent>{children}</LayoutContent>
      </body>
    </html>
  );
}
