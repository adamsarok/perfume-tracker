import { Brain, Cake, House, List, ListChecks, Plus, Settings, Tag } from "lucide-react"
import Link from "next/link"
import { usePathname } from "next/navigation"
 
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/components/ui/sidebar"
 
// Menu items for the perfume tracker app
const items = [
  {
    title: "Home",
    url: "/",
    icon: House,
  },
  {
    title: "Surprise Me",
    url: "/perfumes/surprise-me",
    icon: Cake,
  },
  {
    title: "Add New Perfume",
    url: "/perfumes/new-perfume",
    icon: Plus,
  },
  {
    title: "My Collection",
    url: "/perfumes",
    icon: List,
  },
  {
    title: "Tags",
    url: "/tags",
    icon: Tag,
  },
  {
    title: "Missions",
    url: "/progress",
    icon: ListChecks,
  },
  {
    title: "AI Recommendations",
    url: "/ai",
    icon: Brain,
  },
  {
    title: "Settings",
    url: "/settings",
    icon: Settings,
  },
]
 
export function AppSidebar() {
  const pathname = usePathname()

  return (
    <Sidebar collapsible="icon">
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>Perfume Tracker</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {items.map((item) => (
                <SidebarMenuItem key={item.title}>
                  <SidebarMenuButton asChild isActive={pathname === item.url}>
                    <Link href={item.url}>
                      <item.icon />
                      <span>{item.title}</span>
                    </Link>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
    </Sidebar>
  )
}