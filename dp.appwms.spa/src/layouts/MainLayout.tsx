import { Outlet } from "react-router-dom";
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

import MainMenu from "../components/MainMenu";

export default function MainLayout() {
  const { t, i18n } = useTranslation();

  const toggleLanguage = () => {
    const nextLang = i18n.language === "vi" ? "en" : "vi";
    i18n.changeLanguage(nextLang);
  };

  return (
    <div className="h-screen w-screen flex flex-col overflow-hidden bg-slate-50 dark:bg-slate-950 text-slate-800 dark:text-slate-100">

      <header className="h-16 border-b border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 px-6 flex items-center justify-between shrink-0 z-10 shadow-sm">

        <div className="flex items-center gap-3">
          <span className="font-bold text-lg tracking-tight">WMS Front-Office</span>
        </div>

        {/* Navigation Menu */}
        <MainMenu />


        <div className="flex items-center gap-4">
          <div className="flex items-center gap-1.5 px-3 py-1.5 bg-slate-100 dark:bg-slate-800 rounded-lg text-xs font-semibold text-slate-600 dark:text-slate-300">
            <span>{t("translation:navigation.warehouse")}: DUY PHAT 1</span>
            <ChevronDown className="size-3" />
          </div>


          <button
            onClick={toggleLanguage}
            title={i18n.language === "vi" ? "Switch to English" : "Chuyển sang Tiếng Việt"}
            className="flex items-center justify-center gap-1 px-2.5 py-1.5 text-xs font-bold text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg cursor-pointer transition-all border border-slate-200 dark:border-slate-700"
          >
            <Globe className="size-3.5" />
            <span>{i18n.language.toUpperCase()}</span>
          </button>

          <button className="p-2 text-slate-500 hover:text-slate-800 dark:hover:text-slate-200 rounded-full hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors relative cursor-pointer">
            <Bell className="size-5" />
            <span className="absolute top-1.5 right-1.5 size-2 bg-red-500 rounded-full"></span>
          </button>

          {/* User Profile */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button className="flex items-center gap-2 border-l border-slate-200 dark:border-slate-800 pl-4 cursor-pointer focus:outline-none">
                <Avatar className="size-8">
                  <AvatarImage src="https://github.com/shadcn.png" alt="Admin User" />
                  <AvatarFallback className="bg-primary text-white text-xs">AD</AvatarFallback>
                </Avatar>
                <div className="hidden lg:block text-left">
                  <div className="text-xs font-semibold leading-none">Admin User</div>
                  <span className="text-[10px] text-slate-400">{t("translation:navigation.admin")}</span>
                </div>
                <ChevronDown className="size-3 text-slate-400" />
              </button>
            </DropdownMenuTrigger>

            <DropdownMenuContent align="end" className="w-56 mt-1 z-50">
              <DropdownMenuItem className="cursor-pointer">
                <User className="size-4 mr-2 text-slate-400" />
                <span>{t("translation:navigation.profile")}</span>
              </DropdownMenuItem>

              <DropdownMenuItem className="cursor-pointer">
                <Settings className="size-4 mr-2 text-slate-400" />
                <span>{t("translation:navigation.settings")}</span>
              </DropdownMenuItem>

              <DropdownMenuSeparator />

              <DropdownMenuItem className="cursor-pointer text-red-600 dark:text-red-400 focus:bg-red-50 dark:focus:bg-red-950/20">
                <LogOut className="size-4 mr-2" />
                <span>{t("translation:navigation.logout")}</span>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

      </header>

      <main className="flex-1 w-full overflow-hidden relative bg-slate-50 dark:bg-slate-950">
        <Outlet />
      </main>
    </div>
  );
}


