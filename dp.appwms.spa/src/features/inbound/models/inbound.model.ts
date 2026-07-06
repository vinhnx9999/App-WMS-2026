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
  Approved: 0,
  Receiving: 1,
  Completed: 2,
  Cancelled: 3,
} as const;

export type InboundStatus = typeof InboundStatus[keyof typeof InboundStatus];

export const ReceiptStatus = {
  Receiving: 0,
  Completed: 1,
} as const;

export type ReceiptStatus = typeof ReceiptStatus[keyof typeof ReceiptStatus];

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
  expectedDate: string | null;
  status: InboundStatus;
  totalValue: number;
  itemsCount: number;
}

export interface InboundItemDetailDto {
  skuId: string;
  skuCode: string | null;
  skuName: string | null;
  quantity: number;
  receivedQuantity: number;
  supplierId: string | null;
  supplierName: string | null;
  expiryDate: string | null;
  serialNumber: string | null;
  lotNumber: string | null;
  note: string | null;
}

export interface GetInboundByIdResponse {
  id: string;
  orderNumber: string;
  expectedDate: string | null;
  receivedDate: string | null;
  status: InboundStatus;
  totalValue: number;
  notes: string | null;
  items: InboundItemDetailDto[];
}

export interface CreatePOItemRequest {
  skuId: string;
  quantity: number;
  supplierId?: string | null;
}

export interface CreatePORequest {
  expectedDate?: string | null;
  notes?: string | null;
  items: CreatePOItemRequest[];
}

export interface CreatePoResponse {
  id: string;
}

export interface CreateReceiptItemRequest {
  skuId: string;
  expectedQuantity: number;
  receivedQuantity: number;
  notes?: string | null;
  supplierId?: string | null;
  expiryDate?: string | null;
  serialNumber?: string | null;
  lotNumber?: string | null;
}

export interface CreateReceiptRequest {
  inboundOrderId: string | null;
  warehouseId: string;
  items: CreateReceiptItemRequest[];
}

export interface ReceiveItemRow {
  skuId: string;
  skuCode: string;
  skuName: string;
  expectedQuantity: number;
  receivedQuantity: number;
  notes: string | null;
  supplierId: string | null;
  supplierName: string | null;
  expiryDate: string | null;
  serialNumber: string | null;
  lotNumber: string | null;
}

export interface InboundReceiptDto {
  id: string;
  receiptNumber: string;
  inboundOrderId: string | null;
  inboundOrderNumber: string | null;
  warehouseId: string;
  warehouseName: string | null;
  status: ReceiptStatus;
  createdAt: string;
  itemsCount: number;
}

export interface InboundReceiptItemDetailDto {
  skuId: string;
  skuCode: string | null;
  skuName: string | null;
  expectedQuantity: number;
  receivedQuantity: number;
  notes: string | null;
  supplierId: string | null;
  supplierName: string | null;
  expiryDate: string | null;
  serialNumber: string | null;
  lotNumber: string | null;
}

export interface GetInboundReceiptByIdResponse {
  id: string;
  receiptNumber: string;
  inboundOrderId: string | null;
  inboundOrderNumber: string | null;
  warehouseId: string;
  warehouseName: string | null;
  status: ReceiptStatus;
  createdAt: string;
  items: InboundReceiptItemDetailDto[];
}

export type InboundReceiptDetailDto = GetInboundReceiptByIdResponse;


