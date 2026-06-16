import { RefreshCw, Layers, TrendingUp, MapPin, Play, CheckCircle2, ShieldAlert } from "lucide-react";

export default function DashboardPage() {

    return (
        <div className="h-full w-full overflow-y-auto p-6 space-y-6">

            {/* Tiêu đề trang Dashboard */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight">WMS Dashboard Overview</h1>
                    <p className="text-sm text-slate-500">Giám sát hoạt động và tài nguyên kho hàng theo thời gian thực</p>
                </div>
                <button className="flex items-center gap-2 px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg text-sm hover:bg-slate-50 dark:hover:bg-slate-800 font-medium cursor-pointer">
                    <RefreshCw className="size-4 animate-spin-slow" />
                    Làm mới
                </button>
            </div>
            {/* 1. Hàng KPI Cards */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                {/* KPI 1: Storage Utilization */}
                <div className="p-5 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 shadow-xs">
                    <div className="flex items-center justify-between text-slate-400">
                        <span className="text-xs font-semibold uppercase tracking-wider">Storage Utilization</span>
                        <Layers className="size-5 text-blue-500" />
                    </div>
                    <div className="mt-3 flex items-baseline gap-2">
                        <span className="text-3xl font-bold">74.8%</span>
                        <span className="text-xs text-emerald-500 flex items-center font-medium">
                            <TrendingUp className="size-3 mr-0.5" /> +2.1%
                        </span>
                    </div>
                    <div className="mt-4 w-full bg-slate-100 dark:bg-slate-800 h-2 rounded-full overflow-hidden">
                        <div className="bg-blue-500 h-full rounded-full" style={{ width: "74.8%" }}></div>
                    </div>
                </div>
                {/* KPI 2: Active Pallet */}
                <div className="p-5 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 shadow-xs">
                    <div className="flex items-center justify-between text-slate-400">
                        <span className="text-xs font-semibold uppercase tracking-wider">Active Pallet</span>
                        <MapPin className="size-5 text-indigo-500" />
                    </div>
                    <div className="mt-3 flex items-baseline gap-2">
                        <span className="text-3xl font-bold">14 / 16</span>
                        <span className="text-xs text-slate-400">Available</span>
                    </div>
                    <div className="mt-4 flex items-center gap-1.5 text-xs text-emerald-500 font-medium">
                        <span className="size-2 bg-emerald-500 rounded-full animate-pulse"></span>
                        Trực quan vị trí bình thường
                    </div>
                </div>
                {/* KPI 3: Pending Putaway Rules */}
                <div className="p-5 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 shadow-xs">
                    <div className="flex items-center justify-between text-slate-400">
                        <span className="text-xs font-semibold uppercase tracking-wider">Pending Putaway Rules</span>
                        <Play className="size-5 text-amber-500" />
                    </div>
                    <div className="mt-3 flex items-baseline gap-2">
                        <span className="text-3xl font-bold">5</span>
                        <span className="text-xs text-amber-500 font-medium">Queued</span>
                    </div>
                    <div className="mt-4 w-full bg-slate-100 dark:bg-slate-800 h-2 rounded-full overflow-hidden">
                        <div className="bg-amber-500 h-full rounded-full" style={{ width: "35%" }}></div>
                    </div>
                </div>
                {/* KPI 4: Rule Execution Success */}
                <div className="p-5 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 shadow-xs">
                    <div className="flex items-center justify-between text-slate-400">
                        <span className="text-xs font-semibold uppercase tracking-wider">Rule Execution Success</span>
                        <CheckCircle2 className="size-5 text-emerald-500" />
                    </div>
                    <div className="mt-3 flex items-baseline gap-2">
                        <span className="text-3xl font-bold">99.8%</span>
                        <span className="text-xs text-emerald-500 flex items-center font-medium">
                            <TrendingUp className="size-3 mr-0.5" /> +0.4%
                        </span>
                    </div>
                    <div className="mt-4 flex items-end gap-1 h-3">
                        {[30, 45, 60, 50, 75, 90, 100].map((h, i) => (
                            <div key={i} className="flex-1 bg-blue-100 dark:bg-slate-800 rounded-sm h-full relative overflow-hidden">
                                <div className="bg-blue-500 absolute bottom-0 left-0 right-0 rounded-sm" style={{ height: `${h}%` }}></div>
                            </div>
                        ))}
                    </div>
                </div>
            </div>
            {/* 2. Phần dữ liệu chính (2 cột) */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">

                {/* Cột trái: Chiếm 2 phần (Charts & Statistics) */}
                <div className="lg:col-span-2 space-y-6">

                    {/* Biểu đồ Nhập/Xuất kho */}
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                        {/* Inbound volume */}
                        <div className="p-5 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800">
                            <h3 className="text-sm font-semibold text-slate-500 dark:text-slate-400 uppercase">Inbound Volume (SKU/h)</h3>
                            <div className="h-40 mt-4 flex items-end gap-2">
                                {[40, 60, 50, 80, 55, 95].map((val, idx) => (
                                    <div key={idx} className="flex-1 flex flex-col items-center gap-1.5 h-full justify-end">
                                        <div className="w-full bg-blue-500/20 hover:bg-blue-500/35 rounded-t-md transition-all relative group" style={{ height: `${val}%` }}>
                                            <span className="absolute -top-6 left-1/2 -translate-x-1/2 bg-slate-800 text-white text-[10px] px-1.5 py-0.5 rounded opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap">{val} SKU</span>
                                        </div>
                                        <span className="text-[10px] text-slate-400">{8 + idx * 2}am</span>
                                    </div>
                                ))}
                            </div>
                        </div>
                        {/* Outbound volume */}
                        <div className="p-5 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800">
                            <h3 className="text-sm font-semibold text-slate-500 dark:text-slate-400 uppercase">Outbound Volume (Orders)</h3>
                            <div className="h-40 mt-4 relative">
                                {/* SVG Line Chart */}
                                <svg className="w-full h-full" viewBox="0 0 100 40" preserveAspectRatio="none">
                                    <path d="M 0 35 Q 20 25 40 28 T 80 10 T 100 5" fill="none" stroke="#10b981" strokeWidth="2" />
                                    <path d="M 0 35 Q 20 25 40 28 T 80 10 T 100 5 L 100 40 L 0 40 Z" fill="url(#grad)" opacity="0.1" />
                                    <defs>
                                        <linearGradient id="grad" x1="0%" y1="0%" x2="0%" y2="100%">
                                            <stop offset="0%" stopColor="#10b981" />
                                            <stop offset="100%" stopColor="#10b981" stopOpacity="0" />
                                        </linearGradient>
                                    </defs>
                                </svg>
                                <div className="absolute bottom-0 left-0 right-0 flex justify-between px-1 text-[10px] text-slate-400 mt-2">
                                    <span>8am</span>
                                    <span>12pm</span>
                                    <span>4pm</span>
                                    <span>8pm</span>
                                </div>
                            </div>
                        </div>
                    </div>
                    {/* Phân bổ các Zone & Dock Rates */}
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">

                        {/* Dock Utilization Rates */}
                        <div className="p-5 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800">
                            <h3 className="text-sm font-semibold text-slate-500 dark:text-slate-400 uppercase mb-4">Dock Utilization Rates</h3>
                            <div className="space-y-4">
                                {[
                                    { name: "Dock A (Receiving)", value: 85, color: "bg-blue-500" },
                                    { name: "Dock B (Shipping)", value: 62, color: "bg-amber-500" },
                                    { name: "Dock C (Cross-dock)", value: 24, color: "bg-teal-500" },
                                ].map((dock, idx) => (
                                    <div key={idx} className="space-y-1.5">
                                        <div className="flex justify-between text-xs font-medium">
                                            <span>{dock.name}</span>
                                            <span>{dock.value}%</span>
                                        </div>
                                        <div className="w-full bg-slate-100 dark:bg-slate-800 h-2 rounded-full overflow-hidden">
                                            <div className={`${dock.color} h-full rounded-full`} style={{ width: `${dock.value}%` }}></div>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                        {/* Storage Zone Capacity Distribution */}
                        <div className="p-5 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800">
                            <h3 className="text-sm font-semibold text-slate-500 dark:text-slate-400 uppercase mb-4">Storage Zone Capacity</h3>
                            <div className="space-y-4">
                                {[
                                    { zone: "Zone A (Shuttle Rack)", value: 82, color: "bg-indigo-500" },
                                    { zone: "Zone B (Mezzanine)", value: 49, color: "bg-sky-500" },
                                    { zone: "Zone C (Bulk Area)", value: 91, color: "bg-orange-500" },
                                ].map((zone, idx) => (
                                    <div key={idx} className="space-y-1.5">
                                        <div className="flex justify-between text-xs font-medium">
                                            <span>{zone.zone}</span>
                                            <span>{zone.value}%</span>
                                        </div>
                                        <div className="w-full bg-slate-100 dark:bg-slate-800 h-2 rounded-full overflow-hidden">
                                            <div className={`${zone.color} h-full rounded-full`} style={{ width: `${zone.value}%` }}></div>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    </div>
                </div>
                {/* Cột phải: Cảnh báo Rule Conflict & Alerts */}
                <div className="p-5 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 flex flex-col h-full min-h-[400px]">
                    <div className="flex items-center justify-between mb-4">
                        <div className="flex items-center gap-2">
                            <ShieldAlert className="size-5 text-red-500" />
                            <h3 className="font-semibold text-slate-800 dark:text-slate-100">Rule Conflicts & Alerts</h3>
                        </div>
                        <span className="text-[10px] font-bold px-2 py-0.5 bg-red-100 text-red-700 rounded-full dark:bg-red-950 dark:text-red-300">
                            4 Cần Xử Lý
                        </span>
                    </div>
                    {/* List danh sách Alerts */}
                    <div className="space-y-3 flex-1 overflow-y-auto pr-1">
                        {[
                            {
                                time: "14:22:05",
                                type: "CRITICAL",
                                title: "SKU allocation conflict in Zone A",
                                desc: "Rule ID #R-002 attempts to assign bin location already locked by active picking task.",
                                action: "Resolve"
                            },
                            {
                                time: "14:15:30",
                                type: "WARNING",
                                title: "Putaway strategy validation failed",
                                desc: "Dimensions of incoming pallet exceed maximum allowed for configured Zone B racks.",
                                action: "Amend Rule"
                            },
                            {
                                time: "13:58:12",
                                type: "WARNING",
                                title: "Shuttle routing bottleneck detected",
                                desc: "High traffic in Aisle 4 causing >30s delays in retrieval tasks.",
                                action: "Re-route"
                            },
                            {
                                time: "11:04:45",
                                type: "RESOLVED",
                                title: "Weight limit exception in Zone C",
                                desc: "Automatically resolved by secondary spillover rule #R-102.",
                                action: ""
                            }
                        ].map((alert, idx) => (
                            <div
                                key={idx}
                                className={`p-3.5 rounded-lg border text-xs transition-all hover:bg-slate-50 dark:hover:bg-slate-800/50 ${alert.type === "CRITICAL"
                                    ? "bg-red-50/50 border-red-100 dark:bg-red-950/20 dark:border-red-900/30"
                                    : alert.type === "WARNING"
                                        ? "bg-amber-50/50 border-amber-100 dark:bg-amber-950/20 dark:border-amber-900/30"
                                        : "bg-slate-50/50 border-slate-100 dark:bg-slate-800/20 dark:border-slate-800/30 opacity-70"
                                    }`}
                            >
                                <div className="flex items-center justify-between mb-1">
                                    <span className="text-[10px] text-slate-400 font-mono">{alert.time}</span>
                                    <span className={`text-[9px] font-bold px-1.5 py-0.2 rounded-sm ${alert.type === "CRITICAL"
                                        ? "bg-red-100 text-red-700 dark:bg-red-950 dark:text-red-300"
                                        : alert.type === "WARNING"
                                            ? "bg-amber-100 text-amber-700 dark:bg-amber-950 dark:text-amber-300"
                                            : "bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300"
                                        }`}>{alert.type}</span>
                                </div>
                                <h4 className="font-semibold text-slate-800 dark:text-slate-100 mb-1">{alert.title}</h4>
                                <p className="text-slate-500 dark:text-slate-400 leading-relaxed">{alert.desc}</p>
                                {alert.action && (
                                    <button className="mt-2.5 px-2.5 py-1 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded text-[10px] font-semibold hover:bg-slate-50 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300 cursor-pointer">
                                        {alert.action}
                                    </button>
                                )}
                            </div>
                        ))}
                    </div>
                </div>
            </div>
            {/* 3. Phần Warehouse Layout Map ở chân trang */}
            <div className="p-5 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800">
                <div className="flex items-center justify-between mb-4">
                    <div className="flex items-center gap-2">
                        <Layers className="size-5 text-emerald-500" />
                        <h3 className="font-semibold">Warehouse Layout Map</h3>
                    </div>
                    <div className="flex items-center gap-3 text-xs">
                        <div className="flex items-center gap-1.5">
                            <span className="size-2.5 bg-emerald-500 rounded-xs"></span>
                            <span>Sẵn sàng</span>
                        </div>
                        <div className="flex items-center gap-1.5">
                            <span className="size-2.5 bg-blue-500 rounded-xs"></span>
                            <span>Đang chiếm giữ</span>
                        </div>
                        <div className="flex items-center gap-1.5">
                            <span className="size-2.5 bg-red-500 rounded-xs"></span>
                            <span>Xung đột/Lỗi</span>
                        </div>
                        <div className="flex items-center gap-1.5">
                            <span className="size-2.5 bg-amber-500 rounded-xs"></span>
                            <span>Cảnh báo</span>
                        </div>
                    </div>
                </div>
                {/* Trực quan Layout Grid */}
                <div className="w-full bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg p-6 flex flex-col items-center justify-center">
                    <div className="w-full max-w-4xl grid grid-cols-12 gap-1.5">
                        {Array.from({ length: 48 }).map((_, i) => {
                            let color = "bg-emerald-500/20 border-emerald-500/40 hover:bg-emerald-500/30";
                            if (i === 12 || i === 15 || i === 33) {
                                color = "bg-red-500/20 border-red-500/40 hover:bg-red-500/30 animate-pulse";
                            } else if (i === 8 || i === 19 || i === 41 || i === 42) {
                                color = "bg-amber-500/20 border-amber-500/40 hover:bg-amber-500/30";
                            } else if (i % 3 === 0 || i % 7 === 0) {
                                color = "bg-blue-500/20 border-blue-500/40 hover:bg-blue-500/30";
                            }
                            return (
                                <div
                                    key={i}
                                    className={`aspect-square rounded border transition-all cursor-pointer flex items-center justify-center text-[10px] font-mono text-slate-400 hover:scale-105 ${color}`}
                                    title={`Location Slot ${i + 1}`}
                                >
                                    {i + 1}
                                </div>
                            );
                        })}
                    </div>
                </div>
            </div>
        </div>
    );
}   