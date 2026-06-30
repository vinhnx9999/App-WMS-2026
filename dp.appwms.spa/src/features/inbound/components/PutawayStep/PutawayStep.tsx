import React, { useState, useEffect, useRef, useMemo, useCallback } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { z } from "zod";
import MapLocation, { type LocationOccupancy } from "@/components/MapLocation";
import { useWarehouseStore } from "@/store/warehouse-store";
import { PutawayForm } from "./PutawayForm";
import { PutawayPendingList } from "./PutawayPendingList";
import { PutawayDraftTable } from "./PutawayDraftTable";
import type { PendingPutawayTask, DraftItem } from "../../types/inbound-types";
import apiClient from "@/api/api-client";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/models/response";
import type { SkuDto } from "@/features/master-data/skus/models/sku-dto.model";
import type { SupplierDto } from "@/features/master-data/suppliers/models/supplier.model";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter, DialogClose } from "@/components/ui/dialog";

// Zod validation schema generator
const createPutawaySchema = (t: (key: string) => string) =>
  z.object({
    skuId: z.string().min(1, t("inbound.form.errors.skuRequired")),
    quantity: z.coerce
      .number({ message: t("inbound.form.errors.quantityRequired") })
      .positive(t("inbound.form.errors.quantityPositive")),
    locationId: z.string().min(1, t("inbound.form.errors.locationRequired")),
    palletCode: z.string().optional(),
    supplierId: z.string().optional().nullable(),
    lotNumber: z.string().optional().nullable(),
    expiryDate: z.string().optional().nullable(),
  });

interface PutawayStepProps {
  isDirectMode: boolean;
}

