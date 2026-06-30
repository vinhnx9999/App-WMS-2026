import React from "react";
import { useTranslation } from "react-i18next";
import { Layers, Info, ShieldAlert, MoveDown, ChevronDown } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { LookupSearchInput, type LookupItem } from "../../../../components/LookupSearchInput";
import { skuService } from "@/features/master-data/skus/services/sku.service";
import { supplierService } from "@/features/master-data/suppliers/services/supplier.service";
import type { SkuDto } from "@/features/master-data/skus/models/sku-dto.model";
import type { SupplierDto } from "@/features/master-data/suppliers/models/supplier.model";
import type { LocationOccupancy } from "@/components/MapLocation";

export interface PendingPutawayTask {
  id: string;
  sku: SkuDto;
  supplier: SupplierDto | null;
  quantity: number;
  lotNumber: string;
  expiryDate: string;
}

interface PutawayFormProps {
  isDirectMode: boolean;
  showDetailedFields: boolean;
  setShowDetailedFields: React.Dispatch<React.SetStateAction<boolean>>;
  selectedSku: SkuDto | null;
  setSelectedSku: (sku: SkuDto | null) => void;
  selectedSupplier: SupplierDto | null;
  setSelectedSupplier: (supplier: SupplierDto | null) => void;
  expiryDate: string;
  setExpiryDate: (val: string) => void;
  lotNumber: string;
  setLotNumber: (val: string) => void;
  quantity: string;
  setQuantity: (val: string) => void;
  palletCode: string;
  setPalletCode: (val: string) => void;
  selectedLocation: LocationOccupancy | null;
  suggestedLocationIds: string[];
  mixSupplierWarning: boolean;
  isBatchSticky: boolean;
  setIsBatchSticky: (val: boolean) => void;
  onAddToDraft: () => void;
  skuRef: React.RefObject<HTMLInputElement | null>;
  supplierRef: React.RefObject<HTMLInputElement | null>;
  expiryRef: React.RefObject<HTMLInputElement | null>;
  lotRef: React.RefObject<HTMLInputElement | null>;
  qtyRef: React.RefObject<HTMLInputElement | null>;
  palletRef: React.RefObject<HTMLInputElement | null>;
  triggerSuggestions: (skuId: string, qtyVal: string, supplierId?: string) => void;
}

