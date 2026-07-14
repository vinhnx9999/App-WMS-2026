import { useState, useCallback } from "react";
import { inboundService } from "../services/inbound.service";
import type {
  ReceiveItemRow,
  CreateReceiptRequest,
  GetInboundByIdResponse,
  GetInboundReceiptByIdResponse
} from "../models/inbound.model";

export const useReceiveWork = () => {
  const [order, setOrder] = useState<GetInboundByIdResponse | null>(null);
  const [receipt, setReceipt] = useState<GetInboundReceiptByIdResponse | null>(null);
  const [receiptId, setReceiptId] = useState<string | null>(null);
  const [rows, setRows] = useState<ReceiveItemRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const initFromOrder = useCallback(async (orderId: string) => {
    setLoading(true);
    setError(null);
    setReceiptId(null);
    setReceipt(null);
    try {
      const response = await inboundService.getInboundOrderById(orderId);
      if (response.success && response.data) {
        setOrder(response.data);
        const initialRows: ReceiveItemRow[] = response.data.items.map(item => ({
          skuId: item.skuId,
          skuCode: item.skuCode || "",
          skuName: item.skuName || "",
          expectedQuantity: item.quantity,
          receivedQuantity: 0,
          notes: item.note,
          supplierId: item.supplierId,
          supplierName: item.supplierName,
          expiryDate: item.expiryDate,
          serialNumber: item.serialNumber,
          lotNumber: item.lotNumber,
        }));
        setRows(initialRows);
      } else {
        setError(response.message || "Failed to load order details");
      }
    } catch (err) {
      setError("An error occurred while loading order details");
      console.error(err);
    } finally {
      setLoading(false);
    }
  }, []);

  const initFromDraftReceipt = useCallback(async (draftReceiptId: string) => {
    setLoading(true);
    setError(null);
    setReceiptId(draftReceiptId);
    setOrder(null);
    try {
      const response = await inboundService.getInboundReceiptById(draftReceiptId);
      if (response.success && response.data) {
        setReceipt(response.data);
        const initialRows: ReceiveItemRow[] = response.data.items.map(item => ({
          skuId: item.skuId,
          skuCode: item.skuCode || "",
          skuName: item.skuName || "",
          expectedQuantity: item.expectedQuantity,
          receivedQuantity: item.receivedQuantity,
          notes: item.notes,
          supplierId: item.supplierId,
          supplierName: item.supplierName,
          expiryDate: item.expiryDate,
          serialNumber: item.serialNumber,
          lotNumber: item.lotNumber,
        }));
        setRows(initialRows);
      } else {
        setError(response.message || "Failed to load receipt details");
      }
    } catch (err) {
      setError("An error occurred while loading receipt details");
      console.error(err);
    } finally {
      setLoading(false);
    }
  }, []);

  const scanBarcode = useCallback((barcode: string): boolean => {
    let matched = false;
    setRows(prevRows =>
      prevRows.map(row => {
        if (row.skuCode.toLowerCase() === barcode.trim().toLowerCase()) {
          matched = true;
          return {
            ...row,
            receivedQuantity: row.receivedQuantity + 1,
          };
        }
        return row;
      })
    );
    return matched;
  }, []);

  const updateRow = useCallback((skuId: string, updatedFields: Partial<ReceiveItemRow>) => {
    setRows(prevRows =>
      prevRows.map(row => (row.skuId === skuId ? { ...row, ...updatedFields } : row))
    );
  }, []);

  const saveDraft = async (warehouseId: string): Promise<string | null> => {
    if (rows.length === 0) {
      setError("No items to save");
      return null;
    }
    setSaving(true);
    setError(null);
    try {
      const request: CreateReceiptRequest = {
        inboundOrderId: order?.id || receipt?.inboundOrderId || null,
        warehouseId,
        items: rows.map(row => ({
          skuId: row.skuId,
          expectedQuantity: row.expectedQuantity,
          receivedQuantity: row.receivedQuantity,
          notes: row.notes,
          supplierId: row.supplierId,
          expiryDate: row.expiryDate,
          serialNumber: row.serialNumber,
          lotNumber: row.lotNumber,
        })),
      };

      const response = await inboundService.createInboundReceipt(request);
      if (response.success && response.data) {
        setReceiptId(response.data);
        return response.data;
      } else {
        setError(response.message || "Failed to save draft receipt");
        return null;
      }
    } catch (err) {
      setError("An error occurred while saving draft receipt");
      console.error(err);
      return null;
    } finally {
      setSaving(false);
    }
  };

  const completeReceipt = async (warehouseId: string): Promise<boolean> => {
    setSaving(true);
    setError(null);
    try {
      let activeReceiptId = receiptId;

      if (!activeReceiptId) {
        const request: CreateReceiptRequest = {
          inboundOrderId: order?.id || receipt?.inboundOrderId || null,
          warehouseId,
          items: rows.map(row => ({
            skuId: row.skuId,
            expectedQuantity: row.expectedQuantity,
            receivedQuantity: row.receivedQuantity,
            notes: row.notes,
            supplierId: row.supplierId,
            expiryDate: row.expiryDate,
            serialNumber: row.serialNumber,
            lotNumber: row.lotNumber,
          })),
        };

        const response = await inboundService.createInboundReceipt(request);
        if (response.success && response.data) {
          activeReceiptId = response.data;
          setReceiptId(activeReceiptId);
        } else {
          setError(response.message || "Failed to create receipt before completing");
          setSaving(false);
          return false;
        }
      }

      const completeResponse = await inboundService.completeInboundReceipt(activeReceiptId);
      if (completeResponse.success) {
        setSaving(false);
        return true;
      } else {
        setError(completeResponse.message || "Failed to complete receipt");
        setSaving(false);
        return false;
      }
    } catch (err) {
      setError("An error occurred while completing receipt");
      console.error(err);
      setSaving(false);
      return false;
    }
  };

  return {
    order,
    receipt,
    rows,
    loading,
    saving,
    error,
    receiptId,
    initFromOrder,
    initFromDraftReceipt,
    scanBarcode,
    updateRow,
    saveDraft,
    completeReceipt,
  };
};
