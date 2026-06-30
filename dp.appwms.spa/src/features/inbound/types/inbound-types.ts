import type { SkuDto } from "@/features/master-data/skus/models/sku-dto.model";
import type { SupplierDto } from "@/features/master-data/suppliers/models/supplier.model";
import type { LocationOccupancy } from "@/components/MapLocation";

export type WorkflowStep = "po" | "receive" | "qc" | "putaway";

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
