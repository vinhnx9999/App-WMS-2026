import { useState, useEffect, useCallback } from "react";
import { inboundService } from "../services/inbound.service";
import type { InboundReceiptDto } from "../models/inbound.model";
import type { SearchObject } from "@/models/search.model";
import { useWarehouseStore } from "@/store/warehouse-store";

export const useReceiveStep = () => {
  const { selectedWarehouse } = useWarehouseStore();
  const [receipts, setReceipts] = useState<InboundReceiptDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [limit, setLimit] = useState(10);
  const [totalCount, setTotalCount] = useState(0);
  const [searchObjects, setSearchObjects] = useState<SearchObject[]>([]);

  const fetchReceipts = useCallback(async () => {
    if (!selectedWarehouse?.id) {
      setReceipts([]);
      setTotalCount(0);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      // Add warehouseId filter automatically
      const warehouseFilter: SearchObject = {
        name: "WarehouseId",
        operator: 1, // Equal
        value: selectedWarehouse.id,
      };

      const criteria = [warehouseFilter, ...searchObjects];
      const response = await inboundService.searchInboundReceipts({
        searchObjects: criteria,
        page,
        limit,
      });

      if (response.success && response.data) {
        setReceipts(response.data.items);
        setTotalCount(response.data.totalCount);
      } else {
        setError(response.message || "Failed to search receipts");
      }
    } catch (err) {
      setError("An error occurred while fetching inbound receipts");
      console.error(err);
    } finally {
      setLoading(false);
    }
  }, [selectedWarehouse?.id, page, limit, searchObjects]);

  useEffect(() => {
    fetchReceipts();
  }, [fetchReceipts]);

  const forceCompletePO = async (poId: string): Promise<boolean> => {
    try {
      const response = await inboundService.forceCompleteInboundOrder(poId);
      if (response.success) {
        return true;
      }
      setError(response.message || "Failed to force complete order");
      return false;
    } catch (err) {
      setError("An error occurred while force completing the order");
      console.error(err);
      return false;
    }
  };

  return {
    receipts,
    loading,
    error,
    page,
    limit,
    totalCount,
    setPage,
    setLimit,
    setSearchObjects,
    refresh: fetchReceipts,
    forceCompletePO,
  };
};
