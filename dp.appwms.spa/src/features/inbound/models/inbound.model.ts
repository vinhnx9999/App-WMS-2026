import type { SkuDto } from "@/features/master-data/skus/models/sku-dto.model";
import type { SupplierDto } from "@/features/master-data/suppliers/models/supplier.model";
import type { LocationOccupancy } from "@/components/MapLocation";

export const INBOUND_STEPS = {
  PO: "po",
  RECEIVE: "receive",
  QC: "qc",
  PUTAWAY: "putaway",
} as const;

export type WorkflowStep = typeof INBOUND_STEPS[keyof typeof INBOUND_STEPS];

export interface PendingPutawayTask {
  id: string;
  sku: SkuDto;
  supplier: SupplierDto | null;
  quantity: number;
  lotNumber: string;
  expiryDate: string;
}

export interface DraftItem {
  id: string;
  sku: SkuDto;
  supplier: SupplierDto | null;
  expiryDate: string;
  lotNumber: string;
  quantity: number;
  palletCode: string;
  location: LocationOccupancy;
}