export const PutawayStep: React.FC<PutawayStepProps> = ({ isDirectMode }) => {
  const { t } = useTranslation();
  const { selectedWarehouse } = useWarehouseStore();

  // Draft list state
  const [draftItems, setDraftItems] = useState<DraftItem[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Form states
  const [selectedSku, setSelectedSku] = useState<SkuDto | null>(null);
  const [selectedSupplier, setSelectedSupplier] = useState<SupplierDto | null>(null);
  const [expiryDate, setExpiryDate] = useState("");
  const [lotNumber, setLotNumber] = useState("");
  const [quantity, setQuantity] = useState("");
  const [palletCode, setPalletCode] = useState("");
  const [selectedLocation, setSelectedLocation] = useState<LocationOccupancy | null>(null);
  const [isClearConfirmOpen, setIsClearConfirmOpen] = useState(false);

  // Advanced fields toggle (F4)
  const [showDetailedFields, setShowDetailedFields] = useState(false);
  // Sticky batch checkbox state
  const [isBatchSticky, setIsBatchSticky] = useState(true);

  // Suggestion state
  const [suggestedLocationIds, setSuggestedLocationIds] = useState<string[]>([]);
  const [allWarehouseLocations, setAllWarehouseLocations] = useState<LocationOccupancy[]>([]);

  // Keyboard navigation refs
  const skuRef = useRef<HTMLInputElement>(null);
  const supplierRef = useRef<HTMLInputElement>(null);
  const expiryRef = useRef<HTMLInputElement>(null);
  const lotRef = useRef<HTMLInputElement>(null);
  const qtyRef = useRef<HTMLInputElement>(null);
  const palletRef = useRef<HTMLInputElement>(null);

  // Mocked Pending Putaway Tasks (when preceding steps exist)
  const [pendingTasks, setPendingTasks] = useState<PendingPutawayTask[]>([
    {
      id: "task-1",
      sku: {
        id: "sku-1",
        productId: "p1",
        productCode: "IP15",
        productName: "iPhone 15 Pro Max",
        categoryId: null,
        categoryName: null,
        skuCode: "SKU-IPHONE15",
        name: "iPhone 15 Pro Max 256GB",
        referencePrice: 1200,
        goodsNature: "Electronics",
        description: null,
        createdAt: "",
        updatedAt: null
      },
      supplier: { id: "sup-1", name: "Apple Vietnam LLC", code: "APL", contact: "", phone: "", email: "", address: "", isDeleted: false, createdAt: "" },
      quantity: 15,
      lotNumber: "LOT-2026-001",
      expiryDate: "2027-12-31"
    },
    {
      id: "task-2",
      sku: {
        id: "sku-2",
        productId: "p2",
        productCode: "S24U",
        productName: "Samsung Galaxy S24",
        categoryId: null,
        categoryName: null,
        skuCode: "SKU-SAMS24",
        name: "Samsung Galaxy S24 Ultra",
        referencePrice: 1100,
        goodsNature: "Electronics",
        description: null,
        createdAt: "",
        updatedAt: null
      },
      supplier: { id: "sup-2", name: "Samsung Electronics", code: "SSG", contact: "", phone: "", email: "", address: "", isDeleted: false, createdAt: "" },
      quantity: 8,
      lotNumber: "LOT-2026-002",
      expiryDate: "2027-06-30"
    }
  ]);

  // Load all warehouse locations once to simulate suggestions
  useEffect(() => {
    if (!selectedWarehouse?.id) return;
    apiClient.get<ApiResponse<LocationOccupancy[]>>(
      ENDPOINTS.LOCATION.OCCUPANCY,
      { params: { warehouseId: selectedWarehouse.id } }
    ).then(res => {
      if (res.data.success && res.data.data) {
        setAllWarehouseLocations(res.data.data);
      }
    }).catch(err => console.error("Failed to load locations for suggestions", err));
  }, [selectedWarehouse?.id]);

  // Trigger location suggestions based on SKU selection and quantity
  const triggerSuggestions = useCallback((skuId: string, qtyVal: string, supplierId?: string) => {
    if (!skuId || allWarehouseLocations.length === 0) {
      setSuggestedLocationIds([]);
      return;
    }

    // Simple rule: Suggest up to 3 empty locations on the map
    const emptyLocs = allWarehouseLocations
      .filter(loc => loc.occupancyStatus === "empty")
      .slice(0, 3);

    const ids = emptyLocs.map(loc => loc.id);
    setSuggestedLocationIds(ids);

    // Smart focus: Auto-select the first suggestion if no location is selected
    if (emptyLocs.length > 0 && !selectedLocation) {
      setSelectedLocation(emptyLocs[0]);
    }
  }, [allWarehouseLocations, selectedLocation]);

  // Detect Mix-Supplier Warning
  const mixSupplierWarning = useMemo(() => {
    if (!selectedLocation || !selectedSku) return false;

    // Check if the selected location already has the same SKU from a different supplier in the draft list
    const hasDifferentSupplierInDraft = draftItems.some(
      item => item.location.id === selectedLocation.id &&
        item.sku.id === selectedSku.id &&
        item.supplier?.id !== selectedSupplier?.id
    );

    return hasDifferentSupplierInDraft;
  }, [selectedLocation, selectedSku, selectedSupplier, draftItems]);

  // Submit draft to backend
  const handleConfirmPutaway = async () => {
    if (draftItems.length === 0) {
      toast.error(t("inbound.draft.empty"));
      return;
    }
    if (!selectedWarehouse?.id) {
      toast.error(t("translation:navigation.selectWarehousePrompt"));
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
        serialNumber: null
      }));

      const payload = {
        warehouseId: selectedWarehouse.id,
        items: itemsPayload
      };

      const response = await apiClient.post<ApiResponse<string>>(
        ENDPOINTS.INBOUND.DIRECT_PUTAWAY,
        payload
      );

      if (response.data.success) {
        toast.success(t("inbound.form.errors.submitSuccess", "Direct putaway completed successfully!"));
        setDraftItems([]);
      } else {
        toast.error(response.data.message || "Failed to submit putaway");
      }
    } catch (err: any) {
      console.error("Error submitting putaway:", err);
      const errMsg = err.response?.data?.message || err.message || "Server error";
      toast.error(`${t("inbound.form.errors.submitFailed", "Putaway failed")}: ${errMsg}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleAddToDraft = () => {
    const validator = createPutawaySchema(t);
    const result = validator.safeParse({
      skuId: selectedSku?.id || "",
      quantity: quantity,
      locationId: selectedLocation?.id || "",
      palletCode: palletCode.trim(),
      supplierId: selectedSupplier?.id || null,
      lotNumber: lotNumber || null,
      expiryDate: expiryDate || null,
    });

    if (!result.success) {
      const firstError = result.error.issues[0];
      toast.error(firstError.message);

      // Auto-focus the problematic field
      if (firstError.path.includes("skuId")) skuRef.current?.focus();
      else if (firstError.path.includes("quantity")) qtyRef.current?.focus();
      return;
    }

    const data = result.data;

    // Create draft item
    const newItem: DraftItem = {
      id: Math.random().toString(36).substring(2, 9),
      sku: selectedSku!,
      supplier: selectedSupplier,
      expiryDate: data.expiryDate || "",
      lotNumber: data.lotNumber || "",
      quantity: data.quantity,
      palletCode: data.palletCode || "",
      location: selectedLocation!
    };

    setDraftItems(prev => [...prev, newItem]);
    toast.success(`${t("inbound.form.addToDraft")} - ${selectedSku!.skuCode} (${data.quantity})`);

    // Reset form based on Sticky Batch configuration
    setSelectedSku(null);
    setQuantity("");
    setPalletCode("");
    setSelectedLocation(null);
    setSuggestedLocationIds([]);

    if (!isBatchSticky) {
      setSelectedSupplier(null);
      setExpiryDate("");
      setLotNumber("");
    }

    // Return focus to SKU for next entry
    setTimeout(() => skuRef.current?.focus(), 50);
  };

  // Remove item from draft
  const handleRemoveItem = (id: string) => {
    setDraftItems(prev => prev.filter(item => item.id !== id));
  };

  // Clear all draft items
  const handleClearDraft = () => {
    if (draftItems.length === 0) return;
    setIsClearConfirmOpen(true);
  };

  const confirmClearDraft = () => {
    setDraftItems([]);
    setIsClearConfirmOpen(false);
  };

  // Click on a pending task from preceding steps
  const handleSelectPendingTask = (task: PendingPutawayTask) => {
    setSelectedSku(task.sku);
    setSelectedSupplier(task.supplier);
    setQuantity(task.quantity.toString());
    setLotNumber(task.lotNumber);
    setExpiryDate(task.expiryDate);

    // Automatically trigger suggestions for this task
    triggerSuggestions(task.sku.id, task.quantity.toString(), task.supplier?.id);
    toast.info(`${t("inbound.form.sku")}: ${task.sku.skuCode} - ${t("inbound.form.quantity")}: ${task.quantity}`);
  };

  // Keyboard navigation & Shortcuts (F4, Ctrl+Enter, Esc)
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "F4") {
        e.preventDefault();
        setShowDetailedFields(prev => !prev);
      } else if (e.ctrlKey && e.key === "Enter") {
        e.preventDefault();
        handleConfirmPutaway();
      } else if (e.key === "Escape") {
        // Reset current form
        setSelectedSku(null);
        setSelectedSupplier(null);
        setExpiryDate("");
        setLotNumber("");
        setQuantity("");
        setPalletCode("");
        setSelectedLocation(null);
        setSuggestedLocationIds([]);
        skuRef.current?.focus();
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [draftItems, isSubmitting, selectedWarehouse, selectedSku, quantity, selectedLocation, selectedSupplier, palletCode, lotNumber, expiryDate]);

  return (
    <div className="h-full w-full flex flex-col lg:flex-row gap-4 overflow-hidden">
      {/* Left Panel: Form / Pending List & Draft Table */}
      <div className="w-full lg:w-[42%] flex flex-col gap-4 h-full pr-1 pb-6 overflow-hidden">
        {isDirectMode ? (
          <PutawayForm
            isDirectMode={isDirectMode}
            showDetailedFields={showDetailedFields}
            setShowDetailedFields={setShowDetailedFields}
            selectedSku={selectedSku}
            setSelectedSku={setSelectedSku}
            selectedSupplier={selectedSupplier}
            setSelectedSupplier={setSelectedSupplier}
            expiryDate={expiryDate}
            setExpiryDate={setExpiryDate}
            lotNumber={lotNumber}
            setLotNumber={setLotNumber}
            quantity={quantity}
            setQuantity={setQuantity}
            palletCode={palletCode}
            setPalletCode={setPalletCode}
            selectedLocation={selectedLocation}
            suggestedLocationIds={suggestedLocationIds}
            mixSupplierWarning={mixSupplierWarning}
            isBatchSticky={isBatchSticky}
            setIsBatchSticky={setIsBatchSticky}
            onAddToDraft={handleAddToDraft}
            skuRef={skuRef}
            supplierRef={supplierRef}
            expiryRef={expiryRef}
            lotRef={lotRef}
            qtyRef={qtyRef}
            palletRef={palletRef}
            triggerSuggestions={triggerSuggestions}
          />
        ) : (
          <PutawayPendingList
            pendingTasks={pendingTasks}
            selectedSku={selectedSku}
            onSelectPendingTask={handleSelectPendingTask}
            selectedLocation={selectedLocation}
            onAddToDraft={handleAddToDraft}
          />
        )}

        <PutawayDraftTable
          draftItems={draftItems}
          onRemoveItem={handleRemoveItem}
          onClearDraft={handleClearDraft}
          onConfirmPutaway={handleConfirmPutaway}
          isSubmitting={isSubmitting}
        />
      </div>

      {/* Right Panel: Warehouse Map */}
      <div className="flex-1 h-full min-h-[400px]">
        <MapLocation
          selectedLocationId={selectedLocation?.id}
          suggestedLocationIds={suggestedLocationIds}
          onSelectLocation={setSelectedLocation}
        />
      </div>

      {/* Confirmation Dialog for Clearing Draft */}
      <Dialog open={isClearConfirmOpen} onOpenChange={setIsClearConfirmOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t("inbound.draft.confirmClearTitle")}</DialogTitle>
            <DialogDescription>
              {t("inbound.draft.confirmClear")}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="mt-4 flex gap-2 justify-end">
            <DialogClose asChild>
              <Button variant="outline" size="sm">
                {t("common.cancel")}
              </Button>
            </DialogClose>
            <Button
              variant="destructive"
              size="sm"
              onClick={confirmClearDraft}
            >
              {t("common.confirm")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};
