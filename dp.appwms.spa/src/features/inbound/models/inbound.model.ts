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

export const InboundStatus = {
  Pending: 0,
  Approved: 1,
  Receiving: 2,
  Completed: 3,
  Cancelled: 4,
} as const;

export type InboundStatus = typeof InboundStatus[keyof typeof InboundStatus];

export interface InboundItemDto {
  skuCode: string;
  skuName: string;
  quantity: number;
  receivedQuantity: number;
  supplierId: string | null;
  supplierName: string;
}

export interface InboundOrderDto {
  id: string;
  orderNumber: string;
  supplierName: string;
  expectedDate: string | null;
  status: InboundStatus;
  totalValue: number;
  itemsCount: number;
  items: InboundItemDto[];
}

export interface SearchInboundOrdersParams {
  search?: string;
  supplierId?: string;
  status?: InboundStatus;
  sortBy?: string;
  sortOrder?: string;
  page?: number;
  limit?: number;
}


