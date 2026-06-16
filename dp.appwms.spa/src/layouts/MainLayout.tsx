import React from "react";
import { Bell, User, LayoutDashboard, Database, Settings, FileText, ChevronDown } from "lucide-react";

interface MainLayoutProps {
    children: React.ReactNode;
    activeTab: string;
    setActiveTab: (tab: string) => void;
}

export default function MainLayout({ children, activeTab, setActiveTab }: MainLayoutProps) {
    const navItems = [
        { id: "dashboard", label: "Dashboard", icon: LayoutDashboard },
        { id: "master-data", label: "Master Data", icon: Database },
        { id: "rules", label: "Strategy & Rules Configuration", icon: Settings },
        { id: "audit", label: "System Audit Logs", icon: FileText },
    ];

    return (

        <div className="h-screen w-screen flex flex-col overflow-hidden bg-slate-50 dark:bg-slate-950 text-slate-800 dark:text-slate-100">

            <header className="h-16 border-b border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 px-6 flex items-center justify-between shrink-0 z-10 shadow-xs">

                <div className="flex items-center gap-3">

                    <span className="font-bold text-lg tracking-tight">WMS Front-Office</span>
                </div>

                {/* Navigation Menu */}
                <nav className="hidden md:flex items-center gap-1 h-full ml-8 flex-1">
                    {navItems.map((item) => {
                        const Icon = item.icon;
                        const isActive = activeTab === item.id;
                        return (
                            <button
                                key={item.id}
                                onClick={() => setActiveTab(item.id)}
                                className={`flex items-center gap-2 px-4 h-full border-b-2 text-sm font-medium transition-all cursor-pointer ${isActive
                                    ? "border-primary text-primary"
                                    : "border-transparent text-slate-500 hover:text-slate-800 dark:hover:text-slate-200"
                                    }`}
                            >
                                <Icon className="size-4" />
                                {item.label}
                            </button>
                        );
                    })}
                </nav>

                <div className="flex items-center gap-4">
                    <div className="flex items-center gap-1.5 px-3 py-1.5 bg-slate-100 dark:bg-slate-800 rounded-lg text-xs font-semibold text-slate-600 dark:text-slate-300">
                        <span>Kho: DUY PHAT 1</span>
                        <ChevronDown className="size-3" />
                    </div>

                    <button className="p-2 text-slate-500 hover:text-slate-800 dark:hover:text-slate-200 rounded-full hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors relative cursor-pointer">
                        <Bell className="size-5" />
                        <span className="absolute top-1 right-1 size-2 bg-red-500 rounded-full"></span>
                    </button>

                    {/* User Profile */}
                    <div className="flex items-center gap-2 border-l border-slate-200 dark:border-slate-800 pl-4">
                        <div className="size-8 rounded-full bg-slate-200 dark:bg-slate-700 flex items-center justify-center text-slate-600 dark:text-slate-300">
                            <User className="size-4" />
                        </div>
                        <div className="hidden lg:block text-left">
                            <div className="text-xs font-semibold leading-none">Admin User</div>
                            <span className="text-[10px] text-slate-400">Quản trị viên</span>
                        </div>
                    </div>
                </div>
            </header>

            <main className="flex-1 w-full overflow-hidden relative bg-slate-50 dark:bg-slate-950">
                {children}
            </main>
        </div>
    );
}
