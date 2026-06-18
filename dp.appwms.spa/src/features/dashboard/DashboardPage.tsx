import { useTranslation } from "react-i18next";
import { Layers, TrendingUp, MapPin, Play, CheckCircle2, ShieldAlert } from "lucide-react";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";

export default function DashboardPage() {
    const { t } = useTranslation();

    return (
        <div className="h-full w-full flex flex-col overflow-hidden">
            {/* Header bar matching the layout pattern of other pages */}
            <div className="flex items-center justify-between gap-3 shrink-0 bg-card text-card-foreground p-3 border-b border-border">
                <h1 className="text-sm font-semibold tracking-tight uppercase text-muted-foreground">
                    {t("translation:navigation.dashboard", "Tổng quan vận hành")}
                </h1>
            </div>

            {/* Scrollable content area */}
            <div className="flex-1 w-full overflow-y-auto scrollbar-none p-6 space-y-6 bg-background">
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                    {/* KPI 1: Storage Utilization */}
                    <Card>
                        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                            <CardTitle className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Storage Utilization</CardTitle>
                            <Layers className="size-5 text-primary" />
                        </CardHeader>
                        <CardContent>
                            <div className="flex items-baseline gap-2">
                                <span className="text-3xl font-bold">74.8%</span>
                                <span className="text-xs text-primary flex items-center font-medium">
                                    <TrendingUp className="size-3 mr-0.5" /> +2.1%
                                </span>
                            </div>
                            <div className="mt-4 w-full bg-muted h-2 rounded-full overflow-hidden">
                                <div className="bg-primary h-full rounded-full" style={{ width: "74.8%" }}></div>
                            </div>
                        </CardContent>
                    </Card>

                    {/* KPI 2: Active Pallet */}
                    <Card>
                        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                            <CardTitle className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Active Pallet</CardTitle>
                            <MapPin className="size-5 text-chart-2" />
                        </CardHeader>
                        <CardContent>
                            <div className="flex items-baseline gap-2">
                                <span className="text-3xl font-bold">14 / 16</span>
                                <span className="text-xs text-muted-foreground">Available</span>
                            </div>
                            <div className="mt-4 flex items-center gap-1.5 text-xs text-chart-2 font-medium">
                                <span className="size-2 bg-chart-2 rounded-full animate-pulse"></span>
                                Trực quan vị trí bình thường
                            </div>
                        </CardContent>
                    </Card>

                    {/* KPI 3: Pending Putaway Rules */}
                    <Card>
                        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                            <CardTitle className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Pending Putaway Rules</CardTitle>
                            <Play className="size-5 text-chart-3" />
                        </CardHeader>
                        <CardContent>
                            <div className="flex items-baseline gap-2">
                                <span className="text-3xl font-bold">5</span>
                                <span className="text-xs text-chart-3 font-medium">Queued</span>
                            </div>
                            <div className="mt-4 w-full bg-muted h-2 rounded-full overflow-hidden">
                                <div className="bg-chart-3 h-full rounded-full" style={{ width: "35%" }}></div>
                            </div>
                        </CardContent>
                    </Card>

                    {/* KPI 4: Rule Execution Success */}
                    <Card>
                        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                            <CardTitle className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Rule Execution Success</CardTitle>
                            <CheckCircle2 className="size-5 text-chart-1" />
                        </CardHeader>
                        <CardContent>
                            <div className="flex items-baseline gap-2">
                                <span className="text-3xl font-bold">99.8%</span>
                                <span className="text-xs text-chart-1 flex items-center font-medium">
                                    <TrendingUp className="size-3 mr-0.5" /> +0.4%
                                </span>
                            </div>
                            <div className="mt-4 flex items-end gap-1 h-3">
                                {[30, 45, 60, 50, 75, 90, 100].map((h, i) => (
                                    <div key={i} className="flex-1 bg-muted rounded-sm h-full relative overflow-hidden">
                                        <div className="bg-chart-1 absolute bottom-0 left-0 right-0 rounded-sm" style={{ height: `${h}%` }}></div>
                                    </div>
                                ))}
                            </div>
                        </CardContent>
                    </Card>
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                    <div className="lg:col-span-2 space-y-6">
                        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                            {/* Inbound volume */}
                            <Card>
                                <CardHeader>
                                    <CardTitle className="text-sm font-semibold text-muted-foreground uppercase">Inbound Volume (SKU/h)</CardTitle>
                                </CardHeader>
                                <CardContent>
                                    <div className="h-40 flex items-end gap-2">
                                        {[40, 60, 50, 80, 55, 95].map((val, idx) => (
                                            <div key={idx} className="flex-1 flex flex-col items-center gap-1.5 h-full justify-end">
                                                <div className="w-full bg-chart-1/20 hover:bg-chart-1/35 rounded-t-md transition-all relative group" style={{ height: `${val}%` }}>
                                                    <span className="absolute -top-6 left-1/2 -translate-x-1/2 bg-popover text-popover-foreground text-[10px] px-1.5 py-0.5 rounded opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap border border-border shadow-md">{val} SKU</span>
                                                </div>
                                                <span className="text-[10px] text-muted-foreground">{8 + idx * 2}am</span>
                                            </div>
                                        ))}
                                    </div>
                                </CardContent>
                            </Card>

                            {/* Outbound volume */}
                            <Card>
                                <CardHeader>
                                    <CardTitle className="text-sm font-semibold text-muted-foreground uppercase">Outbound Volume (Orders)</CardTitle>
                                </CardHeader>
                                <CardContent>
                                    <div className="h-40 relative">
                                        {/* SVG Line Chart */}
                                        <svg className="w-full h-full" viewBox="0 0 100 40" preserveAspectRatio="none">
                                            <path d="M 0 35 Q 20 25 40 28 T 80 10 T 100 5" fill="none" stroke="var(--chart-2)" strokeWidth="2" />
                                            <path d="M 0 35 Q 20 25 40 28 T 80 10 T 100 5 L 100 40 L 0 40 Z" fill="url(#grad)" opacity="0.1" />
                                            <defs>
                                                <linearGradient id="grad" x1="0%" y1="0%" x2="0%" y2="100%">
                                                    <stop offset="0%" stopColor="var(--chart-2)" />
                                                    <stop offset="100%" stopColor="var(--chart-2)" stopOpacity="0" />
                                                </linearGradient>
                                            </defs>
                                        </svg>
                                        <div className="absolute bottom-0 left-0 right-0 flex justify-between px-1 text-[10px] text-muted-foreground mt-2">
                                            <span>8am</span>
                                            <span>12pm</span>
                                            <span>4pm</span>
                                            <span>8pm</span>
                                        </div>
                                    </div>
                                </CardContent>
                            </Card>
                        </div>

                        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                            {/* Dock Utilization Rates */}
                            <Card>
                                <CardHeader>
                                    <CardTitle className="text-sm font-semibold text-muted-foreground uppercase">Dock Utilization Rates</CardTitle>
                                </CardHeader>
                                <CardContent className="space-y-4">
                                    {[
                                        { name: "Dock A (Receiving)", value: 85, color: "bg-chart-1" },
                                        { name: "Dock B (Shipping)", value: 62, color: "bg-chart-2" },
                                        { name: "Dock C (Cross-dock)", value: 24, color: "bg-chart-3" },
                                    ].map((dock, idx) => (
                                        <div key={idx} className="space-y-1.5">
                                            <div className="flex justify-between text-xs font-medium">
                                                <span>{dock.name}</span>
                                                <span>{dock.value}%</span>
                                            </div>
                                            <div className="w-full bg-muted h-2 rounded-full overflow-hidden">
                                                <div className={`${dock.color} h-full rounded-full`} style={{ width: `${dock.value}%` }}></div>
                                            </div>
                                        </div>
                                    ))}
                                </CardContent>
                            </Card>

                            {/* Storage Zone Capacity Distribution */}
                            <Card>
                                <CardHeader>
                                    <CardTitle className="text-sm font-semibold text-muted-foreground uppercase">Storage Zone Capacity</CardTitle>
                                </CardHeader>
                                <CardContent className="space-y-4">
                                    {[
                                        { zone: "Zone A (Shuttle Rack)", value: 82, color: "bg-chart-4" },
                                        { zone: "Zone B (Mezzanine)", value: 49, color: "bg-chart-5" },
                                        { zone: "Zone C (Bulk Area)", value: 91, color: "bg-chart-1" },
                                    ].map((zone, idx) => (
                                        <div key={idx} className="space-y-1.5">
                                            <div className="flex justify-between text-xs font-medium">
                                                <span>{zone.zone}</span>
                                                <span>{zone.value}%</span>
                                            </div>
                                            <div className="w-full bg-muted h-2 rounded-full overflow-hidden">
                                                <div className={`${zone.color} h-full rounded-full`} style={{ width: `${zone.value}%` }}></div>
                                            </div>
                                        </div>
                                    ))}
                                </CardContent>
                            </Card>
                        </div>
                    </div>

                    {/* Rule Conflicts & Alerts */}
                    <Card className="flex flex-col h-full min-h-[400px]">
                        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                            <div className="flex items-center gap-2">
                                <ShieldAlert className="size-5 text-destructive" />
                                <CardTitle className="font-semibold text-foreground">Rule Conflicts & Alerts</CardTitle>
                            </div>
                            <span className="text-[10px] font-bold px-2 py-0.5 bg-destructive/10 text-destructive rounded-full">
                                4 Cần Xử Lý
                            </span>
                        </CardHeader>
                        <CardContent className="space-y-3 flex-1 overflow-y-auto pr-1">
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
                                    className={`p-3.5 rounded-lg border text-xs transition-all hover:bg-accent hover:text-accent-foreground ${alert.type === "CRITICAL"
                                        ? "bg-destructive/5 border-destructive/20"
                                        : alert.type === "WARNING"
                                            ? "bg-chart-3/5 border-chart-3/20"
                                            : "bg-muted/30 border-border opacity-70"
                                        }`}
                                >
                                    <div className="flex items-center justify-between mb-1">
                                        <span className="text-[10px] text-muted-foreground font-mono">{alert.time}</span>
                                        <span className={`text-[9px] font-bold px-1.5 py-0.2 rounded-sm ${alert.type === "CRITICAL"
                                            ? "bg-destructive/10 text-destructive"
                                            : alert.type === "WARNING"
                                                ? "bg-chart-3/10 text-chart-3"
                                                : "bg-muted text-muted-foreground"
                                            }`}>{alert.type}</span>
                                    </div>
                                    <h4 className="font-semibold text-foreground mb-1">{alert.title}</h4>
                                    <p className="text-muted-foreground leading-relaxed">{alert.desc}</p>
                                    {alert.action && (
                                        <button className="mt-2.5 px-2.5 py-1 bg-background border border-border rounded text-[10px] font-semibold hover:bg-accent hover:text-accent-foreground text-foreground cursor-pointer">
                                            {alert.action}
                                        </button>
                                    )}
                                </div>
                            ))}
                        </CardContent>
                    </Card>
                </div>

                <Card>
                    <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                        <div className="flex items-center gap-2">
                            <Layers className="size-5 text-chart-1" />
                            <CardTitle className="font-semibold">Warehouse Layout Map</CardTitle>
                        </div>
                        <div className="flex items-center gap-3 text-xs">
                            <div className="flex items-center gap-1.5">
                                <span className="size-2.5 bg-muted-foreground/40 rounded-xs"></span>
                                <span>Sẵn sàng</span>
                            </div>
                            <div className="flex items-center gap-1.5">
                                <span className="size-2.5 bg-chart-2 rounded-xs"></span>
                                <span>Đang chiếm giữ</span>
                            </div>
                            <div className="flex items-center gap-1.5">
                                <span className="size-2.5 bg-destructive rounded-xs"></span>
                                <span>Xung đột/Lỗi</span>
                            </div>
                            <div className="flex items-center gap-1.5">
                                <span className="size-2.5 bg-chart-3 rounded-xs"></span>
                                <span>Cảnh báo</span>
                            </div>
                        </div>
                    </CardHeader>
                    <CardContent>
                        <div className="w-full bg-muted/20 border border-border rounded-lg p-6 flex flex-col items-center justify-center">
                            <div className="w-full max-w-4xl grid grid-cols-12 gap-1.5">
                                {Array.from({ length: 48 }).map((_, i) => {
                                    let color = "bg-muted/40 border-muted-foreground/20 hover:bg-muted/60 text-muted-foreground";
                                    if (i === 12 || i === 15 || i === 33) {
                                        color = "bg-destructive/20 border-destructive/40 hover:bg-destructive/30 text-destructive";
                                    } else if (i === 8 || i === 19 || i === 41 || i === 42) {
                                        color = "bg-chart-3/20 border-chart-3/40 hover:bg-chart-3/30 text-chart-3";
                                    } else if (i % 3 === 0 || i % 7 === 0) {
                                        color = "bg-chart-2/20 border-chart-2/40 hover:bg-chart-2/30 text-chart-2";
                                    }
                                    return (
                                        <div
                                            key={i}
                                            className={`aspect-square rounded border transition-all cursor-pointer flex items-center justify-center text-[10px] font-mono hover:scale-105 ${color}`}
                                            title={`Location Slot ${i + 1}`}
                                        >
                                            {i + 1}
                                        </div>
                                    );
                                })}
                            </div>
                        </div>
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}