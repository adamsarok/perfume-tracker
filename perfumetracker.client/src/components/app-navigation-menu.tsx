import {
    NavigationMenu,
    NavigationMenuContent,
    NavigationMenuItem,
    NavigationMenuLink,
    NavigationMenuList,
    NavigationMenuTrigger,
} from "@/components/ui/navigation-menu"
import { Brain, Cake, House, List, ListChecks, Plus, Settings, Tag } from "lucide-react"
import Link from "next/link"

export default function AppNavigationMenu() {
    return (
        <NavigationMenu>
            <NavigationMenuList>
                <NavigationMenuItem>
                    <NavigationMenuTrigger>Perfume Tracker</NavigationMenuTrigger>
                    <NavigationMenuContent>
                        <ul className="grid w-[300px] gap-2 p-3 md:w-[400px] md:grid-cols-2 lg:w-[500px]">
                            <li className="row-span-2">
                                <NavigationMenuLink asChild>
                                    <Link
                                        className="flex h-full w-full select-none flex-col justify-end rounded-md bg-gradient-to-b from-muted/50 to-muted p-4 no-underline outline-none focus:shadow-md"
                                        href="/"
                                    >
                                        <House className="h-5 w-5" />
                                        <div className="mb-1 mt-3 text-base font-medium">
                                            Home
                                        </div>
                                        <p className="text-xs leading-tight text-muted-foreground">
                                            Return to the main dashboard
                                        </p>
                                    </Link>
                                </NavigationMenuLink>
                            </li>
                            <li>
                                <NavigationMenuLink asChild>
                                    <Link
                                        className="block select-none space-y-1 rounded-md p-2 leading-none no-underline outline-none transition-colors hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground"
                                        href="/perfumes/surprise-me"
                                    >
                                        <div className="flex items-center gap-2">
                                            <Cake className="h-4 w-4" />
                                            <div className="text-sm font-medium leading-none">Surprise Me</div>
                                        </div>
                                        <p className="line-clamp-2 text-xs leading-snug text-muted-foreground">
                                            Get a random perfume recommendation
                                        </p>
                                    </Link>
                                </NavigationMenuLink>
                            </li>
                            <li>
                                <NavigationMenuLink asChild>
                                    <Link
                                        className="block select-none space-y-1 rounded-md p-2 leading-none no-underline outline-none transition-colors hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground"
                                        href="/perfumes/recommendations"
                                    >
                                        <div className="flex items-center gap-2">
                                            <Cake className="h-4 w-4" />
                                            <div className="text-sm font-medium leading-none">Surprise Me</div>
                                        </div>
                                        <p className="line-clamp-2 text-xs leading-snug text-muted-foreground">
                                            Get smart recommendations
                                        </p>
                                    </Link>
                                </NavigationMenuLink>
                            </li>
                            <li>
                                <NavigationMenuLink asChild>
                                    <Link
                                        className="block select-none space-y-1 rounded-md p-2 leading-none no-underline outline-none transition-colors hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground"
                                        href="/perfumes/new-perfume"
                                    >
                                        <div className="flex items-center gap-2">
                                            <Plus className="h-4 w-4" />
                                            <div className="text-sm font-medium leading-none">Add New Perfume</div>
                                        </div>
                                        <p className="line-clamp-2 text-xs leading-snug text-muted-foreground">
                                            Add a new perfume to your collection
                                        </p>
                                    </Link>
                                </NavigationMenuLink>
                            </li>
                            <li>
                                <NavigationMenuLink asChild>
                                    <Link
                                        className="block select-none space-y-1 rounded-md p-2 leading-none no-underline outline-none transition-colors hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground"
                                        href="/perfumes"
                                    >
                                        <div className="flex items-center gap-2">
                                            <List className="h-4 w-4" />
                                            <div className="text-sm font-medium leading-none">My Collection</div>
                                        </div>
                                        <p className="line-clamp-2 text-xs leading-snug text-muted-foreground">
                                            Browse your perfume collection
                                        </p>
                                    </Link>
                                </NavigationMenuLink>
                            </li>
                            <li>
                                <NavigationMenuLink asChild>
                                    <Link
                                        className="block select-none space-y-1 rounded-md p-2 leading-none no-underline outline-none transition-colors hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground"
                                        href="/tags"
                                    >
                                        <div className="flex items-center gap-2">
                                            <Tag className="h-4 w-4" />
                                            <div className="text-sm font-medium leading-none">Tags</div>
                                        </div>
                                        <p className="line-clamp-2 text-xs leading-snug text-muted-foreground">
                                            Manage your perfume tags
                                        </p>
                                    </Link>
                                </NavigationMenuLink>
                            </li>
                            <li>
                                <NavigationMenuLink asChild>
                                    <Link
                                        className="block select-none space-y-1 rounded-md p-2 leading-none no-underline outline-none transition-colors hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground"
                                        href="/progress"
                                    >
                                        <div className="flex items-center gap-2">
                                            <ListChecks className="h-4 w-4" />
                                            <div className="text-sm font-medium leading-none">Missions</div>
                                        </div>
                                        <p className="line-clamp-2 text-xs leading-snug text-muted-foreground">
                                            Track your missions and achievements
                                        </p>
                                    </Link>
                                </NavigationMenuLink>
                            </li>
                            <li>
                                <NavigationMenuLink asChild>
                                    <Link
                                        className="block select-none space-y-1 rounded-md p-2 leading-none no-underline outline-none transition-colors hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground"
                                        href="/ai"
                                    >
                                        <div className="flex items-center gap-2">
                                            <Brain className="h-4 w-4" />
                                            <div className="text-sm font-medium leading-none">AI Recommendations</div>
                                        </div>
                                        <p className="line-clamp-2 text-xs leading-snug text-muted-foreground">
                                            Get AI-powered perfume suggestions
                                        </p>
                                    </Link>
                                </NavigationMenuLink>
                            </li>
                            <li>
                                <NavigationMenuLink asChild>
                                    <Link
                                        className="block select-none space-y-1 rounded-md p-2 leading-none no-underline outline-none transition-colors hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground"
                                        href="/settings"
                                    >
                                        <div className="flex items-center gap-2">
                                            <Settings className="h-4 w-4" />
                                            <div className="text-sm font-medium leading-none">Settings</div>
                                        </div>
                                        <p className="line-clamp-2 text-xs leading-snug text-muted-foreground">
                                            Manage your account settings
                                        </p>
                                    </Link>
                                </NavigationMenuLink>
                            </li>
                        </ul>
                    </NavigationMenuContent>
                </NavigationMenuItem>
            </NavigationMenuList>
        </NavigationMenu>

    )
}