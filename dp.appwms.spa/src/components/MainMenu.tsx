import { useState, useEffect } from "react";
import { Link, NavLink, useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  LayoutDashboard,
  Database,
  Settings,
  FileText,
  Layers,
  Factory,
  Users,
  Sun,
  Moon,
} from "lucide-react";
import {
  NavigationMenu,
  NavigationMenuList,
  NavigationMenuItem,
  NavigationMenuContent,
  NavigationMenuTrigger,
  NavigationMenuLink,
} from "@/components/ui/navigation-menu";
import { Button } from "@/components/ui/button";

export default function MainMenu() {
  const { t } = useTranslation();
  const location = useLocation();

  const isMasterDataActive = location.pathname.startsWith("/master-data");

  // Prevent hover trigger logic of Radix UI NavigationMenu
  const preventHover = (e: React.PointerEvent) => {
    e.preventDefault();
  };

  // State to track current theme
  const [theme, setTheme] = useState(() => {
    if (typeof window !== "undefined") {
      const stored = localStorage.getItem("theme");
      if (stored) return stored;
      const systemPrefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
      return systemPrefersDark ? "dark" : "light";
    }
    return "light";
  });

  // Apply theme class to document element on theme changes
  useEffect(() => {
    const root = window.document.documentElement;
    if (theme === "dark") {
      root.classList.add("dark");
      localStorage.setItem("theme", "dark");
    } else {
      root.classList.remove("dark");
      localStorage.setItem("theme", "light");
    }
  }, [theme]);

  const toggleTheme = () => {
    setTheme((prev) => (prev === "dark" ? "light" : "dark"));
  };

  return (
    <NavigationMenu className="hidden md:flex flex-1">
      <NavigationMenuList className="gap-1">
        {/* 1. Dashboard Link */}
        <NavigationMenuItem>
          <NavigationMenuLink asChild>
            <NavLink
              to="/dashboard"
              className={({ isActive }) =>
                `flex items-center gap-2 px-3 py-2 text-sm transition-all rounded-md ${isActive
                  ? "text-primary bg-slate-100 dark:bg-slate-800 font-semibold"
                  : "text-slate-500 hover:text-slate-800 dark:hover:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-800/30 font-medium"
                }`
              }
            >
              <LayoutDashboard className="size-4" />
              {t("translation:navigation.dashboard")}
            </NavLink>
          </NavigationMenuLink>
        </NavigationMenuItem>

        {/* 2. Master Data (Dropdown Menu, click-only) */}
        <NavigationMenuItem>
          <NavigationMenuTrigger
            onPointerEnter={preventHover}
            onPointerLeave={preventHover}
            onPointerMove={preventHover}
            className={`text-sm transition-all ${isMasterDataActive
              ? "text-primary bg-slate-100 dark:bg-slate-800 font-semibold"
              : "text-slate-500 hover:text-slate-800 dark:hover:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-800/30 font-medium"
              }`}
          >
            <Database className="size-4 mr-2" />
            {t("translation:navigation.masterData")}
          </NavigationMenuTrigger>
          <NavigationMenuContent
            onPointerEnter={preventHover}
            onPointerLeave={preventHover}
            onPointerMove={preventHover}
            className="p-1.5 bg-popover text-popover-foreground border border-border rounded-lg shadow-md min-w-[200px]"
          >
            <ul className="flex flex-col gap-1 w-full">
              <li>
                <NavigationMenuLink asChild>
                  <Link
                    to="/master-data/skus"
                    className={`flex items-center gap-2.5 px-3 py-2 text-sm rounded-md transition-colors hover:bg-accent hover:text-accent-foreground ${location.pathname === "/master-data/skus"
                      ? "font-semibold text-primary bg-accent/50"
                      : "font-medium"
                      }`}
                  >
                    <Layers className="size-4 text-slate-400" />
                    {t("translation:navigation.skus")}
                  </Link>
                </NavigationMenuLink>
              </li>
              <li>
                <NavigationMenuLink asChild>
                  <Link
                    to="/master-data/suppliers"
                    className={`flex items-center gap-2.5 px-3 py-2 text-sm rounded-md transition-colors hover:bg-accent hover:text-accent-foreground ${location.pathname === "/master-data/suppliers"
                      ? "font-semibold text-primary bg-accent/50"
                      : "font-medium"
                      }`}
                  >
                    <Factory className="size-4 text-slate-400" />
                    {t("translation:navigation.suppliers")}
                  </Link>
                </NavigationMenuLink>
              </li>
              <li>
                <NavigationMenuLink asChild>
                  <Link
                    to="/master-data/customers"
                    className={`flex items-center gap-2.5 px-3 py-2 text-sm rounded-md transition-colors hover:bg-accent hover:text-accent-foreground ${location.pathname === "/master-data/customers"
                      ? "font-semibold text-primary bg-accent/50"
                      : "font-medium"
                      }`}
                  >
                    <Users className="size-4 text-slate-400" />
                    {t("translation:navigation.customers")}
                  </Link>
                </NavigationMenuLink>
              </li>
            </ul>
          </NavigationMenuContent>
        </NavigationMenuItem>

        {/* 3. Strategy & Rules Link */}
        <NavigationMenuItem>
          <NavigationMenuLink asChild>
            <NavLink
              to="/rules"
              className={({ isActive }) =>
                `flex items-center gap-2 px-3 py-2 text-sm transition-all rounded-md ${isActive
                  ? "text-primary bg-slate-100 dark:bg-slate-800 font-semibold"
                  : "text-slate-500 hover:text-slate-800 dark:hover:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-800/30 font-medium"
                }`
              }
            >
              <Settings className="size-4" />
              {t("translation:navigation.rules")}
            </NavLink>
          </NavigationMenuLink>
        </NavigationMenuItem>

        {/* 4. System Audit Logs Link */}
        <NavigationMenuItem>
          <NavigationMenuLink asChild>
            <NavLink
              to="/audit"
              className={({ isActive }) =>
                `flex items-center gap-2 px-3 py-2 text-sm transition-all rounded-md ${isActive
                  ? "text-primary bg-slate-100 dark:bg-slate-800 font-semibold"
                  : "text-slate-500 hover:text-slate-800 dark:hover:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-800/30 font-medium"
                }`
              }
            >
              <FileText className="size-4" />
              {t("translation:navigation.audit")}
            </NavLink>
          </NavigationMenuLink>
        </NavigationMenuItem>

        {/* 5. Theme Toggle Switch */}
        <NavigationMenuItem className="ml-2">
          <Button
            variant="ghost"
            size="icon"
            onClick={toggleTheme}
            aria-label="Toggle Theme"
            className="text-slate-500 hover:text-slate-800 dark:text-slate-400 dark:hover:text-slate-200 rounded-full cursor-pointer hover:bg-slate-100 dark:hover:bg-slate-800/30"
          >
            {theme === "dark" ? (
              <Sun className="size-4 text-amber-500 animate-[spin-slow]" />
            ) : (
              <Moon className="size-4 text-indigo-500" />
            )}
          </Button>
        </NavigationMenuItem>
      </NavigationMenuList>
    </NavigationMenu>
  );
}


