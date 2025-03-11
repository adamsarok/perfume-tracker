import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { Button } from "@/components/ui/button";
import { Brain, Cake, ChartArea, Gauge, House, List, Plus, Settings, Tag } from "lucide-react";
import Link from "next/link";
import { Toaster } from "@/components/ui/sonner"

const inter = Inter({ subsets: ["latin"] });

export const metadata: Metadata = {
  title: "Perfume Tracker",
  description: "Generated by create next app",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
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
              <Link href="/stats" passHref>
                <Button variant="outline" size="icon" title="Stats" aria-label="Stats">
                  <ChartArea />
                </Button>
              </Link>
              <Link href="/tags" passHref>
                <Button variant="outline" size="icon" title="Tags" aria-label="Tags">
                  <Tag />
                </Button>
              </Link>
              <Link href="/ai" passHref>
                <Button variant="outline" size="icon" title="Ai Recommendations" aria-label="Ai Recommendations">
                  <Brain />
                </Button>
              </Link>
              <Link href="/dashboard" passHref>
                <Button variant="outline" size="icon" title="Dashboard" aria-label="Dashboard">
                  <Gauge />
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
