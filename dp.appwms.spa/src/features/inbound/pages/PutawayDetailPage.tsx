import { useState, useEffect, useRef, useMemo, useCallback } from "react";
import { useParams, useNavigate, useBlocker, useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { z } from "zod";
import { ArrowLeft, Map as MapIcon, Loader2, AlertTriangle } from "lucide-react";
import MapLocation, { type LocationOccupancy } from "@/components/MapLocation";
import { useWarehouseStore } from "@/store/warehouse-store";
import { PutawayForm } from "../components/Putaway/PutawayForm";
import { PutawayDraftTable } from "../components/Putaway/PutawayDraftTable";
import type { DraftItem } from "../models/inbound.model";
import apiClient from "@/api/api-client";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/models/response";
import type { SkuDto } from "@/features/master-data/skus/models/sku-dto.model";
import type { SupplierDto } from "@/features/master-data/suppliers/models/supplier.model";
import { Button } from "@/components/ui/button";
import {
  Dialog as ConfirmDialog,
  DialogContent as ConfirmDialogContent,
  DialogHeader as ConfirmDialogHeader,
  DialogTitle as ConfirmDialogTitle,
  DialogDescription as ConfirmDialogDescription,
  DialogFooter as ConfirmDialogFooter,
  DialogClose as ConfirmDialogClose
} from "@/components/ui/dialog";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from "@/components/ui/sheet";
import { inboundService } from "../services/inbound.service";

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

export default function PutawayDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const locationPath = useLocation();
  const { t } = useTranslation();
  const { selectedWarehouse } = useWarehouseStore();

  const isDirectMode = id === "create" || locationPath.pathname.includes("/create");

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
  
  // Map Sheet State
  const [isMapOpen, setIsMapOpen] = useState(false);
  const [loadingTask, setLoadingTask] = useState(!isDirectMode);
  const [errorTask, setErrorTask] = useState<string | null>(null);

  // Keyboard navigation refs
  const skuRef = useRef<HTMLInputElement>(null);
  const supplierRef = useRef<HTMLInputElement>(null);
  const expiryRef = useRef<HTMLInputElement>(null);
  const lotRef = useRef<HTMLInputElement>(null);
  const qtyRef = useRef<HTMLInputElement>(null);
  const palletRef = useRef<HTMLInputElement>(null);

  // Block navigation if draft has unsaved items
  const isDirty = draftItems.length > 0;
  const blocker = useBlocker(
    ({ currentLocation, nextLocation }) =>
      isDirty && currentLocation.pathname !== nextLocation.pathname
  );

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

  // If not direct mode, load the task details
  useEffect(() => {
    const fetchTask = async (taskId: string) => {
      setLoadingTask(true);
      setErrorTask(null);
      try {
        // Using receipt API as a placeholder for Putaway Task API
        const response = await inboundService.getInboundReceiptById(taskId);
        if (response.success && response.data) {
          // In a real scenario, this would populate the draft table or form depending on task state
          // For now we just load and set ready
        } else {
          setErrorTask(response.message || "Failed to load Putaway task");
        }
      } catch (err) {
        setErrorTask("An error occurred while loading Putaway task");
        console.error("Error fetching Putaway task:", err);
      } finally {
        setLoadingTask(false);
      }
    };

    if (!isDirectMode && id) {
      fetchTask(id);
    }
  }, [id, isDirectMode]);

  // Trigger location suggestions based on SKU selection and quantity
  const triggerSuggestions = useCallback((skuId: string, _qtyVal: string, _supplierId?: string) => {
   
    console.log("Triggering suggestions for SKU:", skuId, "Quantity:", _qtyVal, "Supplier:", _supplierId);
    if (!skuId || allWarehouseLocations.length === 0) {
      setSuggestedLocationIds([]);
      return;
    }
    const emptyLocs = allWarehouseLocations
      .filter(loc => loc.occupancyStatus === "empty")
      .slice(0, 3);

    const ids = emptyLocs.map(loc => loc.id);
    setSuggestedLocationIds(ids);

    if (emptyLocs.length > 0 && !selectedLocation) {
      setSelectedLocation(emptyLocs[0]);
    }
  }, [allWarehouseLocations, selectedLocation]);

  // Detect Mix-Supplier Warning
  const mixSupplierWarning = useMemo(() => {
    if (!selectedLocation || !selectedSku) return false;
    return draftItems.some(
      item => item.location.id === selectedLocation.id &&
        item.sku.id === selectedSku.id &&
        item.supplier?.id !== selectedSupplier?.id
    );
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
        if (!isDirectMode) {
          navigate("/inbound/putaway");
        }
      } else {
        toast.error(response.data.message || "Failed to submit putaway");
      }
    } catch (err: unknown) {
      console.error("Error submitting putaway:", err);
      const errMsg = err instanceof Error ? err.message : ((err as { response?: { data?: { message?: string } } })?.response?.data?.message || "Server error");
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

      if (firstError.path.includes("skuId")) skuRef.current?.focus();
      else if (firstError.path.includes("quantity")) qtyRef.current?.focus();
      return;
    }

    const data = result.data;

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

    setTimeout(() => skuRef.current?.focus(), 50);
  };

  const handleRemoveItem = (id: string) => {
    setDraftItems(prev => prev.filter(item => item.id !== id));
  };

  const handleClearDraft = () => {
    if (draftItems.length === 0) return;
    setIsClearConfirmOpen(true);
  };

  const confirmClearDraft = () => {
    setDraftItems([]);
    setIsClearConfirmOpen(false);
  };

  const handleBack = () => {
    navigate("/inbound/putaway");
  };

  // Keyboard navigation & Shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "F4") {
        e.preventDefault();
        setShowDetailedFields(prev => !prev);
      } else if (e.ctrlKey && e.key === "Enter") {
        e.preventDefault();
        handleConfirmPutaway();
      } else if (e.key === "Escape" && !isClearConfirmOpen && !isMapOpen && blocker.state !== "blocked") {
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
  }, [draftItems, isSubmitting, selectedWarehouse, selectedSku, quantity, selectedLocation, selectedSupplier, palletCode, lotNumber, expiryDate, isClearConfirmOpen, isMapOpen, blocker.state]);

  if (loadingTask) {
    return (
      <div className="h-full w-full flex flex-col items-center justify-center bg-card border rounded-xl p-8 space-y-3">
        <Loader2 className="size-8 text-primary animate-spin" />
        <p className="text-xs text-muted-foreground">{t("common.loading")}</p>
      </div>
    );
  }

  const title = isDirectMode ? t("inbound.putaway.actions.create", "Direct Putaway") : t("inbound.putaway.detail.title", "Putaway Detail");

  return (
    <>
      <div className="h-full w-full flex flex-col overflow-hidden bg-card border rounded-xl shadow-sm">
        {/* Header */}
        <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 p-4 border-b bg-secondary/10">
          <div className="flex items-center gap-3">
            <Button
              variant="ghost"
              size="icon-xs"
              onClick={handleBack}
              className="cursor-pointer"
              title={t("common.button.cancel", "Back")}
            >
              <ArrowLeft className="size-4" />
            </Button>
            <div>
              <h2 className="text-sm font-bold text-foreground flex items-center gap-2">
                {title}
              </h2>
              <p className="text-[10px] text-muted-foreground mt-0.5">
                {t("inbound.receive.detail.warehouse")}: <span className="font-semibold text-foreground">{selectedWarehouse?.name || "-"}</span>
              </p>
            </div>
          </div>
          
          <Sheet open={isMapOpen} onOpenChange={setIsMapOpen}>
            <SheetTrigger asChild>
              <Button variant="outline" size="sm" className="h-8 text-xs font-semibold cursor-pointer border-primary/20 text-primary hover:bg-primary/10">
                <MapIcon className="size-3.5 mr-1" />
                {t("inbound.putaway.actions.viewMap", "View Map")}
              </Button>
            </SheetTrigger>
            <SheetContent side="right" className="w-[90vw] sm:w-[80vw] sm:max-w-4xl p-0 flex flex-col h-full">
              <SheetHeader className="p-4 border-b bg-secondary/10">
                <SheetTitle>{t("inbound.putaway.map.title", "Warehouse Map")}</SheetTitle>
                <SheetDescription className="text-xs">
                  {t("inbound.putaway.map.description", "Select a target location for putaway.")}
                </SheetDescription>
              </SheetHeader>
              <div className="flex-1 w-full overflow-hidden bg-muted/20">
                <MapLocation
                  selectedLocationId={selectedLocation?.id}
                  suggestedLocationIds={suggestedLocationIds}
                  onSelectLocation={(loc) => {
                    setSelectedLocation(loc);
                    setIsMapOpen(false); // Auto close when selected
                  }}
                />
              </div>
            </SheetContent>
          </Sheet>
        </div>

        {errorTask && (
          <div className="mx-4 mt-4 p-3 bg-destructive/10 border border-destructive/20 text-destructive text-xs rounded-lg">
            {errorTask}
          </div>
        )}

        {/* Content */}
        <div className="flex-1 min-h-0 w-full overflow-hidden flex flex-col lg:flex-row gap-4 p-4">
          <div className="w-full lg:w-[45%] flex flex-col h-full overflow-hidden">
             <PutawayForm
              isDirectMode={true} // In detail page, we just use the form to append to draft
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
          </div>

          <div className="w-full lg:flex-1 h-full flex flex-col overflow-hidden min-h-[300px]">
            <PutawayDraftTable
              draftItems={draftItems}
              onRemoveItem={handleRemoveItem}
              onClearDraft={handleClearDraft}
              onConfirmPutaway={handleConfirmPutaway}
              isSubmitting={isSubmitting}
            />
          </div>
        </div>
      </div>

      {/* Confirmation Dialog for Clearing Draft */}
      <ConfirmDialog open={isClearConfirmOpen} onOpenChange={setIsClearConfirmOpen}>
        <ConfirmDialogContent>
          <ConfirmDialogHeader>
            <ConfirmDialogTitle>{t("inbound.draft.confirmClearTitle")}</ConfirmDialogTitle>
            <ConfirmDialogDescription>
              {t("inbound.draft.confirmClear")}
            </ConfirmDialogDescription>
          </ConfirmDialogHeader>
          <ConfirmDialogFooter className="mt-4 flex gap-2 justify-end">
            <ConfirmDialogClose asChild>
              <Button variant="outline" size="sm">
                {t("common.cancel")}
              </Button>
            </ConfirmDialogClose>
            <Button
              variant="destructive"
              size="sm"
              onClick={confirmClearDraft}
            >
              {t("common.confirm")}
            </Button>
          </ConfirmDialogFooter>
        </ConfirmDialogContent>
      </ConfirmDialog>

      {/* Unsaved Changes Blocker Dialog */}
      <ConfirmDialog open={blocker.state === "blocked"} onOpenChange={(open) => {
        if (!open && blocker.state === "blocked") {
          blocker.reset();
        }
      }}>
        <ConfirmDialogContent>
          <ConfirmDialogHeader>
            <ConfirmDialogTitle className="flex items-center gap-2">
              <AlertTriangle className="size-5 text-warning" />
              {t("inbound.receive.detail.unsaved.title", "Unsaved Changes")}
            </ConfirmDialogTitle>
            <ConfirmDialogDescription>
              {t("inbound.receive.detail.unsaved.message", "You have unsaved changes. Are you sure you want to leave? Your changes will be lost.")}
            </ConfirmDialogDescription>
          </ConfirmDialogHeader>
          <ConfirmDialogFooter>
            <Button variant="outline" onClick={() => blocker.state === "blocked" && blocker.reset()}>
              {t("common.button.cancel", "Cancel")}
            </Button>
            <Button variant="destructive" onClick={() => blocker.state === "blocked" && blocker.proceed()}>
              {t("inbound.receive.detail.unsaved.confirm", "Leave without saving")}
            </Button>
          </ConfirmDialogFooter>
        </ConfirmDialogContent>
      </ConfirmDialog>
    </>
  );
}
