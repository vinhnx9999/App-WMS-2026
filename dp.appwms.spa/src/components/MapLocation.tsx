import { useState, useEffect, useRef, useMemo, useCallback, memo } from "react";
import { Stage, Layer, Rect, Text, Group } from "react-konva";
import apiClient from "@/api/api-client";
import { ENDPOINTS } from "@/api/endpoints";
import { useWarehouseStore } from "@/store/warehouse-store";
import { useTranslation } from "react-i18next";
import { AlertCircle, RefreshCw, ZoomIn, ZoomOut, Maximize2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import type { ApiResponse } from "@/models/response";

export interface LocationOccupancy {
    id: string;
    name: string;
    coorX: number | null;
    coorY: number | null;
    coorZ: number | null;
    type: number;
    isBlocked: boolean;
    occupancyStatus: "blocked" | "path" | "lift" | "occupied" | "empty" | string;
}

interface MapLocationProps {
    selectedLocationId?: string;
    onSelectLocation?: (location: LocationOccupancy) => void;
}

const COLOR_MAP: Record<string, string> = {
    empty: "#10b981",    // Emerald Green
    occupied: "#eab308", // Amber Yellow
    path: "#3b82f6",     // Blue
    blocked: "#ef4444",  // Red
    lift: "#6b7280",     // Gray
};

// Pre-allocated constants to avoid inline object creation per render
const SHADOW_OFFSET_SELECTED = { x: 1, y: 1 };
const SHADOW_OFFSET_NONE = { x: 0, y: 0 };
const LOD_SCALE_THRESHOLD = 0.5;

const MapLocation = memo(function MapLocation({ selectedLocationId, onSelectLocation }: MapLocationProps) {
    const { t } = useTranslation();
    const { selectedWarehouse } = useWarehouseStore();
    const containerRef = useRef<HTMLDivElement>(null);
    const stageRef = useRef<any>(null);
    const tooltipRef = useRef<HTMLDivElement>(null);

    // States
    const [locations, setLocations] = useState<LocationOccupancy[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [selectedFloor, setSelectedFloor] = useState<number | null>(null);

    // Canvas size states
    const [dimensions, setDimensions] = useState({ width: 600, height: 400 });

    // Interactive states
    const [hoveredLoc, setHoveredLoc] = useState<LocationOccupancy | null>(null);

    // LOD: only show labels when scale >= threshold (avoids text rendering overhead at low zoom)
    const [showLabels, setShowLabels] = useState(true);
    const showLabelsRef = useRef(true);

    // Fetch location occupancy
    const fetchLocations = async () => {
        if (!selectedWarehouse?.id) return;
        setIsLoading(true);
        setError(null);
        try {
            const response = await apiClient.get<ApiResponse<LocationOccupancy[]>>(
                ENDPOINTS.LOCATION.OCCUPANCY,
                { params: { warehouseId: selectedWarehouse.id } }
            );
            if (response.data.success && response.data.data) {
                const data = response.data.data;
                setLocations(data);

                // Auto-select first available floor if not selected or if previous floor doesn't exist
                const floors = Array.from(new Set(data.map(l => l.coorZ).filter((z): z is number => z !== null))).sort((a, b) => a - b);
                if (floors.length > 0) {
                    if (selectedFloor === null || !floors.includes(selectedFloor)) {
                        setSelectedFloor(floors[0]);
                    }
                } else {
                    setSelectedFloor(null);
                }
            } else {
                setError(response.data.message || "Failed to load warehouse map data");
            }
        } catch (err: any) {
            console.error("Error fetching location occupancy:", err);
            setError(err.message || "Failed to load warehouse map data");
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        fetchLocations();
    }, [selectedWarehouse?.id]);

    // Handle container resize
    useEffect(() => {
        if (!containerRef.current) return;
        const resizeObserver = new ResizeObserver((entries) => {
            for (let entry of entries) {
                const { width, height } = entry.contentRect;
                setDimensions({
                    width: width || 600,
                    height: height ? Math.max(height, 350) : 400
                });
            }
        });
        resizeObserver.observe(containerRef.current);
        return () => resizeObserver.disconnect();
    }, []);

    // Get unique floors
    const floors = useMemo(() => {
        return Array.from(new Set(locations.map(l => l.coorZ).filter((z): z is number => z !== null))).sort((a, b) => a - b);
    }, [locations]);

    // Filter locations for selected floor
    const floorLocations = useMemo(() => {
        if (selectedFloor === null) return [];
        return locations.filter(l => l.coorZ === selectedFloor && l.coorX !== null && l.coorY !== null);
    }, [locations, selectedFloor]);

    // Grid details
    const gridBounds = useMemo(() => {
        if (floorLocations.length === 0) return { minX: 0, maxX: 0, minY: 0, maxY: 0, width: 0, height: 0 };
        const xs = floorLocations.map(l => l.coorX as number);
        const ys = floorLocations.map(l => l.coorY as number);
        const minX = Math.min(...xs);
        const maxX = Math.max(...xs);
        const minY = Math.min(...ys);
        const maxY = Math.max(...ys);
        return {
            minX,
            maxX,
            minY,
            maxY,
            width: maxX - minX + 1,
            height: maxY - minY + 1
        };
    }, [floorLocations]);

    // Cell calculation constant
    const CELL_SIZE = 45;
    const GAP = 3;

    // Helper: update LOD label visibility only when crossing threshold
    const updateLabelVisibility = useCallback((newScale: number) => {
        const shouldShow = newScale >= LOD_SCALE_THRESHOLD;
        if (shouldShow !== showLabelsRef.current) {
            showLabelsRef.current = shouldShow;
            setShowLabels(shouldShow);
        }
    }, []);

    // Reset view (fit to screen)
    const handleFitToScreen = useCallback(() => {
        if (floorLocations.length === 0) return;
        const stage = stageRef.current;
        if (!stage) return;

        const mapWidth = gridBounds.width * (CELL_SIZE + GAP);
        const mapHeight = gridBounds.height * (CELL_SIZE + GAP);

        const scaleX = (dimensions.width - 40) / mapWidth;
        const scaleY = (dimensions.height - 40) / mapHeight;
        const newScale = Math.min(scaleX, scaleY, 1.5); // Cap max zoom-in at 1.5x

        const x = (dimensions.width - mapWidth * newScale) / 2;
        const y = (dimensions.height - mapHeight * newScale) / 2;

        stage.scale({ x: newScale, y: newScale });
        stage.position({ x, y });
        stage.batchDraw();
        updateLabelVisibility(newScale);
    }, [floorLocations.length, gridBounds, dimensions, updateLabelVisibility]);

    // Auto-fit on floor change or dimensions change
    useEffect(() => {
        if (floorLocations.length > 0 && dimensions.width > 0) {
            handleFitToScreen();
        }
    }, [selectedFloor, floorLocations.length, dimensions.width, dimensions.height]);

    // Zoom handlers
    const handleZoom = useCallback((factor: number) => {
        const stage = stageRef.current;
        if (!stage) return;
        const oldScale = stage.scaleX();
        const newScale = Math.min(Math.max(oldScale * factor, 0.2), 4);

        // Zoom centered on the viewport center
        const center = {
            x: dimensions.width / 2,
            y: dimensions.height / 2
        };
        const mousePointTo = {
            x: (center.x - stage.x()) / oldScale,
            y: (center.y - stage.y()) / oldScale,
        };

        stage.scale({ x: newScale, y: newScale });
        stage.position({
            x: center.x - mousePointTo.x * newScale,
            y: center.y - mousePointTo.y * newScale,
        });
        stage.batchDraw();
        updateLabelVisibility(newScale);
    }, [dimensions.width, dimensions.height, updateLabelVisibility]);

    const handleWheel = useCallback((e: any) => {
        e.evt.preventDefault();
        const stage = stageRef.current;
        if (!stage) return;

        const oldScale = stage.scaleX();
        const pointer = stage.getPointerPosition();
        if (!pointer) return;

        const mousePointTo = {
            x: (pointer.x - stage.x()) / oldScale,
            y: (pointer.y - stage.y()) / oldScale,
        };

        const zoomFactor = e.evt.deltaY < 0 ? 1.1 : 0.9;
        const newScale = Math.min(Math.max(oldScale * zoomFactor, 0.2), 4);

        stage.scale({ x: newScale, y: newScale });
        stage.position({
            x: pointer.x - mousePointTo.x * newScale,
            y: pointer.y - mousePointTo.y * newScale,
        });
        stage.batchDraw();
        updateLabelVisibility(newScale);
    }, [updateLabelVisibility]);

    const handleCellMouseEnter = useCallback((loc: LocationOccupancy, isSelectable: boolean) => {
        const stage = stageRef.current;
        if (stage) {
            stage.container().style.cursor = isSelectable ? "pointer" : "not-allowed";
            const mousePos = stage.getPointerPosition();
            if (mousePos && tooltipRef.current) {
                tooltipRef.current.style.transform = `translate(${mousePos.x + 15}px, ${mousePos.y + 15}px)`;
            }
        }
        setHoveredLoc(loc);
    }, []);

    const handleCellMouseMove = useCallback(() => {
        const stage = stageRef.current;
        if (stage) {
            const mousePos = stage.getPointerPosition();
            if (mousePos && tooltipRef.current) {
                tooltipRef.current.style.transform = `translate(${mousePos.x + 15}px, ${mousePos.y + 15}px)`;
            }
        }
    }, []);

    const handleCellMouseLeave = useCallback(() => {
        const stage = stageRef.current;
        if (stage) {
            stage.container().style.cursor = "grab";
        }
        setHoveredLoc(null);
    }, []);

    // Stable callback for LocationCell onSelect (avoids inline arrow breaking memo)
    const stableOnSelect = useCallback(
        (loc: LocationOccupancy) => { onSelectLocation?.(loc); },
        [onSelectLocation]
    );

    if (!selectedWarehouse) {
        return (
            <div className="h-full w-full flex flex-col items-center justify-center text-muted-foreground p-6 bg-card rounded-xl border border-border">
                <AlertCircle className="size-10 mb-2 text-yellow-500" />
                <p className="text-sm font-semibold">{t("translation:navigation.selectWarehousePrompt")}</p>
            </div>
        );
    }

    return (
        <div className="h-full w-full flex flex-col bg-card rounded-xl border border-border overflow-hidden relative shadow-sm">
            {/* Header controls */}
            <div className="flex flex-wrap items-center justify-between gap-3 p-4 border-b border-border bg-card/50 backdrop-blur-sm z-10">
                <div className="flex items-center gap-3">
                    <span className="text-sm font-bold text-foreground tracking-wide uppercase">
                        {t("translation:inbound.warehouseMap")}
                    </span>
                    {floors.length > 0 && (
                        <Select
                            value={selectedFloor?.toString()}
                            onValueChange={(val) => setSelectedFloor(Number(val))}
                        >
                            <SelectTrigger className="w-[120px] h-9 bg-background border-border text-xs font-semibold">
                                <SelectValue placeholder={t("translation:inbound.selectFloor")} />
                            </SelectTrigger>
                            <SelectContent>
                                {floors.map(f => (
                                    <SelectItem key={f} value={f.toString()} className="text-xs">
                                        {t("translation:inbound.floor", { floor: f })}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    )}
                </div>

                <div className="flex items-center gap-1">
                    <Button variant="outline" size="icon" className="size-8 cursor-pointer" onClick={() => handleZoom(1.2)} title={t("translation:common.zoomIn")}>
                        <ZoomIn className="size-4 text-muted-foreground" />
                    </Button>
                    <Button variant="outline" size="icon" className="size-8 cursor-pointer" onClick={() => handleZoom(0.8)} title={t("translation:common.zoomOut")}>
                        <ZoomOut className="size-4 text-muted-foreground" />
                    </Button>
                    <Button variant="outline" size="icon" className="size-8 cursor-pointer" onClick={handleFitToScreen} title={t("translation:common.fitToScreen")}>
                        <Maximize2 className="size-4 text-muted-foreground" />
                    </Button>
                    <Button variant="outline" size="icon" className="size-8 cursor-pointer" onClick={fetchLocations} disabled={isLoading} title={t("translation:common.reload", "Reload")}>
                        <RefreshCw className={`size-4 text-muted-foreground ${isLoading ? "animate-spin" : ""}`} />
                    </Button>
                </div>
            </div>

            {/* Map Canvas */}
            <div ref={containerRef} className="flex-1 w-full bg-slate-950/25 relative overflow-hidden cursor-grab active:cursor-grabbing">
                {isLoading && locations.length === 0 ? (
                    <div className="absolute inset-0 flex items-center justify-center bg-card/50 z-25">
                        <RefreshCw className="size-8 animate-spin text-primary" />
                    </div>
                ) : error ? (
                    <div className="absolute inset-0 flex flex-col items-center justify-center p-6 text-center text-muted-foreground z-25">
                        <AlertCircle className="size-8 text-destructive mb-2" />
                        <p className="text-sm font-semibold text-destructive">{error}</p>
                        <Button onClick={fetchLocations} className="mt-4 text-xs" variant="outline">
                            {t("translation:common.button.retry")}
                        </Button>
                    </div>
                ) : floorLocations.length === 0 ? (
                    <div className="absolute inset-0 flex items-center justify-center p-6 text-muted-foreground text-center">
                        <p className="text-sm">{t("translation:inbound.noLocationsFound")}</p>
                    </div>
                ) : (
                    <Stage
                        ref={stageRef}
                        width={dimensions.width}
                        height={dimensions.height}
                        draggable
                        onDragStart={(e) => {
                            const stage = e.target.getStage();
                            if (stage) {
                                stage.container().style.cursor = "grabbing";
                            }
                        }}
                        onDragEnd={(e) => {
                            const stage = e.target.getStage();
                            if (stage) {
                                stage.container().style.cursor = "grab";
                            }
                        }}
                        onWheel={handleWheel}
                    >
                        <Layer>
                            {floorLocations.map((loc) => {
                                const isSelected = loc.id === selectedLocationId;
                                const color = COLOR_MAP[loc.occupancyStatus] || "#94a3b8";

                                // Map relative grid coordinate to pixel position
                                const px = ((loc.coorX || 0) - gridBounds.minX) * (CELL_SIZE + GAP);
                                const py = ((loc.coorY || 0) - gridBounds.minY) * (CELL_SIZE + GAP);

                                const isSelectable = loc.occupancyStatus === "empty" || loc.occupancyStatus === "occupied";

                                return (
                                    <LocationCell
                                        key={loc.id}
                                        loc={loc}
                                        isSelected={isSelected}
                                        isSelectable={isSelectable}
                                        color={color}
                                        px={px}
                                        py={py}
                                        CELL_SIZE={CELL_SIZE}
                                        showLabel={showLabels}
                                        onSelect={stableOnSelect}
                                        onMouseEnter={handleCellMouseEnter}
                                        onMouseMove={handleCellMouseMove}
                                        onMouseLeave={handleCellMouseLeave}
                                    />
                                );
                            })}
                        </Layer>
                    </Stage>
                )}

                {/* HTML Tooltip on Hover */}
                <div
                    ref={tooltipRef}
                    className={`absolute pointer-events-none z-30 bg-popover/90 backdrop-blur-md text-popover-foreground px-3 py-2 rounded-lg border border-border shadow-xl text-xs flex flex-col gap-1 transition-all duration-75 ${hoveredLoc ? 'opacity-100' : 'opacity-0'}`}
                    style={{
                        left: 0,
                        top: 0,
                        transform: 'translate(-9999px, -9999px)',
                    }}
                >
                    {hoveredLoc && (
                        <>
                            <div className="font-bold border-b border-border pb-0.5 mb-1 text-primary">
                                {hoveredLoc.name}
                            </div>
                            <div className="flex justify-between gap-4">
                                <span className="text-muted-foreground">{t("translation:inbound.statusLabel")}</span>
                                <span className="font-semibold capitalize text-right flex items-center gap-1.5">
                                    <span
                                        className="size-2 rounded-full inline-block"
                                        style={{ backgroundColor: COLOR_MAP[hoveredLoc.occupancyStatus] || "#94a3b8" }}
                                    />
                                    {t(`translation:inbound.status.${hoveredLoc.occupancyStatus}`, hoveredLoc.occupancyStatus)}
                                </span>
                            </div>
                        </>
                    )}
                </div>
            </div>

            {/* Legend footer */}
            <div className="flex flex-wrap items-center justify-center gap-x-5 gap-y-2 p-3 bg-muted/50 border-t border-border text-[11px] font-medium text-muted-foreground">
                <div className="flex items-center gap-1.5">
                    <span className="size-3 rounded bg-[#10b981] border border-black/10"></span>
                    <span>{t("translation:inbound.status.empty")}</span>
                </div>
                <div className="flex items-center gap-1.5">
                    <span className="size-3 rounded bg-[#eab308] border border-black/10"></span>
                    <span>{t("translation:inbound.status.occupied")}</span>
                </div>
                <div className="flex items-center gap-1.5">
                    <span className="size-3 rounded bg-[#3b82f6] border border-black/10"></span>
                    <span>{t("translation:inbound.status.path")}</span>
                </div>
                <div className="flex items-center gap-1.5">
                    <span className="size-3 rounded bg-[#ef4444] border border-black/10"></span>
                    <span>{t("translation:inbound.status.blocked")}</span>
                </div>
                <div className="flex items-center gap-1.5">
                    <span className="size-3 rounded bg-[#6b7280] border border-black/10"></span>
                    <span>{t("translation:inbound.status.lift")}</span>
                </div>
            </div>
        </div>
    );
});

export default MapLocation;

interface LocationCellProps {
    loc: LocationOccupancy;
    isSelected: boolean;
    isSelectable: boolean;
    color: string;
    px: number;
    py: number;
    CELL_SIZE: number;
    showLabel: boolean;
    onSelect: (loc: LocationOccupancy) => void;
    onMouseEnter: (loc: LocationOccupancy, isSelectable: boolean) => void;
    onMouseMove: () => void;
    onMouseLeave: () => void;
}

const LocationCell = memo(function LocationCell({
    loc,
    isSelected,
    isSelectable,
    color,
    px,
    py,
    CELL_SIZE,
    showLabel,
    onSelect,
    onMouseEnter,
    onMouseMove,
    onMouseLeave,
}: LocationCellProps) {
    return (
        <Group
            x={px}
            y={py}
            onClick={() => {
                if (isSelectable) onSelect(loc);
            }}
            onTap={() => {
                if (isSelectable) onSelect(loc);
            }}
            onMouseEnter={() => onMouseEnter(loc, isSelectable)}
            onMouseMove={onMouseMove}
            onMouseLeave={onMouseLeave}
        >
            {/* Main location square — shadow only on selected cell for GPU perf */}
            <Rect
                width={CELL_SIZE}
                height={CELL_SIZE}
                fill={color}
                cornerRadius={4}
                stroke={isSelected ? "#ffffff" : "#1e293b"}
                strokeWidth={isSelected ? 3 : 1}
                shadowColor={isSelected ? "#000" : undefined}
                shadowBlur={isSelected ? 8 : 0}
                shadowOpacity={isSelected ? 0.4 : 0}
                shadowOffset={isSelected ? SHADOW_OFFSET_SELECTED : SHADOW_OFFSET_NONE}
            />

            {/* Overlay highlight if selected */}
            {isSelected && (
                <Rect
                    width={CELL_SIZE}
                    height={CELL_SIZE}
                    stroke="#3b82f6"
                    strokeWidth={1.5}
                    cornerRadius={4}
                />
            )}

            {/* LOD: hide text labels when zoomed out past threshold */}
            {showLabel && (
                <Text
                    text={loc.name}
                    x={2}
                    y={CELL_SIZE / 2 - 5}
                    width={CELL_SIZE - 4}
                    align="center"
                    fontSize={9}
                    fontStyle="bold"
                    fill={loc.occupancyStatus === "occupied" || loc.occupancyStatus === "path" || loc.occupancyStatus === "lift" ? "#ffffff" : "#0f172a"}
                    listening={false}
                />
            )}
        </Group>
    );
});