import { useState, useEffect, useRef, useMemo, useCallback } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { debounce } from "lodash";
import {
    Plus, Trash2, CheckCircle2,
    Layers, ClipboardList, Info, RefreshCw
} from "lucide-react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ScrollArea } from "@/components/ui/scroll-area";
import MapLocation, { type LocationOccupancy } from "@/components/MapLocation";
import { useWarehouseStore } from "@/store/warehouse-store";
import { skuService } from "../master-data/skus/services/sku.service";
import { supplierService } from "../master-data/suppliers/services/supplier.service";
import type { SkuDto } from "../master-data/skus/models/sku-dto.model";
import type { SupplierDto } from "../master-data/suppliers/models/supplier.model";
import apiClient from "@/api/api-client";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/models/response";

interface DraftItem {
    id: string; // client-side temporary id
    sku: SkuDto;
    supplier: SupplierDto | null;
    expiryDate: string; // YYYY-MM-DD
    lotNumber: string;
    quantity: number;
    palletCode: string;
    location: LocationOccupancy;
}

export default function InboundDirectPage() {
    const { t } = useTranslation();
    const { selectedWarehouse } = useWarehouseStore();

    // Draft list state
    const [draftItems, setDraftItems] = useState<DraftItem[]>([]);
    const [isSubmitting, setIsSubmitting] = useState(false);

    // Form states
    const [skuSearch, setSkuSearch] = useState("");
    const [selectedSku, setSelectedSku] = useState<SkuDto | null>(null);
    const [skuResults, setSkuResults] = useState<SkuDto[]>([]);
    const [isSkuLoading, setIsSkuLoading] = useState(false);
    const [isSkuOpen, setIsSkuOpen] = useState(false);

    const [supplierSearch, setSupplierSearch] = useState("");
    const [selectedSupplier, setSelectedSupplier] = useState<SupplierDto | null>(null);
    const [supplierResults, setSupplierResults] = useState<SupplierDto[]>([]);
    const [isSupplierLoading, setIsSupplierLoading] = useState(false);
    const [isSupplierOpen, setIsSupplierOpen] = useState(false);

    const [expiryDate, setExpiryDate] = useState("");
    const [lotNumber, setLotNumber] = useState("");
    const [quantity, setQuantity] = useState("");
    const [palletCode, setPalletCode] = useState("");
    const [selectedLocation, setSelectedLocation] = useState<LocationOccupancy | null>(null);

    // Keyboard navigation refs
    const skuRef = useRef<HTMLInputElement>(null);
    const supplierRef = useRef<HTMLInputElement>(null);
    const expiryRef = useRef<HTMLInputElement>(null);
    const lotRef = useRef<HTMLInputElement>(null);
    const qtyRef = useRef<HTMLInputElement>(null);
    const palletRef = useRef<HTMLInputElement>(null);
    const submitBtnRef = useRef<HTMLButtonElement>(null);

    // Autocomplete dropdown index for keyboard navigation
    const [skuIndex, setSkuIndex] = useState(-1);
    const [supplierIndex, setSupplierIndex] = useState(-1);

    // Search SKU (Debounced)
    const searchSkus = useMemo(
        () => debounce(async (query: string) => {
            if (!query) {
                setSkuResults([]);
                return;
            }
            setIsSkuLoading(true);
            try {
                const res = await skuService.searchSkus({ search: query, page: 1, limit: 10 });
                if (res.success && res.data) {
                    setSkuResults(res.data.items);
                }
            } catch (err) {
                console.error("Error searching SKUs:", err);
            } finally {
                setIsSkuLoading(false);
            }
        }, 300),
        []
    );

    // Search Supplier (Debounced)
    const searchSuppliers = useMemo(
        () => debounce(async (query: string) => {
            if (!query) {
                setSupplierResults([]);
                return;
            }
            setIsSupplierLoading(true);
            try {
                const res = await supplierService.searchSuppliers({ search: query, page: 1, limit: 10 });
                if (res.success && res.data) {
                    setSupplierResults(res.data.items);
                }
            } catch (err) {
                console.error("Error searching suppliers:", err);
            } finally {
                setIsSupplierLoading(false);
            }
        }, 300),
        []
    );

    useEffect(() => {
        return () => {
            searchSkus.cancel();
            searchSuppliers.cancel();
        };
    }, [searchSkus, searchSuppliers]);

    // Handle global Ctrl+Enter shortcut for submission
    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.ctrlKey && e.key === "Enter") {
                e.preventDefault();
                handleConfirmPutaway();
            }
        };
        window.addEventListener("keydown", handleKeyDown);
        return () => window.removeEventListener("keydown", handleKeyDown);
    }, [draftItems, isSubmitting, selectedWarehouse]);

    // Click outside to close dropdowns
    const skuContainerRef = useRef<HTMLDivElement>(null);
    const supplierContainerRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const handleClickOutside = (e: MouseEvent) => {
            if (skuContainerRef.current && !skuContainerRef.current.contains(e.target as Node)) {
                setIsSkuOpen(false);
            }
            if (supplierContainerRef.current && !supplierContainerRef.current.contains(e.target as Node)) {
                setIsSupplierOpen(false);
            }
        };
        document.addEventListener("mousedown", handleClickOutside);
        return () => document.removeEventListener("mousedown", handleClickOutside);
    }, []);

    // Add item to draft table
    const handleAddToDraft = () => {
        if (!selectedSku) {
            toast.error("Vui lòng chọn SKU hợp lệ.");
            skuRef.current?.focus();
            return;
        }
        if (!quantity || Number(quantity) <= 0) {
            toast.error("Số lượng phải lớn hơn 0.");
            qtyRef.current?.focus();
            return;
        }
        if (!selectedLocation) {
            toast.error("Vui lòng chọn vị trí cất hàng (từ bản đồ hoặc gõ chọn).");
            return;
        }

        // Add to draft list
        const newItem: DraftItem = {
            id: Math.random().toString(36).substr(2, 9),
            sku: selectedSku,
            supplier: selectedSupplier,
            expiryDate: expiryDate || "",
            lotNumber: lotNumber || "",
            quantity: Number(quantity),
            palletCode: palletCode.trim(),
            location: selectedLocation
        };

        setDraftItems(prev => [...prev, newItem]);
        toast.success(`Đã thêm ${selectedSku.skuCode} vào danh sách tạm.`);

        // Reset form except supplier/expiry/lot to facilitate rapid entry of same batch
        setSelectedSku(null);
        setSkuSearch("");
        setQuantity("");
        setPalletCode("");
        // Keep location or clear? Usually clear to force selecting new location
        setSelectedLocation(null);

        // Focus back to SKU input for next item
        setTimeout(() => {
            skuRef.current?.focus();
        }, 50);
    };

    // Remove item from draft
    const handleRemoveItem = (id: string) => {
        setDraftItems(prev => prev.filter(item => item.id !== id));
    };

    // Clear all draft items
    const handleClearDraft = () => {
        if (draftItems.length === 0) return;
        if (window.confirm("Bạn có chắc chắn muốn xóa toàn bộ danh sách tạm?")) {
            setDraftItems([]);
        }
    };

    // Submit draft to backend
    const handleConfirmPutaway = async () => {
        if (draftItems.length === 0) {
            toast.error("Danh sách tạm đang trống.");
            return;
        }
        if (!selectedWarehouse?.id) {
            toast.error("Vui lòng chọn kho hàng.");
            return;
        }

        setIsSubmitting(true);
        try {
            const itemsPayload = draftItems.map(item => ({
                skuId: item.sku.id,
                quantity: item.quantity,
                targetLocationId: item.location.id,
                palletCode: item.palletCode || null,
                supplierId: item.supplier?.id || null,
                expiryDate: item.expiryDate ? new Date(item.expiryDate).toISOString() : null,
                lotNumber: item.lotNumber || null,
                serialNumber: null // Direct putaway in this flow doesn't capture serials by default unless added
            }));

            const payload = {
                tenantId: "00000000-0000-0000-0000-000000000000", // Will be overridden by backend
                warehouseId: selectedWarehouse.id,
                items: itemsPayload
            };

            const response = await apiClient.post<ApiResponse<string>>(
                ENDPOINTS.INBOUND.DIRECT_PUTAWAY,
                payload
            );

            if (response.data.success) {
                toast.success("Đã hoàn tất cất hàng trực tiếp thành công!");
                setDraftItems([]);
            } else {
                toast.error(response.data.message || "Lỗi khi thực hiện cất hàng.");
            }
        } catch (err: any) {
            console.error("Error submitting direct putaway:", err);
            const errMsg = err.response?.data?.message || err.message || "Lỗi kết nối máy chủ.";
            toast.error(`Yêu cầu thất bại: ${errMsg}`);
        } finally {
            setIsSubmitting(false);
        }
    };

    // Keyboard navigation within the SKU input field
    const handleSkuKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === "ArrowDown") {
            e.preventDefault();
            setIsSkuOpen(true);
            setSkuIndex(prev => Math.min(prev + 1, skuResults.length - 1));
        } else if (e.key === "ArrowUp") {
            e.preventDefault();
            setSkuIndex(prev => Math.max(prev - 1, -1));
        } else if (e.key === "Enter") {
            e.preventDefault();
            if (isSkuOpen && skuIndex >= 0 && skuIndex < skuResults.length) {
                const selected = skuResults[skuIndex];
                setSelectedSku(selected);
                setSkuSearch(`${selected.name} (${selected.skuCode})`);
                setIsSkuOpen(false);
                setSkuIndex(-1);
                supplierRef.current?.focus();
            } else if (selectedSku) {
                supplierRef.current?.focus();
            }
        } else if (e.key === "Escape") {
            setIsSkuOpen(false);
            setSkuIndex(-1);
        }
    };

    // Keyboard navigation within the Supplier input field
    const handleSupplierKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === "ArrowDown") {
            e.preventDefault();
            setIsSupplierOpen(true);
            setSupplierIndex(prev => Math.min(prev + 1, supplierResults.length - 1));
        } else if (e.key === "ArrowUp") {
            e.preventDefault();
            setSupplierIndex(prev => Math.max(prev - 1, -1));
        } else if (e.key === "Enter") {
            e.preventDefault();
            if (isSupplierOpen && supplierIndex >= 0 && supplierIndex < supplierResults.length) {
                const selected = supplierResults[supplierIndex];
                setSelectedSupplier(selected);
                setSupplierSearch(selected.name);
                setIsSupplierOpen(false);
                setSupplierIndex(-1);
                expiryRef.current?.focus();
            } else {
                expiryRef.current?.focus();
            }
        } else if (e.key === "Escape") {
            setIsSupplierOpen(false);
            setSupplierIndex(-1);
        }
    };

    return (
        <div className="h-full w-full flex flex-col lg:flex-row overflow-hidden bg-background p-4 gap-4">
            {/* Left Panel: Form & Draft Table */}
            <div className="w-full lg:w-[42%] flex flex-col gap-4 overflow-hidden h-full">
                {/* Rapid Input Card */}
                <div className="bg-card text-card-foreground rounded-xl border border-border p-4 shadow-sm shrink-0">
                    <div className="flex items-center gap-2 mb-3 border-b border-border pb-2">
                        <Layers className="size-5 text-primary" />
                        <h2 className="text-sm font-bold uppercase tracking-wider text-foreground">
                            Putaway
                        </h2>
                    </div>

                    <div className="grid grid-cols-2 gap-3">
                        {/* SKU Selection */}
                        <div className="col-span-2 relative" ref={skuContainerRef}>
                            <label className="text-xs font-semibold text-muted-foreground block mb-1">
                                Sản phẩm (SKU) <span className="text-red-500">*</span>
                            </label>
                            <Input
                                ref={skuRef}
                                value={skuSearch}
                                onChange={(e) => {
                                    setSkuSearch(e.target.value);
                                    if (selectedSku) setSelectedSku(null);
                                    setIsSkuOpen(true);
                                    searchSkus(e.target.value);
                                }}
                                onFocus={() => setIsSkuOpen(true)}
                                onKeyDown={handleSkuKeyDown}
                                placeholder=""
                                className="h-9 text-xs bg-background border-border"
                            />
                            {isSkuOpen && (skuResults.length > 0 || isSkuLoading) && (
                                <div className="absolute top-full left-0 w-full z-50 mt-1 border border-border bg-popover text-popover-foreground rounded-md shadow-lg overflow-hidden">
                                    <ScrollArea className="max-h-40">
                                        <div className="p-1">
                                            {isSkuLoading ? (
                                                <div className="p-2 text-xs text-muted-foreground text-center">Đang tìm...</div>
                                            ) : (
                                                skuResults.map((sku, index) => (
                                                    <button
                                                        key={sku.id}
                                                        type="button"
                                                        onClick={() => {
                                                            setSelectedSku(sku);
                                                            setSkuSearch(`${sku.name} (${sku.skuCode})`);
                                                            setIsSkuOpen(false);
                                                            setSkuIndex(-1);
                                                            supplierRef.current?.focus();
                                                        }}
                                                        className={`w-full text-left px-3 py-2 text-xs rounded-sm hover:bg-accent hover:text-accent-foreground text-foreground transition-colors flex justify-between items-center ${index === skuIndex ? "bg-accent text-accent-foreground" : ""}`}
                                                    >
                                                        <span className="font-semibold">{sku.skuCode}</span>
                                                        <span className="text-[10px] text-muted-foreground truncate max-w-[200px]">{sku.name}</span>
                                                    </button>
                                                ))
                                            )}
                                        </div>
                                    </ScrollArea>
                                </div>
                            )}
                        </div>

                        {/* Supplier Selection */}
                        <div className="col-span-2 relative" ref={supplierContainerRef}>
                            <label className="text-xs font-semibold text-muted-foreground block mb-1">
                                Nhà cung cấp
                            </label>
                            <Input
                                ref={supplierRef}
                                value={supplierSearch}
                                onChange={(e) => {
                                    setSupplierSearch(e.target.value);
                                    if (selectedSupplier) setSelectedSupplier(null);
                                    setIsSupplierOpen(true);
                                    searchSuppliers(e.target.value);
                                }}
                                onFocus={() => setIsSupplierOpen(true)}
                                onKeyDown={handleSupplierKeyDown}
                                placeholder=""
                                className="h-9 text-xs bg-background border-border"
                            />
                            {isSupplierOpen && (supplierResults.length > 0 || isSupplierLoading) && (
                                <div className="absolute top-full left-0 w-full z-50 mt-1 border border-border bg-popover text-popover-foreground rounded-md shadow-lg overflow-hidden">
                                    <ScrollArea className="max-h-40">
                                        <div className="p-1">
                                            {isSupplierLoading ? (
                                                <div className="p-2 text-xs text-muted-foreground text-center">Đang tìm</div>
                                            ) : (
                                                supplierResults.map((sup, index) => (
                                                    <button
                                                        key={sup.id}
                                                        type="button"
                                                        onClick={() => {
                                                            setSelectedSupplier(sup);
                                                            setSupplierSearch(sup.name);
                                                            setIsSupplierOpen(false);
                                                            setSupplierIndex(-1);
                                                            expiryRef.current?.focus();
                                                        }}
                                                        className={`w-full text-left px-3 py-2 text-xs rounded-sm hover:bg-accent hover:text-accent-foreground text-foreground transition-colors ${index === supplierIndex ? "bg-accent text-accent-foreground" : ""}`}
                                                    >
                                                        {sup.name}
                                                    </button>
                                                ))
                                            )}
                                        </div>
                                    </ScrollArea>
                                </div>
                            )}
                        </div>

                        {/* Expiry Date */}
                        <div>
                            <label className="text-xs font-semibold text-muted-foreground block mb-1">
                                Hạn sử dụng (EXP)
                            </label>
                            <Input
                                ref={expiryRef}
                                type="date"
                                value={expiryDate}
                                onChange={(e) => setExpiryDate(e.target.value)}
                                onKeyDown={(e) => {
                                    if (e.key === "Enter") {
                                        e.preventDefault();
                                        lotRef.current?.focus();
                                    }
                                }}
                                className="h-9 text-xs bg-background border-border"
                            />
                        </div>

                        {/* Lot Number */}
                        <div>
                            <label className="text-xs font-semibold text-muted-foreground block mb-1">
                                Lot Numbers
                            </label>
                            <Input
                                ref={lotRef}
                                value={lotNumber}
                                onChange={(e) => setLotNumber(e.target.value)}
                                onKeyDown={(e) => {
                                    if (e.key === "Enter") {
                                        e.preventDefault();
                                        qtyRef.current?.focus();
                                    }
                                }}
                                placeholder="Gõ số lô..."
                                className="h-9 text-xs bg-background border-border"
                            />
                        </div>

                        {/* Quantity */}
                        <div>
                            <label className="text-xs font-semibold text-muted-foreground block mb-1">
                                Số lượng <span className="text-red-500">*</span>
                            </label>
                            <Input
                                ref={qtyRef}
                                type="number"
                                min="1"
                                value={quantity}
                                onChange={(e) => setQuantity(e.target.value)}
                                onKeyDown={(e) => {
                                    if (e.key === "Enter") {
                                        e.preventDefault();
                                        palletRef.current?.focus();
                                    }
                                }}
                                placeholder="Số lượng..."
                                className="h-9 text-xs bg-background border-border"
                            />
                        </div>

                        {/* Pallet Code */}
                        <div>
                            <label className="text-xs font-semibold text-muted-foreground block mb-1">
                                Mã Pallet (Tùy chọn)
                            </label>
                            <Input
                                ref={palletRef}
                                value={palletCode}
                                onChange={(e) => setPalletCode(e.target.value)}
                                onKeyDown={(e) => {
                                    if (e.key === "Enter") {
                                        e.preventDefault();
                                        // Trigger add to draft if everything is valid
                                        handleAddToDraft();
                                    }
                                }}
                                placeholder=""
                                className="h-9 text-xs bg-background border-border"
                            />
                        </div>

                        {/* Selected Location Display */}
                        <div className="col-span-2 p-2 bg-muted/50 rounded-lg border border-border flex items-center justify-between text-xs">
                            <div className="flex items-center gap-2">
                                <span className="text-muted-foreground">Vị trí đã chọn:</span>
                                {selectedLocation ? (
                                    <span className="font-bold text-primary bg-primary/10 px-2.5 py-0.5 rounded-full">
                                        {selectedLocation.name}
                                    </span>
                                ) : (
                                    <span className="text-muted-foreground italic flex items-center gap-1">
                                        <Info className="size-3.5" /> Click chọn ô trên bản đồ
                                    </span>
                                )}
                            </div>
                            <Button
                                ref={submitBtnRef}
                                type="button"
                                onClick={handleAddToDraft}
                                size="sm"
                                className="h-7 text-[10px] bg-primary text-primary-foreground hover:bg-primary/95 flex items-center gap-1 cursor-pointer font-bold"
                            >
                                <Plus className="size-3" /> Thêm hàng chờ
                            </Button>
                        </div>
                    </div>
                </div>

                {/* Draft Table Card */}
                <div className="bg-card text-card-foreground rounded-xl border border-border p-4 shadow-sm flex-1 flex flex-col overflow-hidden">
                    <div className="flex items-center justify-between border-b border-border pb-2 mb-2">
                        <div className="flex items-center gap-2">
                            <ClipboardList className="size-5 text-primary" />
                            <h2 className="text-sm font-bold uppercase tracking-wider text-foreground">
                                Danh sách tạm ({draftItems.length})
                            </h2>
                        </div>
                        {draftItems.length > 0 && (
                            <Button
                                variant="ghost"
                                size="sm"
                                onClick={handleClearDraft}
                                className="h-7 text-xs text-destructive hover:bg-destructive/10 cursor-pointer"
                            >
                                <Trash2 className="size-3.5 mr-1" /> Xóa tất cả
                            </Button>
                        )}
                    </div>

                    {/* Table */}
                    <div className="flex-1 overflow-auto min-h-[150px]">
                        {draftItems.length === 0 ? (
                            <div className="h-full w-full flex flex-col items-center justify-center text-muted-foreground gap-1.5 p-4 text-center">
                                <ClipboardList className="size-8 text-muted-foreground/50" />
                                <p className="text-xs">Danh sách tạm rỗng. Nhập biểu mẫu bên trên để thêm sản phẩm.</p>
                            </div>
                        ) : (
                            <table className="w-full text-left text-xs border-collapse">
                                <thead className="bg-muted text-muted-foreground sticky top-0 font-bold z-10">
                                    <tr>
                                        <th className="p-2 border-b border-border">Mã SKU</th>
                                        <th className="p-2 border-b border-border">Số lượng</th>
                                        <th className="p-2 border-b border-border">Vị trí</th>
                                        <th className="p-2 border-b border-border">Lô / Pallet</th>
                                        <th className="p-2 border-b border-border text-center">Xóa</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {draftItems.map((item) => (
                                        <tr key={item.id} className="hover:bg-muted/30 transition-colors border-b border-border">
                                            <td className="p-2 font-medium">
                                                <div>{item.sku.skuCode}</div>
                                                <div className="text-[10px] text-muted-foreground truncate max-w-[140px]">{item.sku.name}</div>
                                            </td>
                                            <td className="p-2 font-bold text-foreground">{item.quantity}</td>
                                            <td className="p-2">
                                                <span className="font-semibold text-primary">{item.location.name}</span>
                                            </td>
                                            <td className="p-2 text-[10px] text-muted-foreground">
                                                {item.lotNumber && <div>Lô: {item.lotNumber}</div>}
                                                {item.palletCode && <div>Pallet: {item.palletCode}</div>}
                                                {!item.lotNumber && !item.palletCode && <span>-</span>}
                                            </td>
                                            <td className="p-2 text-center">
                                                <button
                                                    onClick={() => handleRemoveItem(item.id)}
                                                    className="p-1 hover:bg-destructive/15 text-destructive rounded transition-colors cursor-pointer"
                                                >
                                                    <Trash2 className="size-3.5" />
                                                </button>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        )}
                    </div>

                    {/* Submit Actions */}
                    <div className="mt-4 pt-3 border-t border-border flex items-center justify-between bg-muted/20 p-2 rounded-lg">
                        <div className="text-xs text-muted-foreground flex items-center gap-1">
                            <kbd className="px-1.5 py-0.5 bg-muted border border-border rounded text-[10px] font-mono shadow-sm">Ctrl + Enter</kbd>
                            <span>Xác nhận nhanh</span>
                        </div>
                        <Button
                            onClick={handleConfirmPutaway}
                            disabled={draftItems.length === 0 || isSubmitting}
                            className="bg-linear-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 text-white shadow-md font-bold text-xs px-4 h-9 cursor-pointer transition-all flex items-center gap-1.5"
                        >
                            {isSubmitting ? (
                                <RefreshCw className="size-3.5 animate-spin" />
                            ) : (
                                <CheckCircle2 className="size-4" />
                            )}
                            Xác nhận nhập kho
                        </Button>
                    </div>
                </div>
            </div>

            {/* Right Panel: Warehouse Map */}
            <div className="flex-1 h-full min-h-[400px]">
                <MapLocation
                    selectedLocationId={selectedLocation?.id}
                    onSelectLocation={setSelectedLocation}
                />
            </div>
        </div>
    );
}
