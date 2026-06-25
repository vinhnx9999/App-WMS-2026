import { Outlet } from "react-router-dom";
import { useEffect } from "react";
import { useAuthStore } from "@/store/auth-store";
import { useWarehouseStore } from "@/store/warehouse-store";
import {
  Bell, User, Settings, ChevronDown, LogOut, Globe
} from "lucide-react";
import { useTranslation } from "react-i18next";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu";

import {
  Avatar,
  AvatarFallback,
  AvatarImage
} from "@/components/ui/avatar";

import TopMenu from "../components/TopMenu";

export default function MainLayout() {
  const { t, i18n } = useTranslation();
  const { user, isAuthenticated, logout } = useAuthStore();
  const {
    warehouses,
    selectedWarehouse,
    isLoading: isWarehouseLoading,
    fetchWarehouses,
    setSelectedWarehouse,
    clearSelection
  } = useWarehouseStore();

  useEffect(() => {
    if (isAuthenticated) {
      fetchWarehouses();
    }
  }, [isAuthenticated, fetchWarehouses]);

  const handleLogout = () => {
    clearSelection();
    logout();
  };

  const toggleLanguage = () => {
    const nextLang = i18n.language === "vi" ? "en" : "vi";
    i18n.changeLanguage(nextLang);
  };

  const getInitials = (name?: string) => {
    if (!name) return "US";
    const parts = name.split(" ");
    return parts.map(p => p[0]).join("").substring(0, 2).toUpperCase();
  };

  return (
    <div className="h-screen w-screen flex flex-col overflow-hidden bg-background text-foreground">

      <header className="h-16 border-b border-border bg-card text-card-foreground px-6 flex items-center justify-between shrink-0 z-10 shadow-sm">

        <div className="flex items-center gap-3">
          <span className="font-bold text-lg tracking-tight">WMS Front-Office</span>
        </div>

        {/* Navigation Menu */}
        <TopMenu />

        <div className="flex items-center gap-4">
          {/* Warehouse Switcher */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button className="flex items-center gap-1.5 px-3 py-1.5 bg-muted hover:bg-muted/80 rounded-lg text-xs font-semibold text-muted-foreground cursor-pointer focus:outline-none transition-colors border border-transparent hover:border-border">
                <span>
                  {t("translation:navigation.warehouse")}:{" "}
                  {selectedWarehouse ? selectedWarehouse.name : ""}
                </span>
                <ChevronDown className="size-3" />
              </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-56 z-50">
              {isWarehouseLoading ? (
                <DropdownMenuItem disabled className="text-xs text-muted-foreground">
                  {t("translation:common.loading")}
                </DropdownMenuItem>
              ) : warehouses.length === 0 ? (
                <DropdownMenuItem disabled className="text-xs text-muted-foreground">
                  {t("translation:common.noWarehouse")}
                </DropdownMenuItem>
              ) : (
                warehouses.map((wh) => (
                  <DropdownMenuItem
                    key={wh.id}
                    className={`cursor-pointer text-xs ${selectedWarehouse?.id === wh.id ? "font-bold text-primary" : ""
                      }`}
                    onSelect={() => setSelectedWarehouse(wh)}
                  >
                    {wh.name}
                  </DropdownMenuItem>
                ))
              )}
            </DropdownMenuContent>
          </DropdownMenu>


          <button
            onClick={toggleLanguage}
            title={i18n.language === "vi" ? "Switch to English" : "Chuyển sang Tiếng Việt"}
            className="flex items-center justify-center gap-1 px-2.5 py-1.5 text-xs font-bold text-muted-foreground hover:bg-accent hover:text-accent-foreground rounded-lg cursor-pointer transition-all border border-border"
          >
            <Globe className="size-3.5" />
            <span>{i18n.language.toUpperCase()}</span>
          </button>

          <button className="p-2 text-muted-foreground hover:text-foreground rounded-full hover:bg-accent transition-colors relative cursor-pointer">
            <Bell className="size-5" />
            <span className="absolute top-1.5 right-1.5 size-2 bg-red-500 rounded-full"></span>
          </button>

          {/* User Profile */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button className="flex items-center gap-2 border-l border-border pl-4 cursor-pointer focus:outline-none">
                <Avatar className="size-8">
                  <AvatarImage src="" alt={user?.fullName || "User"} />
                  <AvatarFallback className="bg-primary text-primary-foreground text-xs">
                    {getInitials(user?.fullName)}
                  </AvatarFallback>
                </Avatar>
                <div className="hidden lg:block text-left">
                  <div className="text-xs font-semibold leading-none">{user?.fullName || "Guest"}</div>
                  <span className="text-[10px] text-muted-foreground">{user?.role || t("translation:navigation.admin")}</span>
                </div>
                <ChevronDown className="size-3 text-muted-foreground" />
              </button>
            </DropdownMenuTrigger>

            <DropdownMenuContent align="end" className="w-56 mt-1 z-50">
              <DropdownMenuItem className="cursor-pointer">
                <User className="size-4 mr-2 text-muted-foreground" />
                <span>{t("translation:navigation.profile")}</span>
              </DropdownMenuItem>

              <DropdownMenuItem className="cursor-pointer">
                <Settings className="size-4 mr-2 text-muted-foreground" />
                <span>{t("translation:navigation.settings")}</span>
              </DropdownMenuItem>

              <DropdownMenuSeparator />

              <DropdownMenuItem
                onSelect={handleLogout}
                className="cursor-pointer text-destructive focus:bg-destructive/10"
              >
                <LogOut className="size-4 mr-2" />
                <span>{t("translation:navigation.logout")}</span>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

      </header>

      <main className="flex-1 w-full overflow-hidden relative bg-background">
        <Outlet />
      </main>
    </div>
  );
}