export const PutawayForm: React.FC<PutawayFormProps> = ({
  isDirectMode,
  showDetailedFields,
  setShowDetailedFields,
  selectedSku,
  setSelectedSku,
  selectedSupplier,
  setSelectedSupplier,
  expiryDate,
  setExpiryDate,
  lotNumber,
  setLotNumber,
  quantity,
  setQuantity,
  palletCode,
  setPalletCode,
  selectedLocation,
  suggestedLocationIds,
  mixSupplierWarning,
  isBatchSticky,
  setIsBatchSticky,
  onAddToDraft,
  skuRef,
  supplierRef,
  expiryRef,
  lotRef,
  qtyRef,
  palletRef,
  triggerSuggestions,
}) => {
  const { t } = useTranslation();

  // Load all SKUs for lookup (returns all records, cached/loaded once in LookupSearchInput)
  const fetchSkus = async (): Promise<LookupItem[]> => {
    const res = await skuService.skuLookup();
    return res.success && res.data ? res.data : [];
  };

  // Load all suppliers for lookup (returns all records, cached/loaded once in LookupSearchInput)
  const fetchSuppliers = async (): Promise<LookupItem[]> => {
    const res = await supplierService.supplierLookup();
    return res.success && res.data ? res.data : [];
  };

  // Convert SkuDto to LookupItem
  const skuValue = selectedSku
    ? { id: selectedSku.id, code: selectedSku.skuCode, name: selectedSku.name }
    : null;

  // Convert SupplierDto to LookupItem
  const supplierValue = selectedSupplier
    ? { id: selectedSupplier.id, code: selectedSupplier.code, name: selectedSupplier.name }
    : null;

  return (
    <div className="bg-card text-card-foreground rounded-xl border border-border p-4 shadow-sm min-h-0 flex flex-col">
      <div className="flex items-center justify-between mb-3 border-b border-border pb-2">
        <div className="flex items-center gap-2">
          <Layers className="size-5 text-primary" aria-hidden="true" />
          <h2 className="text-sm font-bold uppercase tracking-wider text-foreground">
            {isDirectMode ? t("inbound.tabs.directPutaway") : t("inbound.tabs.putawayInfo")}
          </h2>
        </div>
        {/* Toggle Advanced Fields button (Always available) */}
        <Button
          variant="ghost"
          size="sm"
          onClick={() => setShowDetailedFields((prev) => !prev)}
          className="h-7 text-xs font-semibold text-primary hover:bg-primary/10 cursor-pointer transition-colors"
        >
          {t("inbound.form.advancedFields")}
          <ChevronDown className="size-4" aria-hidden="true" />
        </Button>
      </div>

      {/* Selected Location Details & Add to Draft Button */}
      <div className="mt-1 mb-3 p-2.5 bg-muted/35 rounded-lg border border-border flex flex-col gap-2">
        <div className="flex items-center justify-between text-xs">
          <div className="flex items-center gap-1.5">
            <span className="text-muted-foreground">{t("inbound.form.targetLocation")}:</span>
            {selectedLocation ? (
              <span className="font-bold text-primary bg-primary/10 px-2 py-0.5 rounded">
                {selectedLocation.name}
              </span>
            ) : (
              <span className="text-muted-foreground italic flex items-center gap-1">
              </span>
            )}
          </div>
          <Button
            type="button"
            onClick={onAddToDraft}
            disabled={!selectedSku}
            size="sm"
            className="h-8 text-xs bg-primary text-primary-foreground hover:bg-primary/95 flex items-center gap-1 cursor-pointer font-bold px-3 transition-colors"
          >
            {t("inbound.form.addToDraft")}
          </Button>
        </div>

        {/* Mix Supplier Warning Banner */}
        {mixSupplierWarning && (
          <div className="flex items-center gap-1.5 text-[10px] text-amber-600 bg-amber-50 dark:bg-amber-950/20 p-1.5 rounded border border-amber-200/50">
            <ShieldAlert className="size-4 shrink-0" aria-hidden="true" />
            <span>{t("inbound.form.errors.mixSupplierWarning")}</span>
          </div>
        )}
      </div>

      {/* INPUT FORM (Always displayed) */}
      <div className="flex-1 overflow-y-auto pr-1 min-h-0">
        <div className="grid grid-cols-2 gap-3">
          {/* SKU Selection using LookupSearchInput */}
          <div className="col-span-2">
            <LookupSearchInput
              label={t("inbound.form.sku")}
              placeholder={t("inbound.form.skuPlaceholder")}
              required
              fetchData={fetchSkus}
              value={skuValue}
              onChange={(item) => {
                if (item) {
                  const mappedSku: SkuDto = {
                    id: item.id,
                    skuCode: item.code,
                    name: item.name,
                    productId: "",
                    productCode: "",
                    productName: "",
                    categoryId: null,
                    categoryName: null,
                    referencePrice: 0,
                    goodsNature: "",
                    description: null,
                    createdAt: "",
                    updatedAt: null,
                  };
                  setSelectedSku(mappedSku);
                  triggerSuggestions(item.id, quantity, selectedSupplier?.id);
                } else {
                  setSelectedSku(null);
                }
              }}
              inputRef={skuRef}
              onKeyDown={(e) => {
                if (e.key === "Enter" && !e.defaultPrevented && selectedSku) {
                  e.preventDefault();
                  if (showDetailedFields) {
                    supplierRef.current?.focus();
                  } else {
                    qtyRef.current?.focus();
                  }
                }
              }}
            />
          </div>

          {/* Detailed / Advanced Fields (Supplier, Lot, Expiry) */}
          {showDetailedFields && (
            <>
              {/* Supplier Selection using LookupSearchInput */}
              <div className="col-span-2">
                <LookupSearchInput
                  label={t("inbound.form.supplier")}
                  placeholder={t("inbound.form.supplierPlaceholder")}
                  fetchData={fetchSuppliers}
                  value={supplierValue}
                  onChange={(item) => {
                    if (item) {
                      const mappedSupplier: SupplierDto = {
                        id: item.id,
                        code: item.code,
                        name: item.name,
                        contact: "",
                        phone: "",
                        email: "",
                        address: "",
                        isDeleted: false,
                        createdAt: "",
                      };
                      setSelectedSupplier(mappedSupplier);
                      triggerSuggestions(selectedSku?.id || "", quantity, item.id);
                    } else {
                      setSelectedSupplier(null);
                    }
                  }}
                  inputRef={supplierRef}
                  onKeyDown={(e) => {
                    if (e.key === "Enter" && !e.defaultPrevented) {
                      e.preventDefault();
                      expiryRef.current?.focus();
                    }
                  }}
                />
              </div>

              {/* Expiry Date */}
              <div>
                <label htmlFor="putaway-expiry-date" className="text-xs font-semibold text-muted-foreground block mb-1">
                  {t("inbound.form.expiryDate")}
                </label>
                <Input
                  id="putaway-expiry-date"
                  name="expiryDate"
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
                <label htmlFor="putaway-lot-number" className="text-xs font-semibold text-muted-foreground block mb-1">
                  {t("inbound.form.lotNumber")}
                </label>
                <Input
                  id="putaway-lot-number"
                  name="lotNumber"
                  autoComplete="off"
                  ref={lotRef}
                  value={lotNumber}
                  onChange={(e) => setLotNumber(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") {
                      e.preventDefault();
                      qtyRef.current?.focus();
                    }
                  }}
                  placeholder={t("inbound.form.lotNumberPlaceholder")}
                  className="h-9 text-xs bg-background border-border"
                />
              </div>
            </>
          )}

          {/* Quantity */}
          <div>
            <label htmlFor="putaway-quantity" className="text-xs font-semibold text-muted-foreground block mb-1">
              {t("inbound.form.quantity")} <span className="text-red-500">*</span>
            </label>
            <Input
              id="putaway-quantity"
              name="quantity"
              inputMode="numeric"
              ref={qtyRef}
              type="number"
              min="1"
              value={quantity}
              onChange={(e) => {
                setQuantity(e.target.value);
                if (selectedSku) triggerSuggestions(selectedSku.id, e.target.value, selectedSupplier?.id);
              }}
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  e.preventDefault();
                  palletRef.current?.focus();
                }
              }}
              placeholder={t("inbound.form.quantityPlaceholder")}
              className="h-9 text-xs bg-background border-border"
            />
          </div>

          {/* Pallet Code */}
          <div>
            <label htmlFor="putaway-pallet-code" className="text-xs font-semibold text-muted-foreground block mb-1">
              {t("inbound.form.location")}
            </label>
            <Input
              id="putaway-pallet-code"
              name="palletCode"
              autoComplete="off"
              ref={palletRef}
              value={palletCode}
              onChange={(e) => setPalletCode(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  e.preventDefault();
                  onAddToDraft();
                }
              }}
              placeholder={t("inbound.form.locationPlaceholder")}
              className="h-9 text-xs bg-background border-border"
            />
          </div>

          {/* Sticky Batch Checkbox */}
          {showDetailedFields && (
            <div className="col-span-2 flex items-center gap-2 mt-1">
              <input
                id="sticky-batch"
                type="checkbox"
                checked={isBatchSticky}
                onChange={(e) => setIsBatchSticky(e.target.checked)}
                className="rounded border-border text-primary focus:ring-primary size-3.5 cursor-pointer"
              />
              <label htmlFor="sticky-batch" className="text-[11px] text-muted-foreground cursor-pointer select-none">
                {t("inbound.form.rememberLot")}
              </label>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
